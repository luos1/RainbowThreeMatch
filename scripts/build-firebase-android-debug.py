#!/usr/bin/env python3
"""Export the Unity 2022.3 Android player and build it with the API 34 toolchain."""
import argparse
import os
from pathlib import Path
import re
import shutil
import subprocess
import tempfile
import xml.etree.ElementTree as ET

AGP_VERSION = "8.1.4"
COMPILE_SDK = 34
BUILD_TOOLS = "34.0.0"
FIREBASE_ARTIFACTS = ("firebase-app-unity", "firebase-auth-unity", "firebase-firestore-unity")


def prepare_export(export):
    root_build = export / "build.gradle"
    build = root_build.read_text()
    for plugin in ("application", "library"):
        pattern = rf"(id 'com\.android\.{plugin}' version ')[^']+'"
        build, count = re.subn(pattern, rf"\g<1>{AGP_VERSION}'", build)
        if count != 1:
            raise ValueError(f"Unity export is missing the expected Android {plugin} plugin declaration")
    root_build.write_text(build)

    unity_library = export / "unityLibrary"
    dependencies = (unity_library / "build.gradle").read_text()
    if any(f"com.google.firebase:{artifact}:" not in dependencies for artifact in FIREBASE_ARTIFACTS):
        raise ValueError("Unity export is missing Firebase App, Auth or Firestore dependencies")
    if not any(
        ET.parse(resource).findtext("./string[@name='google_app_id']")
        for resource in unity_library.rglob("google-services.xml")
    ):
        raise ValueError("Unity export is missing the Firebase Android app resource")
    # Unity 2022.3 leaves package declarations in exported manifests. AGP 8
    # requires the namespace to be set in the Gradle DSL instead.
    for module in (export / "launcher", unity_library, *unity_library.glob("*.androidlib")):
        legacy_library = module.suffix == ".androidlib"
        manifest = module / ("AndroidManifest.xml" if legacy_library else "src/main/AndroidManifest.xml")
        manifest_xml = manifest.read_text()
        package = ET.fromstring(manifest_xml).get("package", "")
        if not re.fullmatch(r"[A-Za-z_][A-Za-z0-9_.]*", package):
            raise ValueError(f"Unity export is missing a manifest package for {module.name}")
        module_build = module / "build.gradle"
        build = module_build.read_text()
        if legacy_library:
            build = build.replace("apply plugin: 'android-library'", "apply plugin: 'com.android.library'")
        build, count = re.subn(
            r"^(\s*)compileSdkVersion\s+\d+", rf"\g<1>namespace '{package}'\n    compileSdkVersion {COMPILE_SDK}",
            build, flags=re.M,
        )
        if count != 1:
            raise ValueError(f"Unity export is missing the compile SDK declaration for {module.name}")
        build = re.sub(r"\bbuildToolsVersion\s+['\"][^'\"]+['\"]", f"buildToolsVersion '{BUILD_TOOLS}'", build)
        module_build.write_text(build)
        manifest.write_text(re.sub(r"\s+package=(['\"])[^'\"]+\1", "", manifest_xml, count=1))

    # Unity points at its bundled SDK. Let the Android plugin use ANDROID_HOME.
    (export / "local.properties").unlink(missing_ok=True)


def run_stage(command, env, log, console_log=None):
    with (console_log or log).open("wb") as output:
        result = subprocess.run(command, env=env, stdout=output, stderr=subprocess.STDOUT)
    log.with_suffix(".exit").write_text(f"{result.returncode}\n")
    print(f"{log.name}: exit {result.returncode} ({log})", flush=True)
    return result.returncode


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--unity", required=True, type=Path, help="Unity 2022.3 Editor executable with Android support")
    parser.add_argument("--sdk", required=True, type=Path, help="Android SDK containing API 34 and Build Tools 34.0.0")
    parser.add_argument("--jdk", required=True, type=Path, help="JDK 17 installation directory")
    parser.add_argument("--gradle", required=True, type=Path, help="Gradle 8.0.2 executable")
    args = parser.parse_args()
    if not os.environ.get("RAINBOW_GOOGLE_WEB_CLIENT_ID", "").strip():
        parser.error("Export RAINBOW_GOOGLE_WEB_CLIENT_ID before starting the build")
    if not (args.sdk / f"platforms/android-{COMPILE_SDK}/android.jar").is_file() or not (
        args.sdk / f"build-tools/{BUILD_TOOLS}/apksigner"
    ).is_file():
        parser.error("Install Android SDK Platform 34 and Build Tools 34.0.0")

    os.umask(0o077)
    repo = Path(__file__).resolve().parents[1]
    root = repo / "Build" / "firebase-android"
    root.mkdir(parents=True, exist_ok=True)
    build_dir = Path(tempfile.mkdtemp(prefix="debug-", dir=root))
    export = build_dir / "gradle-project"
    env = os.environ.copy()
    env.update(RAINBOW_ANDROID_EXPORT=str(export), GRADLE_USER_HOME=str(root / "gradle-cache"))
    print(f"Build directory: {build_dir}", flush=True)
    code = run_stage(
        [args.unity.resolve(), "-batchmode", "-nographics", "-quit", "-buildTarget", "Android",
         "-projectPath", repo / "Rainbow", "-executeMethod", "RainbowDevelopmentBuild.ExportFirebaseAndroid",
         "-logFile", build_dir / "unity.log"],
        env, build_dir / "unity.log", build_dir / "unity-console.log",
    )
    if code:
        return code

    try:
        prepare_export(export)
    except (OSError, ET.ParseError, ValueError) as error:
        parser.exit(1, f"Could not configure Unity export: {error}\n")

    android_home = root / "android-user-home"
    android_home.mkdir(exist_ok=True)
    env.update(ANDROID_HOME=str(args.sdk.resolve()), ANDROID_SDK_ROOT=str(args.sdk.resolve()),
               ANDROID_USER_HOME=str(android_home), JAVA_HOME=str(args.jdk.resolve()))
    code = run_stage([args.gradle.resolve(), "--no-daemon", "--console=plain", "-p", export,
                      ":launcher:assembleDebug"], env, build_dir / "gradle.log")
    if code:
        return code
    apk = build_dir / "Rainbow-debug.apk"
    shutil.copyfile(export / "launcher/build/outputs/apk/debug/launcher-debug.apk", apk)
    print(f"Debug APK: {apk}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

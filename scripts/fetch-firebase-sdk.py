#!/usr/bin/env python3
"""Fetch only the four pinned public SDK archives; never fetch project config."""
import argparse
import hashlib
import json
from pathlib import Path
import shutil
import tempfile
import urllib.request

PACKAGES = {
    "com.google.external-dependency-manager-1.2.190": "9a326599dab4e6bcaf13cd267689cec9e302abe213929335b3d0f93083b998de",
    "com.google.firebase.app-14.2.0": "94b713e94514e3ef1c17143be64009ed9e8c7c9221b4c6078b33e7a3c381372b",
    "com.google.firebase.auth-14.2.0": "c1bc03810acf318a8d98e5decb1b764bcb15c5f65bbe7c934308b815ded8325d",
    "com.google.firebase.firestore-14.2.0": "49cd9e16489de4b69134ec5d3a106a5a565f4fd2f21b8a028c39616615f0c016",
}


def digest(path):
    checksum = hashlib.sha256()
    with path.open("rb") as archive:
        for chunk in iter(lambda: archive.read(1024 * 1024), b""):
            checksum.update(chunk)
    return checksum.hexdigest()


def fetch(target):
    target.mkdir(parents=True, exist_ok=True)
    for archive, expected in PACKAGES.items():
        destination = target / (archive + ".tgz")
        if destination.exists() and digest(destination) == expected:
            print("Verified " + destination.name)
            continue
        package = archive.rsplit("-", 1)[0]
        url = f"https://dl.google.com/games/registry/unity/{package}/{archive}.tgz"
        with tempfile.NamedTemporaryFile(dir=target, suffix=".download", delete=False) as download:
            temporary = Path(download.name)
            try:
                with urllib.request.urlopen(url, timeout=120) as response:
                    shutil.copyfileobj(response, download)
                download.close()
                if digest(temporary) != expected:
                    raise RuntimeError("SHA-256 mismatch: " + archive)
                temporary.replace(destination)
            finally:
                temporary.unlink(missing_ok=True)
        print("Downloaded and verified " + destination.name)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--disable", action="store_true", help="Remove only the pinned Firebase entries locally")
    args = parser.parse_args()
    project = Path(__file__).resolve().parents[1] / "Rainbow"
    manifest_path = project / "Packages" / "manifest.json"
    manifest = json.loads(manifest_path.read_text())
    dependencies = manifest["dependencies"]
    pinned = {archive.rsplit("-", 1)[0]: f"file:../FirebasePackages/{archive}.tgz" for archive in PACKAGES}
    for package, path in pinned.items():
        if package in dependencies and dependencies[package] != path:
            raise RuntimeError(f"Resolve the existing {package} manifest entry before continuing")
    if args.disable:
        for package in pinned:
            dependencies.pop(package, None)
    else:
        fetch(project / "FirebasePackages")
        dependencies.update(pinned)
    manifest_path.write_text(json.dumps(manifest, indent=2) + "\n")


if __name__ == "__main__":
    main()

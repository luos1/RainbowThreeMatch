# Firebase Unity SDK import: pinned, local, and reversible

This repository deliberately keeps Firebase optional. Production code is
guarded by `RAINBOW_FIREBASE`; without that scripting define and the packages,
the project builds against local fakes. Do not add a cloud project, log in with
the Firebase CLI, enable billing, or commit a Firebase configuration file as
part of this import.

## Verified package set

The following official Google Unity Package Manager archives were fetched and
their package manifests and SHA-256 values checked on 2026-11-26. The
tracked `scripts/fetch-firebase-sdk.py` verifies each SHA-256, then adds local
`file:../FirebasePackages/...` manifest references. Unity 2022.3 rejected
HTTPS tarball values in this project. The manifest in Git has no Firebase
entries so a clean checkout can open without the optional SDK. The package
archives and all project configuration stay local.

| Archive | Version | SHA-256 |
| --- | --- | --- |
| `com.google.external-dependency-manager-1.2.190.tgz` | 1.2.190 | `9a326599dab4e6bcaf13cd267689cec9e302abe213929335b3d0f93083b998de` |
| `com.google.firebase.app-14.2.0.tgz` | 14.2.0 | `94b713e94514e3ef1c17143be64009ed9e8c7c9221b4c6078b33e7a3c381372b` |
| `com.google.firebase.auth-14.2.0.tgz` | 14.2.0 | `c1bc03810acf318a8d98e5decb1b764bcb15c5f65bbe7c934308b815ded8325d` |
| `com.google.firebase.firestore-14.2.0.tgz` | 14.2.0 | `49cd9e16489de4b69134ec5d3a106a5a565f4fd2f21b8a028c39616615f0c016` |

Download URLs are deliberately versioned:

1. <https://dl.google.com/games/registry/unity/com.google.external-dependency-manager/com.google.external-dependency-manager-1.2.190.tgz>
2. <https://dl.google.com/games/registry/unity/com.google.firebase.app/com.google.firebase.app-14.2.0.tgz>
3. <https://dl.google.com/games/registry/unity/com.google.firebase.auth/com.google.firebase.auth-14.2.0.tgz>
4. <https://dl.google.com/games/registry/unity/com.google.firebase.firestore/com.google.firebase.firestore-14.2.0.tgz>

Firebase App requires EDM4U 1.2.190. Firebase Auth and Firestore each require
Firebase App 14.2.0. The EDM4U package's own manifest calls its package name
deprecated in favour of a Unity-owned replacement; retain 1.2.190 here because
it is the exact dependency declared by the Firebase packages. Do not import a
separate EDM4U `.unitypackage` alongside these tarballs.

## Import procedure

1. Close the Unity project and run `python3 scripts/fetch-firebase-sdk.py` from
   the repository root. It adds the relative tarball dependencies only after
   all four hashes pass. If downloading fails, the manifest remains as it was.
2. Open `Rainbow/` in Unity 2022.3.8f1. Confirm Package Manager resolved all
   four archives and check for native dependency or Gradle-template errors.
3. The Firestore UPM version defines `RAINBOW_FIREBASE` in `Rainbow.Game`.
   For a local-only checkout, leave the script unrun or close Unity and run
   `python3 scripts/fetch-firebase-sdk.py --disable`. Reopen Unity to refresh
   the package lock. Keep the SDK-enabled manifest and lock changes local.
4. For real Firebase builds, put the appropriate owner-provided configuration
   file at the root of `Rainbow/Assets/`. Unity/Firebase generate additional
   platform files from it. Git ignores the config and generated desktop copy.

Firebase's current official Unity Google Sign-In guide describes a different
`GoogleSignInNative` package which includes Credential Manager dependencies.
Do not import it in this project: UniversalSDK remains the only native Google
sign-in UI, and its ID token is the input to
`GoogleAuthProvider.GetCredential(idToken, null)`.

## Compatibility and validation boundary

Firebase Unity 14.2.0 is current as of 2026-11-26. Firebase 14.0.0 requires
Unity 2022.3+ and Android `minSdkVersion` 24; this Unity project meets the
editor and Android requirements. Firebase 13 raised the iOS minimum
deployment target to 15, which this project sets for Firebase compatibility.
Firebase's standalone GoogleSignInNative guide currently asks for Android API
34, but it is excluded for the UniversalSDK compatibility reason above.

No stable installed-size budget is published for these products. Measure the
generated development APK/AAB and iOS archive after a real import rather than
guessing a size. The Firebase config files are needed only for actual device
integration; their absence must keep the guarded build and fake tests usable.

## Primary sources

- [Firebase: additional Unity installation options](https://firebase.google.com/docs/unity/setup-alternative)
- [Google APIs for Unity package archive](https://developers.google.com/unity/archive)
- [Firebase Unity release notes](https://firebase.google.com/support/release-notes/unity)
- [Firebase Unity Google Sign-In](https://firebase.google.com/docs/auth/unity/google-signin)

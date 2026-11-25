# Firebase Unity SDK import: pinned, local, and reversible

This repository deliberately keeps Firebase optional. Production code is
guarded by `RAINBOW_FIREBASE`; without that scripting define and the packages,
the project builds against local fakes. Do not add a cloud project, log in with
the Firebase CLI, enable billing, or commit a Firebase configuration file as
part of this import.

## Verified package set

The following official Google Unity Package Manager archives were fetched and
their package manifests and SHA-256 values checked on 2026-11-26. The committed
`Rainbow/Packages/manifest.json` pins the exact official HTTPS URLs, so a clean
clone resolves the same package set. `Rainbow/FirebasePackages/` is an ignored
local download cache only and is intentionally absent from Git.

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

1. Open the cloned project in Unity 2022.3.8f1. Unity Package Manager resolves
   the four HTTPS-pinned entries in `Rainbow/Packages/manifest.json`; no manual
   `file:` dependency is needed or should be committed.
2. Reopen after resolution and confirm the Console has no duplicate Google
   Sign-In, Gradle-template, or iOS resolver errors.
3. `Rainbow.Game.asmdef` enables `RAINBOW_FIREBASE` only when the pinned
   Firestore package is present. If Package Manager cannot resolve it, the
   guarded implementation remains excluded and fake-based tests still compile.
4. To repair an offline resolution failure, fetch the exact four archives above
   into ignored `Rainbow/FirebasePackages/`, check their hashes, then use
   Package Manager's **Add package from tarball** in the listed order. Do not
   change the committed HTTPS pins to local paths.
5. When the owner supplies them, place `google-services.json` and/or
   `GoogleService-Info.plist` locally as Firebase specifies. They stay ignored;
   verify with `git status --ignored` that they cannot enter a commit.

Firebase's current official Unity Google Sign-In guide describes a different
`GoogleSignInNative` package which includes Credential Manager dependencies.
Do not import it in this project: UniversalSDK remains the only native Google
sign-in UI, and its ID token is the input to
`GoogleAuthProvider.GetCredential(idToken, null)`.

## Compatibility and validation boundary

Firebase Unity 14.2.0 is current as of 2026-11-26. Firebase 14.0.0 requires
Unity 2022.3+ and Android `minSdkVersion` 24; this Unity project meets the
editor requirement but its Android minimum level must be updated before an
SDK-enabled APK. Firebase 13 raised the iOS minimum deployment target to 15.
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

# Firebase decision: Google account and cloud save

## Verdict

**Use Firebase Authentication and Cloud Firestore for this first cloud-save
release.**  It supplies the requested Google credential exchange, anonymous
identity, account linking, authenticated owner-only data access, and mobile SDKs
without a custom token-verification or session service.  This is the lighter
choice for one private `users/{uid}` record per player on the free Spark plan.
It does not validate that a player earned a submitted score; a competitive
leaderboard still needs server-authoritative validation.

The existing UniversalSDK Google flow remains the only native sign-in provider.
Pass its ID token to `GoogleAuthProvider.GetCredential(...)` rather than
discarding it. Firebase's current Unity Google guide uses a separate
GoogleSignInNative plug-in which imports Credential Manager dependencies; do
not add that alongside UniversalSDK. Keep an adapter boundary, and evaluate a
single-provider Credential Manager migration before Android release.

## Firebase versus the HTTPS alternative

| Concern | Firebase Auth + Firestore | HTTPS service in `google-sign-in.md` |
| --- | --- | --- |
| Google login | Exchanges the native ID token for a Firebase credential and UID. | Must verify issuer, signature, expiry and audience, then issue/refresh a service session. |
| Guest plus linking | `SignInAnonymouslyAsync` then `LinkWithCredentialAsync` preserves the anonymous account's protected data. | Needs a guest-session format and an atomic account-migration design. |
| Credential conflict | `CredentialAlreadyInUse` is a typed outcome; retain guest data and show recovery. | Must define collision, recovery and merge behavior. |
| Authorization | Rules can limit `users/{uid}` to `request.auth.uid == uid`. | Must implement endpoint and database authorization. |
| Operations | No service deployment for this scope. | Requires a dedicated host, secret store, monitoring and database. |
| Integrity | Rules can reject a lower score, but cannot prove gameplay. | Same unless the service validates gameplay. |

Firebase reduces custom security and operational code for a non-competitive
personal-best save. Keep the HTTPS route for a future server-authoritative
leaderboard, custom backend behavior, or requirements beyond Spark limits.

## Auth and merge contract

1. Initialize `FirebaseApp`, then use `FirebaseAuth.DefaultInstance`.
2. Guest start uses `SignInAnonymouslyAsync`; that Firebase UID owns its data.
3. Google login builds a credential from the UniversalSDK ID token. Signed-out
   players call `SignInWithCredentialAsync`; anonymous players call
   `LinkWithCredentialAsync`.
4. `CredentialAlreadyInUse` / `FirebaseAccountLinkException` is a visible link
   conflict. Do not clear the guest cache or silently merge two identities.
   Require sign-in to the existing Google account before a deliberate merge.
5. Linking merges guest cloud/local data with the signed-in account using maximum
   high score and newest `updatedAt` for other fields.

Firebase documents a current account-linking known issue. Device tests must
exercise linking and conflict recovery before release.

## Firestore security boundary

The first ruleset is owner-only and monotonic for the score. `request.resource`
is the proposed document state, allowing the update comparison. Production
rules must also restrict fields and timestamp types.

```rules
rules_version = '2';
service cloud.firestore {
  match /databases/{database}/documents {
    match /users/{uid} {
      allow read: if request.auth != null && request.auth.uid == uid;
      allow create: if request.auth != null && request.auth.uid == uid
                    && request.resource.data.highScore is int
                    && request.resource.data.highScore >= 0;
      allow update: if request.auth != null && request.auth.uid == uid
                    && request.resource.data.highScore is int
                    && request.resource.data.highScore >= resource.data.highScore;
      allow delete: if false;
    }
  }
}
```

Cloud save uses a Firestore transaction: `highScore = max(local, cloud)`;
non-score fields choose newest `updatedAt`. Rules are access control, not
anti-cheat.

## Spark constraints

Use a dedicated Rainbow project on **Spark only**. Google and anonymous Auth
are no-cost options. One Firestore database gets 50,000 reads/day, 20,000
writes/day, 20,000 deletes/day, 1 GiB storage and 10 GiB/month outbound data.
If a Spark quota is exceeded, that product is disabled until the next cycle;
there is no paid overage. Linking a Cloud Billing account upgrades the whole
project to Blaze, so do not link billing. Cloud Functions, extensions and a
backend are explicitly out of scope.

## Unity package decision (verified 2026-11-26)

Pin **14.2.0** for every Firebase Unity package. The official archive lists it
for Unity 2022.1+; release notes set Unity 2022 LTS, Android API 24+ and iOS
15+ as the current major-version floor. This project uses Unity 2022.3.8f1,
but its Android minimum API 22 must be raised before an SDK-enabled Android
build.

The downloaded tarballs below were checked on 2026-11-26: their package
manifests identify the stated versions and their SHA-256 hashes are recorded in
[`firebase-sdk-import.md`](firebase-sdk-import.md). Firebase 14.0.0 raised the
core SDK's Android `minSdkVersion` to 24. The newer, separate
`GoogleSignInNative` integration guide asks for Android API 34, but that guide's
plugin is deliberately excluded here because UniversalSDK remains this project's
single native Google UI.

Use UPM tarballs in this order and do not mix them with Firebase
`.unitypackage` imports:

1. [External Dependency Manager 1.2.190](https://dl.google.com/games/registry/unity/com.google.external-dependency-manager/com.google.external-dependency-manager-1.2.190.tgz)
2. [Firebase App 14.2.0](https://dl.google.com/games/registry/unity/com.google.firebase.app/com.google.firebase.app-14.2.0.tgz)
3. [Firebase Auth 14.2.0](https://dl.google.com/games/registry/unity/com.google.firebase.auth/com.google.firebase.auth-14.2.0.tgz)
4. [Firebase Firestore 14.2.0](https://dl.google.com/games/registry/unity/com.google.firebase.firestore/com.google.firebase.firestore-14.2.0.tgz)

The official docs do not publish a stable installed-size budget per product.
Do not invent one; after dependency resolution, record the AAB/APK and iOS
archive deltas. The committed Unity manifest pins the official HTTPS tarball
URLs, which makes a clean clone reproducible without vendoring large binaries.
The ignored `Rainbow/FirebasePackages/` directory is only a local download cache.
If the packages cannot be downloaded, compile with `RAINBOW_FIREBASE` absent and
use fakes; the import document states how to restore the exact tarballs before
device cloud login.

The Firebase packages declare their dependency on the pinned EDM4U package.
That EDM4U package manifest currently marks itself deprecated in favour of a
Unity-owned package name, but it remains the dependency requested by Firebase
14.2.0; do not substitute it without validating the resolved Android/iOS
dependencies.

## Config and iOS audience caveat

Repository policy excludes `google-services.json` and `GoogleService-Info.plist`.
Obtain them locally from the owner and never commit them. Firebase's Unity
Google guide requires the Web client ID as the ID-token `aud` on **both** Android
and iOS. Existing UniversalSDK iOS setup uses an iOS client ID, so inspect a
real signed-device token before enabling Firebase exchange there. If its `aud`
is not the Firebase Web client ID, update the native configuration or migrate
the bridge; do not weaken audience validation.

## Official sources

- [Google sign-in for Unity](https://firebase.google.com/docs/auth/unity/google-signin): Firebase credential exchange, Web client-ID audience, Credential Manager plugin.
- [Anonymous Auth for Unity](https://firebase.google.com/docs/auth/unity/anonymous-auth) and [account linking](https://firebase.google.com/docs/auth/unity/account-linking): guest UID, linking and known-issue warning.
- [Firebase Auth Unity API](https://firebase.google.com/docs/reference/unity/namespace/firebase/auth): `CredentialAlreadyInUse`.
- [Firestore rule conditions](https://firebase.google.com/docs/firestore/security/rules-conditions): owner rules and `request.resource` comparisons.
- [Firebase pricing plans](https://firebase.google.com/docs/projects/billing/firebase-pricing-plans) and [Firestore pricing](https://firebase.google.com/docs/firestore/pricing): Spark behavior and quotas.
- [Unity setup](https://firebase.google.com/docs/unity/setup), [UPM installation](https://firebase.google.com/docs/unity/setup-alternative), [Unity package archive](https://developers.google.com/unity/archive), and [Unity release notes](https://firebase.google.com/support/release-notes/unity): package install order, version and platform support.
- [Credential Manager](https://developer.android.com/identity/sign-in/credential-manager-siwg): Android migration direction.

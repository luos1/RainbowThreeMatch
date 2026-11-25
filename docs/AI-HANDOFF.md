# RainbowThreeMatch AI handoff

RainbowThreeMatch uses Google sign-in before entering the game. The project owner has decided that the infrastructure must be **new and dedicated to RainbowThreeMatch**. Another AI or team will create it. This handoff covers the Unity changes, what was executed, and what the infrastructure owner needs to supply. Google is the selected identity provider. The first release platform, final application identifiers, cloud and storage provider, and score policy remain open.

## Repository and branch order

The repository is [luos1/RainbowThreeMatch](https://github.com/luos1/RainbowThreeMatch). The Unity project is `Rainbow/` within the checkout. At handoff, both PRs were open and unmerged.

| PR | Base at handoff | Feature branch and verified code commit |
| --- | --- | --- |
| [#1 Gameplay and verification](https://github.com/luos1/RainbowThreeMatch/pull/1) | `main` at `0634f16` | `rainbow-three-match-local` at `1a18322` with gameplay commits `6c52097`, `5776cfa`, `1a18322` |
| [#2 Google sign-in](https://github.com/luos1/RainbowThreeMatch/pull/2) | `rainbow-three-match-local` at `1a18322` | `rainbow-google-sign-in` with verified implementation `1207616ff1163360f184fe326ed08985f942ccd1` and the follow-up handoff documentation |

Review #2 against #1's head to see the authentication changes. A comparison against `main` also includes the gameplay work from #1. Fetch the latest remote refs and recheck both PRs before touching the stack. A fresh checkout of #2 already contains #1's commits. The original Orca workspace also has a local `.gitignore` change, `.claude-flow/`, and `LOCAL-VALIDATION.md` from the prior work. Preserve those if using that workspace. GitHub Issues was disabled when checked. The owner controls merge and deployment approval.

```sh
# Use a fresh checkout if the external AI does not have the Orca workspace.
git clone --branch rainbow-google-sign-in https://github.com/luos1/RainbowThreeMatch.git
cd RainbowThreeMatch
git status --short --branch
git log --oneline origin/rainbow-three-match-local..HEAD
```

## Environment and reproducible checks

| Item | On the validation Mac |
| --- | --- |
| Orca checkout | `$HOME/orca/workspaces/game/https-github.com-luos1-RainbowThreeMatch` |
| Unity project | `Rainbow/` inside the checkout |
| Unity Hub | 4.0.2 ARM64 at `/Applications/Unity Hub.app` |
| Official Unity CLI | 1.0.1 at `$HOME/.unity/bin/unity` |
| Unity Editor | 2022.3.8f1 ARM64, revision `b5eafc012955`, at `/Volumes/Data/Unity/Hub/Editor/2022.3.8f1/Unity.app` |
| Mobile toolchains | The validation Editor had Mac Standalone support. Android/iOS build modules and the iPhoneOS SDK were unavailable there. |

Use the pinned Editor from [ProjectVersion.txt](../Rainbow/ProjectSettings/ProjectVersion.txt), an active Unity license, and the installed binary path for the current machine. Run the following from the repository root. The Unity CLI manages installations. The Editor itself runs the checks. [Official Unity CLI](https://docs.unity.com/en-us/unity-cli/use-unity-cli) · [Unity Test Framework CLI](https://docs.unity3d.com/Packages/com.unity.test-framework@1.1/manual/reference-command-line.html)

```sh
"$HOME/.unity/bin/unity" --version
"$HOME/.unity/bin/unity" editors --installed
RAINBOW_UNITY='/Volumes/Data/Unity/Hub/Editor/2022.3.8f1/Unity.app/Contents/MacOS/Unity'
RAINBOW_RESULTS="$(mktemp -d "${TMPDIR:-/tmp}/rainbow-validation.XXXXXX")"
"$RAINBOW_UNITY" -batchmode -nographics -quit -buildTarget StandaloneOSX \
  -projectPath "$PWD/Rainbow" -logFile "$RAINBOW_RESULTS/import.log"
"$RAINBOW_UNITY" -batchmode -runTests -testPlatform PlayMode \
  -projectPath "$PWD/Rainbow" -testResults "$RAINBOW_RESULTS/playmode.xml" \
  -logFile "$RAINBOW_RESULTS/playmode.log"
```

The implementation commit passed import and compilation with exit 0. Play Mode returned exit 0 and **25 passed, 0 failed**. The suite comprised 12 match-rule cases, two game-scene cases, five sign-in scene cases and six SDK callback cases. Real Unity scene loads and EventSystem raycasts covered startup, sign-in eligibility, lobby and Play. A sign-in test double provided success, cancellation and failure. Synthetic payloads exercised SDK callback parsing, and the real adapter exercised the unsupported Editor path. The authentication fixtures are [GoogleSignInSceneTests](../Rainbow/Assets/Tests/PlayMode/GoogleSignInSceneTests.cs) and [GoogleSignInCallbackTests](../Rainbow/Assets/Tests/PlayMode/GoogleSignInCallbackTests.cs). Android/iOS C# branches passed static compilation. The Android Gradle configuration and the iOS bridge received static checks. The mobile packages, live Google OAuth, and account score storage were unverified. Original XML and logs stay in the source workspace under `.omx/validation/rainbow/auth/`.

## Code and integration points

| Area | Current behavior and where to extend |
| --- | --- |
| Login and scenes | [Login.cs](../Rainbow/Assets/Scripts/Login.cs) handles status, duplicate clicks, cancellation, timeout and failures. [Loading.cs](../Rainbow/Assets/Scripts/UI/Loading.cs) moves `00.Loading` to `01.Lobby` after sign-in. [Lobby.cs](../Rainbow/Assets/Scripts/UI/Lobby.cs) requires a local session and opens `02.Game`. Sign-out returns to sign-in. |
| SDK response and session | [GoogleSignIn.cs](../Rainbow/Assets/Scripts/GoogleSignIn.cs) calls UniversalSDK for Android/iOS. `Complete` checks that the provider supplied both a user ID and an ID token, then sends the ID to the local login flow. [UserData.cs](../Rainbow/Assets/Scripts/UserData.cs) holds only the account ID in memory. A new app process starts signed out. The ID token is discarded. There is no backend session, restored credential, guest route, or durable score API. |
| Future score submission | [Game.cs](../Rainbow/Assets/Scripts/Game.cs) owns `Score`. `Update` marks the round complete when the timer expires. To add persistence, capture the final score there before `OnClickRetry()` resets it. Bind submission and any retry queue to the authenticated account used when the round started. Keep networking outside board and scoring rules. |
| Mobile build config | Android uses [launcherTemplate.gradle](../Rainbow/Assets/Plugins/Android/launcherTemplate.gradle). iOS uses [PlistUpdating.cs](../Rainbow/Assets/UniversalSDK/Editor/PlistUpdating.cs). See the platform workflow below and the [detailed SDK setup](google-sign-in.md). |

The two identifiers in [ProjectSettings.asset](../Rainbow/ProjectSettings/ProjectSettings.asset) are both `com.DefaultCompany.com.unity.template.mobile2D`. Replace the relevant placeholder with the owner-approved final package or bundle ID before registering OAuth clients and signing a test build.

## Dedicated infrastructure and setup order

1. Obtain the decisions and scoped admin access listed below. Provision a new RainbowThreeMatch-only Google Cloud project and Google Auth Platform app. Configure a test audience and approved test accounts. Put OAuth registrations, any backend service, and a new dedicated database or storage resource under RainbowThreeMatch ownership. Use project-scoped IAM and independent secrets, data, service accounts and billing controls. Keep the Fitness Supabase project and all other projects, databases, keys and user data separate. If using Firebase, enable it on the new Rainbow-owned project.
2. Register clients and inject their **public client IDs** into the build process. Use the platform toolchain and test device provisioned externally. The Unity code already reads the following variables and does not require an OAuth client secret in the player. [Android client authentication](https://developers.google.com/android/guides/client-auth) · [iOS Google Sign-In setup](https://developers.google.com/identity/sign-in/ios/start-integrating)

   | Platform | Register | Inject and inspect |
   | --- | --- | --- |
   | Android | Android OAuth client for the final package and **SHA-1 of the certificate that signs the installed APK**. Also create a Web OAuth client in the same Rainbow project. For Play App Signing, use the Play app-signing certificate SHA-1 for the Play build. Register the test signer separately if different. | Supply the **Web** client ID as `RAINBOW_GOOGLE_WEB_CLIENT_ID` in the environment of Unity or the exported Gradle build. Gradle writes the `google_web_client_id` string consumed by UniversalSDK 3.0.5. Check the merged resource and resolved SDK dependency graph. |
   | iOS | iOS OAuth client for the final bundle ID. Obtain the authorized Apple signing team, certificate and provisioning profile. | Supply the **iOS** client ID as `RAINBOW_GOOGLE_IOS_CLIENT_ID` before launching Unity to export Xcode. An enabled local UniversalSDK settings asset is the fallback. The postprocessor sets `GoogleClientID` and `GIDClientID`, derives `com.googleusercontent.apps.<client-prefix>` and adds it to `CFBundleURLTypes` → `CFBundleURLSchemes`. Verify the generated Info.plist and the callback on device. |

   On the provisioned build host, inject the value from CI configuration or export the variable in the same shell before launching the Editor executable with `-projectPath "$PWD/Rainbow"`. A Hub or Editor process already running may retain its previous environment. Keep OAuth client secrets, signing private keys or keystores, service credentials and database credentials in the dedicated CI/cloud secret store or the platform signing store. Grant scoped access. Keep provider tokens, personal data and signing material out of the repository, shipped player files, chat, and build logs.
3. Prove native Google sign-in on a signed build first. Cancel once, retry, choose a permitted Google test account, return to the app, reach the lobby, start a game, sign out and sign in again. Test a failed request and confirm it leaves the app at sign-in with a retry option. A test-double run, consent-screen screenshot or static compile cannot establish this result. Verify the legacy Android UniversalSDK compatibility and plan Credential Manager migration if required for the selected release. See the [SDK notes and Apple release caveats](google-sign-in.md#google-oauth-test-environment).
4. After choosing a score backend, extend `GoogleSignIn.Complete` (or its caller) to hand the native ID token directly to the selected session exchange over HTTPS before discarding it. Keep the Unity account ID local. For server access, authenticate every request using the service session and route reads and writes to the verified account. Establish behavior for exchange failure, expiration, sign-out, offline retry and account switching before activating cloud saves. Place a score submission at completed-round capture, never on a UI score event fired for each match.

   - **Firebase Auth and Firestore**. In the dedicated Rainbow project, register the selected mobile app and enable the Google provider. Exchange the Google credential through Firebase Auth, then use the Firebase UID for owner-only Firestore access. Keep a single personal-best record per account and apply transactional max updates with ownership and score validation in Security Rules. Review the native token audience and SDK dependency impact with the owner before adopting a Firebase client package. [Firebase Unity Google Sign-In](https://firebase.google.com/docs/auth/unity/google-signin) · [Firebase Security Rules and Auth](https://firebase.google.com/docs/rules/rules-and-auth)
   - **Separate HTTPS service and dedicated store**. Exchange the Google ID token using a supported verifier. Validate Google's signature with its rotating keys, allowed issuer, expiration and the expected audience for the actual native client configuration. Derive the stable account key from the verified `sub`, then issue a service session. Read and update scores under the session identity and enforce an atomic max operation. Check the native iOS token audience before selecting a server-client-ID strategy. The current iOS exporter supplies the iOS sign-in ID. [Google ID token verification](https://developers.google.com/identity/gsi/web/guides/verify-google-id-token)
5. Prove cloud persistence with real accounts after the backend is connected. A higher completed score must survive relaunch on the same account. Verify a second device if cross-device sync is selected. A lower score or retry must preserve the existing best. A different account must see only its own score, and anonymous, invalid or expired sessions must be rejected. Decide separately how to verify results if the score is used for a public leaderboard.

## Owner decisions and required access

- [ ] First release platform and intended test/distribution channel. Each platform needs its own signing and verification evidence.
- [ ] Final Android package and/or iOS bundle ID and the matching signer or provisioning team.
- [ ] Rainbow project owners, Google Cloud admin access, test account list, app support contact and authorized secret store.
- [ ] Score storage model, cloud provider, environment/region, cost controls, and retention. Confirm whether scores are private, cross-device, or used in a leaderboard.
- [ ] How to pass public client IDs and signing references securely to the build host after external provisioning. Keep approvals for public OAuth consent and store release with the project owner.

## Paste into the receiving AI

> Continue `luos1/RainbowThreeMatch` from PR #2 (`rainbow-google-sign-in`, based on PR #1). Read `docs/AI-HANDOFF.md` and `docs/google-sign-in.md`. Provision new Rainbow-only Google OAuth and app infrastructure after confirming the open platform, app ID, owner access and storage decisions. Keep all Fitness and other service resources and data separate. Verify the signed mobile build with a real Google test account, then connect the approved score backend and prove account isolation and persistence. Report actual executions and pending items separately. Keep secrets in the dedicated credential store and leave publishing, merge and deployment to explicit owner approval.

# Firebase Spark setup for RainbowThreeMatch

The owner completes these steps in a dedicated RainbowThreeMatch project. Use Spark throughout and leave Cloud Billing disconnected. Firebase provides private personal-best storage. The rules validate ownership and save shape and prevent score decreases. Competitive ranking would require server-side gameplay validation. [Firebase plans](https://firebase.google.com/docs/projects/billing/firebase-pricing-plans)

## 1. Register the mobile apps

1. In Unity 2022.3.8f1, open **Edit > Project Settings > Player > Other Settings > Identification** and set the approved Android Package Name and iOS Bundle Identifier. Both entries in Git currently use the Unity template identifier.
2. In the [Firebase console](https://console.firebase.google.com/), select **Create a project**, use a RainbowThreeMatch-specific project name, and keep the plan on **Spark**. Analytics is optional and this integration has no Analytics dependency.
3. Under **Project settings > General > Your apps**, register Android and iOS with those exact identifiers and the correct case. Under the Android app, add the SHA-1 of the certificate that signs the installed test APK. If you distribute with Google Play app signing, add the Play signing certificate fingerprint before device QA. Use the matching signing identity for iOS. [Firebase Unity app registration](https://firebase.google.com/docs/unity/setup) · [Android client authentication](https://developers.google.com/android/guides/client-auth)

## 2. Enable Authentication and install the SDK

1. In the Firebase console open **Build > Authentication > Get started > Sign-in method**, enable **Google** and **Anonymous**, and save each provider. Set the project support email for Google. Copy the Web client ID from the Google provider's **Web SDK configuration**. Confirm the iOS client ID and reversed client ID from the iOS app configuration. If Google Auth Platform is in Testing, add the approved test accounts. [Google provider configuration](https://firebase.google.com/docs/auth/unity/google-signin) · [Anonymous provider configuration](https://firebase.google.com/docs/auth/unity/anonymous-auth)
2. Download the refreshed **google-services.json** for Android and **GoogleService-Info.plist** for iOS from **Project settings > General > Your apps**. Place the files needed for your targets directly in `Rainbow/Assets/`. Keep the files, generated desktop config and Android Firebase resources, keystores, and credentials in local storage. Git ignores the standard paths and signing extensions.
3. With Unity closed, run `python3 scripts/fetch-firebase-sdk.py` from the repo root and open `Rainbow/` in Unity 2022.3.8f1. Keep both the overlay changes in `Rainbow/Packages/` and the archived packages local. Review the resolved native dependencies and use **Assets > External Dependency Manager > Android Resolver > Force Resolve** if necessary. Use UniversalSDK as the sole native Google entry point. [SDK import and versions](firebase-sdk-import.md)
4. Launch Unity from a process with `RAINBOW_GOOGLE_WEB_CLIENT_ID` set to the Web client ID. On iOS set `RAINBOW_GOOGLE_IOS_CLIENT_ID` to the iOS client ID as well. These are public identifiers. On iOS check the generated Xcode app's `Info.plist` for `GIDClientID`, `GIDServerClientID`, and the reversed callback scheme. Confirm a signed device receives a token accepted by Firebase for this project. The bundled UniversalSDK Google implementation requires device verification for the Web audience. Avoid adding a second Google native bridge to the existing app. [Google Sign-In iOS integration](https://developers.google.com/identity/sign-in/ios/start-integrating) · [Firebase Unity Google token audience](https://firebase.google.com/docs/auth/unity/google-signin)

## 3. Create Firestore and deploy the rules

1. In **Build > Firestore Database**, select **Create database**, choose **Standard edition**, the default database, and the intended data region. Start in **Production mode** with the default rules closed. The Spark quota applies to one qualifying database. [Create a Firestore database](https://firebase.google.com/docs/firestore/quickstart) · [Firestore quotas](https://firebase.google.com/docs/firestore/quotas)
2. From the repo root, using a Firebase CLI installation and the owner's authorized account, run:

   ```sh
   firebase login
   firebase deploy --only firestore:rules --project YOUR_RAINBOW_FIREBASE_PROJECT_ID
   ```

   Specify the dedicated Rainbow project explicitly. Verify the console's **Firestore > Rules** tab shows the deployed `firestore.rules`. The committed `firebase.json` targets rules. [Firebase CLI deployments](https://firebase.google.com/docs/cli) · [Firestore rules](https://firebase.google.com/docs/firestore/security/get-started)

## 4. Verify the build on a device

1. Build a debug-signed APK with the Android module, SDK, NDK and JDK for Unity 2022.3.8f1. iOS builds require the iOS module, Xcode, supported CocoaPods resolution and the authorized signing team.

   ```sh
   UNITY='/Volumes/Data/Unity/Hub/Editor/2022.3.8f1/Unity.app/Contents/MacOS/Unity'
   RAINBOW_DEV_APK="$HOME/rtm-logs/Rainbow-dev.apk" "$UNITY" \
     -batchmode -nographics -quit -buildTarget Android -projectPath "$PWD/Rainbow" \
     -executeMethod RainbowDevelopmentBuild.AndroidApk -logFile "$HOME/rtm-logs/android-build.log"
   ```

2. Verify online guest start, the first score write, app restart, and successful Google linking with an unused credential. Confirm the Firebase UID stays the same. Sign out, sign in as that Google account, and check the score on a second device.
3. Use a Google account that already has a save to verify the conflict screen. First dismiss and confirm the guest is still active, then select **기존 Google 계정에 합치기** and choose that account. Confirm the higher score is kept and newer metadata wins. Account transfer reads the guest record while the guest is still signed in.
4. During a round, disconnect the network after signing in. After the round, restore connectivity and enter Lobby again or use its sync retry action. Check that personal best is retained. If login or linking fails, retain the UID and account error details for diagnosis and omit tokens and credentials.
5. In the Firestore emulator or Rules Playground, check unauthenticated access, access to another UID, attempts to lower the score, and a valid owner update. Review quotas in Firebase Console before opening access to more testers.

The Editor and the SDK-free build use local guest storage. The SDK-free manifest allows compilation and fake-based testing offline. An SDK-enabled mobile build needs a matching local platform config and connectivity for initial Firebase sign-in. Firebase anonymous users should link a provider before signing out to recover their identity on another device. [Firebase anonymous account behavior](https://firebase.google.com/docs/auth/unity/anonymous-auth)

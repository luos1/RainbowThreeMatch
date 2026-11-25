
#if UNITY_IOS

using UnityEngine;
using System.Runtime.InteropServices;

namespace Universal.UniversalSDK
{
    public class NativeInterface
    {
        [DllImport("__Internal")]
        private static extern int universal_sdk_has_google_configuration();

        public static bool HasGoogleConfiguration() => Application.platform == RuntimePlatform.IPhonePlayer
            && universal_sdk_has_google_configuration() != 0;

        [DllImport("__Internal")]
        private static extern void universal_sdk_handle_google_url(string url);

        [DllImport("__Internal")]
        private static extern void universal_sdk_setup();
        public static void SetupSDK()
        {            
            if (!Application.isPlaying) { return; }
            if (IsInvalidRuntime(null)) { return; }
            
            universal_sdk_setup();
            Application.deepLinkActivated -= universal_sdk_handle_google_url;
            Application.deepLinkActivated += universal_sdk_handle_google_url;
        }

        [DllImport("__Internal")]
        private static extern void universal_sdk_login(string identifier,
                                                       int loginType);
        public static void Login(string identifier,
                                 LoginType loginType)
        {
            if (!Application.isPlaying) { return; }
            if (IsInvalidRuntime(identifier)) { return; }

            universal_sdk_login(identifier, (int)loginType);
        }

        [DllImport("__Internal")]
        private static extern void universal_sdk_logout();
        public static void Logout()
        {
            if (!Application.isPlaying) { return; }
            if (IsInvalidRuntime(null)) { return; }

            universal_sdk_logout();
        }
        
        [DllImport("__Internal")]
        private static extern void universal_sdk_openSafariView(string url);
        public static void OpenCustomTabView(string url)
        {
            if (!Application.isPlaying) { return; }
            if (IsInvalidRuntime(null)) { return; }

            universal_sdk_openSafariView(url);
        }

        private static bool IsInvalidRuntime(string identifier)
        {
            return Helpers.IsInvalidRuntime(identifier, RuntimePlatform.IPhonePlayer);
        }
    }
}
#endif
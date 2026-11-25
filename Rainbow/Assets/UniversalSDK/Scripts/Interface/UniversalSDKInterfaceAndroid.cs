
#if UNITY_ANDROID
using UnityEngine;

namespace Universal.UniversalSDK
{
    public class NativeInterface
    {
        private static AndroidJavaObject universalSdkWrapper;

        public static bool HasGoogleConfiguration()
        {
            if (Application.platform != RuntimePlatform.Android)
                return false;

            using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
            using var resources = activity.Call<AndroidJavaObject>("getResources");
            string packageName = activity.Call<string>("getPackageName");
            int resourceId = resources.Call<int>("getIdentifier", "google_web_client_id", "string", packageName);
            return resourceId != 0 && !string.IsNullOrWhiteSpace(resources.Call<string>("getString", resourceId));
        }

        public static void SetupSDK()
        {
            if (!Application.isPlaying) { return; }
            if (IsInvalidRuntime(null)) { return; }

            universalSdkWrapper ??= new AndroidJavaObject("com.universal.sdk.unitywrapper.UniversalSdkWrapper");
            universalSdkWrapper.Call("setupSDK");
        }

        public static void Login(string identifier,
                                 LoginType loginType)
        {
            if (!Application.isPlaying) { return; }
            if (IsInvalidRuntime(identifier)) { return; }

            object[] param = new object[2];
            param[0] = identifier;
            param[1] = (int)loginType;

            if (universalSdkWrapper != null)
                universalSdkWrapper.Call("login", param);
        }

        public static void Logout()
        {
            if (!Application.isPlaying) { return; }
            if (IsInvalidRuntime(null)) { return; }            

            if (universalSdkWrapper != null)
                universalSdkWrapper.Call("logout");
        }

        public static void OpenCustomTabView(string url)
        {
            if (!Application.isPlaying) { return; }
            if (IsInvalidRuntime(null)) { return; }

            object[] param = new object[1];
            param[0] = url;            

            if (universalSdkWrapper != null)
                universalSdkWrapper.Call("openCustomTabView", param);
        }       

        private static bool IsInvalidRuntime(string identifier)
        {
            return Helpers.IsInvalidRuntime(identifier, RuntimePlatform.Android);
        }
    }
}

#endif
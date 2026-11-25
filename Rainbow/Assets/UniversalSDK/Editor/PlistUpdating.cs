#if UNITY_IOS
using System;
using System.Text.RegularExpressions;
using UnityEditor.Build;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using System.IO;

namespace Universal.UniversalSDK.Editor
{
    public class PlistUpdating
    {
        [PostProcessBuildAttribute(1)]
        public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
        {
            if (target != BuildTarget.iOS)
            {
                return;
            }                        
            
            string plistPath = pathToBuiltProject + "/Info.plist";
            PlistDocument plist = new PlistDocument();
            plist.ReadFromString(File.ReadAllText(plistPath));

            PlistElementDict rootDict = plist.root;            

            SetupURLScheme(rootDict);
            SetupQueriesSchemes(rootDict);
            SetupFacebookSetting(rootDict);

            File.WriteAllText(plistPath, plist.WriteToString());
                        
        }

        static void SetupURLScheme(PlistElementDict rootDict)
        {
            string googleClientId = Environment.GetEnvironmentVariable("RAINBOW_GOOGLE_IOS_CLIENT_ID")?.Trim();
            if (string.IsNullOrEmpty(googleClientId) && UniversalSDKSettings.UseGoogleLogin)
                googleClientId = UniversalSDKSettings.GoogleClientID;

            bool useGoogle = !string.IsNullOrEmpty(googleClientId);
            if (!useGoogle && !UniversalSDKSettings.UseFacebookLogin)
                return;

            PlistElementArray array = GetOrCreateArray(rootDict, "CFBundleURLTypes");
            var lineURLScheme = array.AddDict();
            lineURLScheme.SetString("CFBundleTypeRole", "Editor");
            lineURLScheme.SetString("CFBundleURLName", "Client");
            var schemes = lineURLScheme.CreateArray("CFBundleURLSchemes");

            if (useGoogle)
            {
                const string suffix = ".apps.googleusercontent.com";
                if (!Regex.IsMatch(googleClientId, @"\A[A-Za-z0-9-]+\.apps\.googleusercontent\.com\z"))
                    throw new BuildFailedException("Configure a valid Google OAuth iOS client ID.");

                schemes.AddString("com.googleusercontent.apps." + googleClientId.Substring(0, googleClientId.Length - suffix.Length));
                rootDict.SetString("GoogleClientID", googleClientId);
                rootDict.SetString("GIDClientID", googleClientId);
            }

            if (UniversalSDKSettings.UseFacebookLogin)
                schemes.AddString("fb"+ UniversalSDKSettings.FacebookAppID);
        }

        static void SetupQueriesSchemes(PlistElementDict rootDict)
        {
            if (UniversalSDKSettings.UseFacebookLogin)
            {
                PlistElementArray array = GetOrCreateArray(rootDict, "LSApplicationQueriesSchemes");
                array.AddString("fbapi");
                array.AddString("fbauth2");
            }
        }

        static void SetupFacebookSetting(PlistElementDict rootDict)
        {
            if (UniversalSDKSettings.UseFacebookLogin)
            {
                rootDict.SetString("FacebookAppID", UniversalSDKSettings.FacebookAppID);
                rootDict.SetString("FacebookClientToken", UniversalSDKSettings.FacebookClientToken);
            }                
        }

        static PlistElementArray GetOrCreateArray(PlistElementDict dict, string key)
        {
            PlistElement array = dict[key];
            if (array != null)
            {
                return array.AsArray();
            }
            else
            {
                return dict.CreateArray(key);
            }
        }
    }
}
#endif
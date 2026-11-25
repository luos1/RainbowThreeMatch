using System;
using UnityEngine;
using UnityEngine.Scripting;

namespace Universal.UniversalSDK
{
    public class UniversalSDK : MonoBehaviour
    {
        static UniversalSDK instance;

        void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }


            else if (instance != this)
            {
                Destroy(gameObject);
                return;
            }
            DontDestroyOnLoad(gameObject);
            SetupSDK();
        }

        public static UniversalSDK Ins
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("UniversalSDK");
                    instance = go.AddComponent<UniversalSDK>();
                }
                return instance;
            }
        }

        void SetupSDK()
        {            
            NativeInterface.SetupSDK();
        }        

        public void Login(LoginType loginType,
                          Action<Result<LoginResult>> action)
        {            
            BeginLogin(loginType, action);
        }

        public Action BeginLogin(LoginType loginType, Action<Result<LoginResult>> action)
        {
            return UniversalAPI.BeginLogin(loginType, action);
        }

        public void Logout()
        {
            NativeInterface.Logout();
        }        
        
        public void OpenCustomTabView(string url)
        {
            NativeInterface.OpenCustomTabView(url);            
        }        

        [Preserve]
        public void OnApiOk(string result)
        {            
            UniversalAPI._OnApiOk(result);
        }

        [Preserve]
        public void OnApiError(string result)
        {            
            UniversalAPI._OnApiError(result);
        }
    }
}

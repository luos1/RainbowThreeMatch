using System;
using System.Collections.Generic;
using UnityEngine;

namespace Universal.UniversalSDK
{
    public class UniversalAPI
    {
        public static Dictionary<string, FlattenAction> actions =
            new Dictionary<string, FlattenAction>();        

        public static void Login(LoginType loginType,                                 
                                 Action<Result<LoginResult>> action)
        {
            UniversalSDK.Ins.BeginLogin(loginType, action);
        }

        internal static Action BeginLogin(LoginType loginType, Action<Result<LoginResult>> action)
        {
            var identifier = AddAction(FlattenAction.JsonFlatten<LoginResult>(action));
            try
            {
                NativeInterface.Login(identifier, loginType);
            }
            catch
            {
                actions.Remove(identifier);
                throw;
            }

            return () => actions.Remove(identifier);
        }

        static string AddAction(FlattenAction action)
        {
            var identifier = Guid.NewGuid().ToString();
            actions.Add(identifier, action);
            return identifier;
        }

        static FlattenAction PopActionFromPayload(CallbackMessageForUnity payload)
        {
            var identifier = payload?.Identifier;
            if (identifier == null)
            {
                return null;
            }
            FlattenAction action = null;
            if (actions.TryGetValue(identifier, out action))
            {
                actions.Remove(identifier);
                return action;
            }
            return null;
        }        

        private static CallbackMessageForUnity ParsePayload(string value)
        {
            try
            {
                return CallbackMessageForUnity.FromJson(value);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        public static void _OnApiOk(string result)
        {
            var payload = ParsePayload(result);
            var action = PopActionFromPayload(payload);
            if (action != null)
            {
                action.CallOk(payload.Value);
            }
        }

        public static void _OnApiError(string result)
        {
            var payload = ParsePayload(result);
            var action = PopActionFromPayload(payload);
            if (action != null)
            {
                action.CallError(payload.Value);
            }
        }        
    }

}
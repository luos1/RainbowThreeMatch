using System;
using UnityEngine;

namespace Universal.UniversalSDK
{
    public class FlattenAction
    {
        private Action<string> successAction;
        private Action<string> failureAction;

        FlattenAction(Action<string> successAction, Action<string> failureAction)
        {
            this.successAction = successAction;
            this.failureAction = failureAction;
        }

        static public FlattenAction JsonFlatten<T>(Action<Result<T>> action)
        {
            var flattenAction = new FlattenAction(
                value => {
                    var result = Result<T>.Ok(Parse<T>(value));
                    action.Invoke(result);
                },
                error => {
                    var result = Result<T>.Error(Parse<Error>(error));
                    action.Invoke(result);
                }
            );
            return flattenAction;
        }

        static public FlattenAction UnitFlatten(Action<Result<Unit>> action)
        {
            var flattenAction = new FlattenAction(
                _ => {
                    var result = Result<Unit>.Ok(Unit.Value);
                    action.Invoke(result);
                },
                error => {
                    var result = Result<Unit>.Error(Parse<Error>(error));
                    action.Invoke(result);
                }
            );
            return flattenAction;
        }

        private static T Parse<T>(string json)
        {
            try
            {
                return JsonUtility.FromJson<T>(json);
            }
            catch (ArgumentException)
            {
                return default;
            }
        }

        public void CallOk(string s)
        {
            successAction.Invoke(s);
        }

        public void CallError(string s)
        {
            failureAction.Invoke(s);
        }
    }
}
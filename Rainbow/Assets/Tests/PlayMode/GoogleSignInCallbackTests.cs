using NUnit.Framework;
using Universal.UniversalSDK;

public class GoogleSignInCallbackTests
{
    [TestCase("{\"userID\":\"test-user\",\"idToken\":\"test-token\"}", true, "test-token")]
    [TestCase("{\"userID\":\"test-user\"}", false, null)]
    [TestCase("invalid-json", false, null)]
    public void SdkSuccessReturnsTheIdTokenRequiredByFirebase(string payload, bool succeeds, string expectedToken)
    {
        var callback = FlattenAction.JsonFlatten<LoginResult>(result => GoogleSignIn.Complete(result, (outcome, idToken) =>
        {
            Assert.That(outcome, Is.EqualTo(succeeds ? SignInOutcome.Success : SignInOutcome.Failed));
            Assert.That(idToken, Is.EqualTo(expectedToken));
        }));
        callback.CallOk(payload);
    }

    [TestCase("{\"code\":7001}", true)]
    [TestCase("{\"code\":7002}", false)]
    [TestCase("invalid-json", false)]
    public void SdkCancellationIsDistinctFromOtherFailures(string payload, bool canceled)
    {
        var callback = FlattenAction.JsonFlatten<LoginResult>(result => GoogleSignIn.Complete(result, (outcome, idToken) =>
        {
            Assert.That(outcome, Is.EqualTo(canceled ? SignInOutcome.Canceled : SignInOutcome.Failed));
            Assert.That(idToken, Is.Null);
        }));
        callback.CallError(payload);
    }
}

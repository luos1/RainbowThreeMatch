using NUnit.Framework;
using Universal.UniversalSDK;

public class GoogleSignInCallbackTests
{
    [TestCase("{\"userID\":\"test-user\",\"idToken\":\"test-token\"}", true, "test-user")]
    [TestCase("{\"userID\":\"test-user\"}", false, null)]
    [TestCase("invalid-json", false, null)]
    public void SdkSuccessRequiresAnIdentityAndToken(string payload, bool succeeds, string expectedId)
    {
        var callback = FlattenAction.JsonFlatten<LoginResult>(result => GoogleSignIn.Complete(result, (outcome, id) =>
        {
            Assert.That(outcome, Is.EqualTo(succeeds ? SignInOutcome.Success : SignInOutcome.Failed));
            Assert.That(id, Is.EqualTo(expectedId));
        }));
        callback.CallOk(payload);
    }

    [TestCase("{\"code\":7001}", true)]
    [TestCase("{\"code\":7002}", false)]
    [TestCase("invalid-json", false)]
    public void SdkCancellationIsDistinctFromOtherFailures(string payload, bool canceled)
    {
        var callback = FlattenAction.JsonFlatten<LoginResult>(result => GoogleSignIn.Complete(result, (outcome, id) =>
        {
            Assert.That(outcome, Is.EqualTo(canceled ? SignInOutcome.Canceled : SignInOutcome.Failed));
            Assert.That(id, Is.Null);
        }));
        callback.CallError(payload);
    }
}

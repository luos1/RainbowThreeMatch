using System;
using System.Collections.Generic;
using NUnit.Framework;
using Rainbow.Services;

public sealed class FakeAuthServiceTests
{
    [Test]
    public void AnonymousSignInCreatesAnAnonymousCurrentUser()
    {
        var service = new FakeAuthService();

        AuthUser user = service.SignInAnonymously().GetAwaiter().GetResult();

        Assert.That(user.Uid, Does.StartWith("guest-"));
        Assert.That(user.IsAnonymous, Is.True);
        Assert.That(service.CurrentUser, Is.EqualTo(user));
    }

    [Test]
    public void GoogleSignInUsesTheAccountRegisteredForTheIdToken()
    {
        var service = new FakeAuthService();
        service.RegisterGoogleCredential("google-id-token", "google-user", "구글 사용자");

        AuthUser user = service.SignInWithGoogle("google-id-token").GetAwaiter().GetResult();

        Assert.That(user, Is.EqualTo(new AuthUser("google-user", false, "구글 사용자")));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void GoogleSignInRejectsABlankIdToken(string idToken)
    {
        var service = new FakeAuthService();

        Assert.Throws<ArgumentException>(() => service.SignInWithGoogle(idToken).GetAwaiter().GetResult());
    }

    [Test]
    public void LinkingAnUnclaimedGoogleCredentialPreservesTheGuestUid()
    {
        var service = new FakeAuthService();
        AuthUser guest = service.SignInAnonymously().GetAwaiter().GetResult();

        AuthUser linked = service.LinkGuestToGoogle("new-google-id-token").GetAwaiter().GetResult();

        Assert.That(linked.Uid, Is.EqualTo(guest.Uid));
        Assert.That(linked.IsAnonymous, Is.False);
        Assert.That(service.CurrentUser, Is.EqualTo(linked));
    }

    [Test]
    public void LinkConflictPreservesTheAnonymousCurrentUser()
    {
        var service = new FakeAuthService();
        service.RegisterGoogleCredential("claimed-google-id-token", "existing-google-user");
        AuthUser guest = service.SignInAnonymously().GetAwaiter().GetResult();

        Assert.Throws<AuthLinkConflictException>(() => service.LinkGuestToGoogle("claimed-google-id-token").GetAwaiter().GetResult());
        Assert.That(service.CurrentUser, Is.EqualTo(guest));
        Assert.That(service.CurrentUser.IsAnonymous, Is.True);
    }

    [Test]
    public void SignOutClearsTheUserAndPublishesTheNullState()
    {
        var service = new FakeAuthService();
        service.SignInAnonymously().GetAwaiter().GetResult();
        var observed = new List<AuthUser>();
        service.StateChanged += observed.Add;

        service.SignOut();

        Assert.That(service.CurrentUser, Is.Null);
        Assert.That(observed, Is.EqualTo(new AuthUser[] { null }));
    }

    [Test]
    public void SigningOutWhileAlreadySignedOutDoesNotPublishADuplicateState()
    {
        var service = new FakeAuthService();
        int events = 0;
        service.StateChanged += _ => events++;

        service.SignOut();

        Assert.That(events, Is.Zero);
    }
}

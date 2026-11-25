using System;
using NUnit.Framework;
using Rainbow.Services;
using UnityEngine;

public sealed class LocalGuestAuthServiceTests
{
    private string storagePrefix;

    [SetUp]
    public void SetUp() => storagePrefix = "Rainbow.Tests.Auth." + Guid.NewGuid().ToString("N") + ".";

    [TearDown]
    public void TearDown()
    {
        PlayerPrefs.DeleteKey(storagePrefix + "uid");
        PlayerPrefs.DeleteKey(storagePrefix + "active");
    }

    [Test]
    public void AnonymousSessionUsesAStableUidAndRestoresAfterRestart()
    {
        var first = new LocalGuestAuthService(storagePrefix);
        AuthUser signedIn = first.SignInAnonymously().GetAwaiter().GetResult();

        var restarted = new LocalGuestAuthService(storagePrefix);
        restarted.Initialize().GetAwaiter().GetResult();

        Assert.That(signedIn.Uid, Is.Not.Empty);
        Assert.That(signedIn.IsAnonymous, Is.True);
        Assert.That(restarted.CurrentUser.Uid, Is.EqualTo(signedIn.Uid));
        Assert.That(restarted.CurrentUser.IsAnonymous, Is.True);
    }

    [Test]
    public void SignOutInvalidatesTheActiveSessionButReusesTheGuestIdentity()
    {
        var service = new LocalGuestAuthService(storagePrefix);
        AuthUser first = service.SignInAnonymously().GetAwaiter().GetResult();
        AuthUser observed = first;
        service.StateChanged += user => observed = user;

        service.SignOut();
        var restarted = new LocalGuestAuthService(storagePrefix);
        restarted.Initialize().GetAwaiter().GetResult();

        Assert.That(service.CurrentUser, Is.Null);
        Assert.That(observed, Is.Null);
        Assert.That(restarted.CurrentUser, Is.Null);
        Assert.That(restarted.SignInAnonymously().GetAwaiter().GetResult().Uid, Is.EqualTo(first.Uid));
    }

    [Test]
    public void GoogleOperationsExplainThatFirebaseIsRequired()
    {
        var service = new LocalGuestAuthService(storagePrefix);

        Assert.That(() => service.SignInWithGoogle("id-token").GetAwaiter().GetResult(),
            Throws.TypeOf<NotSupportedException>().With.Message.Contains("RAINBOW_FIREBASE"));
        Assert.That(() => service.LinkGuestToGoogle("id-token").GetAwaiter().GetResult(),
            Throws.TypeOf<NotSupportedException>().With.Message.Contains("RAINBOW_FIREBASE"));
    }
}

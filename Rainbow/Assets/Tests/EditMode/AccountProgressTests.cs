using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Rainbow.Services;

public sealed class AccountProgressTests
{
    [Test]
    public void LinkedGuestProgressMergesWithTheExistingCloudAccount()
    {
        var auth = new FakeAuthService();
        auth.RegisterGoogleCredential("token", "member");
        var local = new InMemoryCloudSaveService();
        var cloud = new InMemoryCloudSaveService();
        var progress = new AccountProgress(auth, local, cloud);
        var guest = Complete(auth.SignInAnonymously());
        Complete(local.Save(guest.Uid, new SaveData(800, 1000, "Guest")));
        Complete(cloud.Save("member", new SaveData(400, 2000, "Member")));

        SaveData snapshot = Complete(progress.Sync(guest.Uid));
        var member = Complete(auth.SignInWithGoogle("token"));
        Complete(progress.KeepLinkedProgress(member, snapshot));
        Assert.That(Complete(progress.Sync(member.Uid)), Is.EqualTo(new SaveData(800, 2000, "Member")));
        Assert.That(Complete(cloud.Load(member.Uid)), Is.EqualTo(Complete(local.Load(member.Uid))));
        Assert.That(Complete(local.Load(guest.Uid)), Is.EqualTo(snapshot));
    }

    [Test]
    public void AFailedUploadLeavesTheNewScoreForASubsequentSync()
    {
        var auth = new FakeAuthService();
        var guest = Complete(auth.SignInAnonymously());
        var local = new InMemoryCloudSaveService();
        var progress = new AccountProgress(auth, local, new FailedCloud());

        Assert.Throws<InvalidOperationException>(() => progress.RecordScore(guest.Uid, 420).GetAwaiter().GetResult());
        Assert.That(Complete(local.Load(guest.Uid)).HighScore, Is.EqualTo(420));

        var cloud = new InMemoryCloudSaveService();
        Complete(new AccountProgress(auth, local, cloud).Sync(guest.Uid));
        Assert.That(Complete(cloud.Load(guest.Uid)).HighScore, Is.EqualTo(420));
    }

    [Test]
    public void ACompletedRoundCannotUploadForADifferentSignedInAccount()
    {
        var auth = new FakeAuthService();
        var guest = Complete(auth.SignInAnonymously());
        auth.RegisterGoogleCredential("token", "member");
        var member = Complete(auth.SignInWithGoogle("token"));
        var local = new InMemoryCloudSaveService();
        var cloud = new InMemoryCloudSaveService();
        var progress = new AccountProgress(auth, local, cloud);

        progress.RecordScore(guest.Uid, 980).GetAwaiter().GetResult();

        Assert.That(Complete(local.Load(guest.Uid)).HighScore, Is.EqualTo(980));
        Assert.That(Complete(cloud.Load(guest.Uid)), Is.Null);
        Assert.That(Complete(cloud.Load(member.Uid)), Is.Null);
    }

    private static T Complete<T>(Task<T> task)
    {
        Assert.That(task.IsCompleted, Is.True, "The test doubles complete synchronously.");
        return task.GetAwaiter().GetResult();
    }

    private sealed class FailedCloud : ICloudSaveService
    {
        public Task<SaveData> Save(string uid, SaveData data) => Task.FromException<SaveData>(new InvalidOperationException());
        public Task<SaveData> Load(string uid) => Task.FromException<SaveData>(new InvalidOperationException());
    }
}

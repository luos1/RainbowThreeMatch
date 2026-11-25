using UnityEngine;

public sealed class UserData
{
    public string ID { get; }
    public static UserData Current { get; private set; }

    private UserData(string id) => ID = id;

    internal static void StartSession(string id) => Current = new UserData(id);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    internal static void ClearSession() => Current = null;
}

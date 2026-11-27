namespace Tests.Shared;

public static class FixtureLock {
    public static IDisposable Take(string fixture) => FeedLock.Wait(Repo.Path("artifacts~", "fixture-locks", fixture));
}

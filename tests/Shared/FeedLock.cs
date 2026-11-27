namespace Tests.Shared;

static class FeedLock {
    static readonly TimeSpan WaitLimit = TimeSpan.FromMinutes(60);

    public static FileStream Wait(string feed) {
        var started = DateTime.UtcNow;
        while (true) {
            if (TryTake(feed) is { } taken) {
                return taken;
            }
            if (DateTime.UtcNow - started > WaitLimit) {
                throw new TimeoutException($"A test process held {PathOf(feed)} for {WaitLimit.TotalMinutes} minutes.");
            }
            Thread.Sleep(500);
        }
    }

    public static FileStream? TryTake(string feed) {
        Directory.CreateDirectory(Path.GetDirectoryName(feed)!);
        try {
            return new FileStream(PathOf(feed), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        } catch (IOException) {
            return null;
        }
    }

    public static string PathOf(string feed) => feed + ".lock";
}

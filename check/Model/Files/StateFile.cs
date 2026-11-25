namespace Umpire.CheckModel;

static class StateFile {
    const string MadeAt = "\"madeAt\":\"";
    const string LockExtension = ".lock";
    const int Tries = 100;
    const int WaitMilliseconds = 20;

    public static string Read(string file) => Retry(() => File.ReadAllText(file));

    public static bool Save(string file, string text) {
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        using (Lock(file + LockExtension)) {
            if (Has(file, text)) {
                File.SetLastWriteTimeUtc(file, DateTime.UtcNow);
                return false;
            }
            Write(file, text);
        }
        return true;
    }

    static void Write(string file, string text) {
        var temporary = $"{file}.{Guid.NewGuid():N}.tmp";
        try {
            File.WriteAllText(temporary, text);
            Retry(() => Move(temporary, file));
        } finally {
            File.Delete(temporary);
        }
    }

    static bool Has(string file, string text) => File.Exists(file) && WithNoDate(Read(file)) == WithNoDate(text);

    static FileStream Lock(string file) => Retry(() =>
        new FileStream(file, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 1, FileOptions.DeleteOnClose));

    static bool Move(string from, string to) {
        if (File.Exists(to)) {
            File.Replace(from, to, null);
        } else {
            File.Move(from, to);
        }
        return true;
    }

    static T Retry<T>(Func<T> action) {
        for (var attempt = 1; ; attempt++) {
            try {
                return action();
            } catch (Exception e) when (attempt < Tries && CanPass(e)) {
                Thread.Sleep(WaitMilliseconds);
            }
        }
    }

    static bool CanPass(Exception e) =>
        e is UnauthorizedAccessException or IOException and not (FileNotFoundException or DirectoryNotFoundException);

    static string WithNoDate(string text) {
        var start = text.IndexOf(MadeAt, StringComparison.Ordinal);
        return start < 0 ? text : text.Remove(start, text.IndexOf('"', start + MadeAt.Length) - start);
    }
}

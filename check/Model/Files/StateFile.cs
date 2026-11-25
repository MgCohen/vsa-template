namespace Umpire.CheckModel;

static class StateFile {
    const string MadeAt = "\"madeAt\":\"";
    const string LockExtension = ".lock";
    const int Tries = 100;
    const int WaitMilliseconds = 20;

    public static bool Save(string file, string text) {
        if (Has(file, text)) {
            return false;
        }
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        using (Lock(file + LockExtension)) {
            if (Has(file, text)) {
                return false;
            }
            var temporary = $"{file}.{Guid.NewGuid():N}.tmp";
            File.WriteAllText(temporary, text);
            Retry(() => Move(temporary, file));
        }
        return true;
    }

    static bool Has(string file, string text) => File.Exists(file) && WithNoDate(Retry(() => File.ReadAllText(file))) == WithNoDate(text);

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
            } catch (Exception e) when (attempt < Tries && e is IOException or UnauthorizedAccessException) {
                Thread.Sleep(WaitMilliseconds);
            }
        }
    }

    static string WithNoDate(string text) {
        var start = text.IndexOf(MadeAt, StringComparison.Ordinal);
        return start < 0 ? text : text.Remove(start, text.IndexOf('"', start + MadeAt.Length) - start);
    }
}

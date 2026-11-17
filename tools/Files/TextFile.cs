namespace Umpire.Files;

using System.Text;

public sealed record TextFile(string Path, string Content) {
    const string Crlf = "\r\n";
    const string Lf = "\n";

    public static void WriteAllOrNone(string root, IReadOnlyList<TextFile> files) {
        var existing = files.Where(file => File.Exists(file.FullPath(root))).Select(file => file.Path).ToList();
        if (existing is not []) {
            throw new FilesExistException(existing);
        }
        var made = new List<string>();
        try {
            foreach (var file in files) {
                var full = file.FullPath(root);
                file.WriteNew(full);
                made.Add(full);
            }
        } catch {
            foreach (var full in made) {
                File.Delete(full);
            }
            throw;
        }
    }

    public bool Write(string root) {
        var full = FullPath(root);
        if (!File.Exists(full)) {
            WriteNew(full);
            return true;
        }
        var bom = File.ReadAllBytes(full) is [0xEF, 0xBB, 0xBF, ..];
        var ending = DominantEnding(File.ReadAllText(full));
        var text = WithLf(Content);
        File.WriteAllText(full, ending == Lf ? text : text.Replace(Lf, ending), new UTF8Encoding(bom));
        return false;
    }

    static string WithLf(string text) => text.Replace(Crlf, Lf);

    static string DominantEnding(string text) {
        var crlf = text.Split(Crlf).Length - 1;
        var lf = text.Count(c => c == '\n') - crlf;
        return crlf > lf ? Crlf : Lf;
    }

    void WriteNew(string full) {
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(full)!);
        using var stream = new FileStream(full, FileMode.CreateNew, FileAccess.Write);
        stream.Write(Encoding.UTF8.GetBytes(WithLf(Content)));
    }

    string FullPath(string root) => System.IO.Path.GetFullPath(System.IO.Path.Combine(root, Path));
}

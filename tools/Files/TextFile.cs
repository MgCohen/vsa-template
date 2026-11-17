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
        foreach (var file in files) {
            file.Write(root);
        }
    }

    public bool Write(string root) {
        var full = FullPath(root);
        var text = Content.Replace(Crlf, Lf);
        if (!File.Exists(full)) {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(full)!);
            File.WriteAllText(full, text, new UTF8Encoding(false));
            return true;
        }
        var bom = File.ReadAllBytes(full) is [0xEF, 0xBB, 0xBF, ..];
        var ending = DominantEnding(File.ReadAllText(full));
        File.WriteAllText(full, ending == Lf ? text : text.Replace(Lf, ending), new UTF8Encoding(bom));
        return false;
    }

    static string DominantEnding(string text) {
        var crlf = text.Split(Crlf).Length - 1;
        var lf = text.Count(c => c == '\n') - crlf;
        return crlf > lf ? Crlf : Lf;
    }

    string FullPath(string root) => System.IO.Path.GetFullPath(System.IO.Path.Combine(root, Path));
}

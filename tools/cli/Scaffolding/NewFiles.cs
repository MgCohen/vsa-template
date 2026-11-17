namespace Umpire.Cli.Scaffolding;

using Umpire.Files;

static class NewFiles {
    public static void Write(string block, IReadOnlyList<TextFile> files, string hint = "") {
        try {
            TextFile.WriteAllOrNone(block, files);
        } catch (FilesExistException e) {
            throw new UmpireException(e.Message + hint);
        }
        foreach (var file in files) {
            Console.WriteLine($"  created {file.Path}");
        }
    }
}

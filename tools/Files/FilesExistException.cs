namespace Umpire.Files;

public sealed class FilesExistException(IReadOnlyList<string> paths) : IOException(
    "These files exist. The tool does not write over a file:"
    + Environment.NewLine + "  " + string.Join(Environment.NewLine + "  ", paths)) {
    public IReadOnlyList<string> Paths => paths;
}

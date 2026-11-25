namespace Umpire.CheckModel;

using System.Security.Cryptography;

public static class FileHash {
    public static string OfText(string text) {
        using var sha = SHA256.Create();
        return Text(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(text)));
    }

    public static string Of(string file) {
        using var sha = SHA256.Create();
        using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        return Text(sha.ComputeHash(stream));
    }

    static string Text(byte[] hash) => BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
}

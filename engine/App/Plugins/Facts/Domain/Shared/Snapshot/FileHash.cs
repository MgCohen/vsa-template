namespace Umpire.CheckModel;

using System.Security.Cryptography;

public static class FileHash {
    public static string OfText(string text) {
        using var sha = SHA256.Create();
        return Of(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(text)));
    }

    public static string Of(byte[] hash) => BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
}

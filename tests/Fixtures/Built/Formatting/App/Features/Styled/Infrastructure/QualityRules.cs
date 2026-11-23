// IDE0005: no code in this file uses System.Text.
using System.Text;

namespace Styled.Infrastructure;

internal sealed class QualityRules
{
    private readonly int start;

    // IDE0052: the constructor writes this field, and no code reads it.
    // The value is not a constant, so the compiler does not report CS0414.
    private readonly int written;

    public QualityRules(int start, int written)
    {
        this.start = start;
        this.written = written;
    }

    // IDE0051: no code calls this method.
    private int Unused()
    {
        return start;
    }

    // IDE0060: no code reads the parameter name.
    public int Count(string name)
    {
        // IDE0059: the next line replaces the value 0, so no code reads it.
        int total = 0;
        total = Next();
        // IDE0004: Next() already gives an int.
        int next = (int)Next();
        return total + next;
    }

    // IDE0079 (editor only, the build does not report it): no CA2000 occurs in this method, so the suppression hides nothing.
#pragma warning disable CA2000
    public int Next()
    {
        return start + 1;
    }
#pragma warning restore CA2000

    // CA2000: no code disposes the stream.
    public long Size(string path)
    {
        FileStream stream = File.OpenRead(path);
        return stream.Length + start;
    }

    // CA1849: a blocking Read in an async method, and ReadAsync exists.
    public async Task<int> LoadAsync(Stream stream, byte[] buffer)
    {
        int read = stream.Read(buffer, 0, buffer.Length) + start;
        await Task.Yield();
        return read;
    }
}

// CA1052: a class with only static members that is not static. It is public: CA1852 already seals an internal class.
public class Paths
{
    public static string Root
    {
        get { return "/app"; }
    }
}

// CA1012: an abstract class with a public constructor.
internal abstract class Worker
{
    public Worker()
    {
    }
}

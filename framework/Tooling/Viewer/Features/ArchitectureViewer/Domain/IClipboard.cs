namespace ArchitectureViewer.Domain;

public interface IClipboard
{
    Task<bool> Copy(string text);
}

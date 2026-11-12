using System.Text.Json;
using ArchitectureViewer.Domain.Shared;

namespace ArchitectureViewer.Infrastructure;

public static class LayoutJson
{
    private static readonly JsonSerializerOptions web = new(JsonSerializerDefaults.Web);

    public static LayoutData Read(string json)
    {
        return JsonSerializer.Deserialize<LayoutData>(json, web)!;
    }
}

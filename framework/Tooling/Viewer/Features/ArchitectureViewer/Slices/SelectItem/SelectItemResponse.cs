using ArchitectureViewer.Domain.Shared;
using VSA.Contracts.Requests;

namespace ArchitectureViewer.Slices.SelectItem;

public sealed record SelectItemResponse(Focus Selection, IReadOnlyList<string> Hot, IReadOnlyList<string> Related)
    : IResponse
{
    public static readonly SelectItemResponse Nothing = new(Focus.None, [], []);
}

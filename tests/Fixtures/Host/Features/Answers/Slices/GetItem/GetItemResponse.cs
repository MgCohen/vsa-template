using VSA.Contracts.Requests;

namespace Answers.Slices.GetItem;

public sealed record GetItemResponse(Guid Id, int Count, bool Loud) : IResponse;

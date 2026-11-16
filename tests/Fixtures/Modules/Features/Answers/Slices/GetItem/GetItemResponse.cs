namespace Answers.Slices.GetItem;

using Umpire.Vsa.Contracts.Requests;

public sealed record GetItemResponse(Guid Id, int Count, bool Loud) : IResponse;

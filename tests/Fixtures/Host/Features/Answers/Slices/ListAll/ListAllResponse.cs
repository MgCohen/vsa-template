namespace Answers.Slices.ListAll;

using Umpire.Vsa.Contracts.Requests;

public sealed record ListAllResponse(int Count) : IResponse;

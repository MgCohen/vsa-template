using Umpire.Vsa.Contracts.Requests;

namespace Answers.Slices.ListAll;

public sealed record ListAllResponse(int Count) : IResponse;

using VSA.Contracts.Requests;

namespace Score.Slices.GetScore;

public sealed record GetScoreResponse(int Total) : IResponse;

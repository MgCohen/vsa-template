using VSA.Contracts.Requests;

namespace Score.Slices.GetScore;

public sealed record GetScoreRequest(int Points) : IRequest<GetScoreResponse>;

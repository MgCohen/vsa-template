using Umpire.Vsa.Contracts.Requests;

namespace Answers.Slices.Answer;

public sealed record AnswerResponse(string Text) : IResponse;

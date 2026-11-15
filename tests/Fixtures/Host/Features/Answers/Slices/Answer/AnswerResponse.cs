namespace Answers.Slices.Answer;

using Umpire.Vsa.Contracts.Requests;

public sealed record AnswerResponse(string Text) : IResponse;

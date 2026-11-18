namespace Answers.Slices.ListAll;

using Umpire.Messages.Contracts;
using Umpire.Vsa.Contracts.Requests;

public sealed record ListAllRequest : IRequest<ListAllResponse> {
    public static Effect Effect => Effect.Read;
}

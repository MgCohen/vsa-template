namespace Greet.Slices.Greet;

using Umpire.Vsa.Contracts.Requests;

public sealed record GreetRequest(string Name, int Code = 0, GreetStyle Style = GreetStyle.Plain) : IRequest<GreetResponse>;

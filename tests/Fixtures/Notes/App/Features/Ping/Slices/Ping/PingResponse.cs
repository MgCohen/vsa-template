namespace Notes.Features.Ping.Slices.Ping;

using Umpire.Vsa.Contracts.Requests;

public sealed record PingResponse(string Message) : IResponse;

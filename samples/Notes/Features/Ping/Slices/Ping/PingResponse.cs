namespace Notes.Features.Ping.Slices.Ping;

using VSA.Contracts.Requests;

public sealed record PingResponse(string Message) : IResponse;

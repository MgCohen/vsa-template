using VSA.Contracts.Requests;

namespace Ping.Slices.Ping;

public sealed record PingResponse(string Message) : IResponse;

using VSA.Contracts.Requests;

namespace Ping.Contracts.Requests.Ping;

public sealed record PingResponse(string Message) : IResponse;

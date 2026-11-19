namespace Greet.Slices.Greet;

using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Requests;

sealed class GreetHandler : RequestHandler<GreetRequest, GreetResponse> {
    public override Task<Result<GreetResponse>> HandleAsync(Envelope<GreetRequest> envelope) {
        var request = envelope.Message;
        if (request.Name.Length == 0) {
            return Task.FromResult<Result<GreetResponse>>(new Error("greet.name", "The name is empty."));
        }
        var code = request.Style == GreetStyle.Loud ? 10
            : request.Marks != GreetMarks.None ? 20 + (int)request.Marks
            : request.Code;
        return Task.FromResult<Result<GreetResponse>>(new GreetResponse($"Hello, {request.Name}", code));
    }
}

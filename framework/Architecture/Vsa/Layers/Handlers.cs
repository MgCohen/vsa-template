using VSA.Architecture;
using VSA.Handlers.Commands;
using VSA.Handlers.Events;
using VSA.Handlers.Requests;

namespace VSA.Architecture.Vsa;

public record Handlers : Layer {
    protected override void Define(Part part) {
        part.Reaches(Reach.Only);
    }

    public record Requests : Layer {
        protected override void Define(Part part) {
            part.OfType(typeof(RequestHandler<,>));
        }
    }

    public record Events : Layer {
        protected override void Define(Part part) {
            part.OfType(typeof(IntegrationEventHandler<>));
        }
    }

    public record Commands : Layer {
        protected override void Define(Part part) {
            part.OfType(typeof(CommandHandler<,>));
        }
    }
}

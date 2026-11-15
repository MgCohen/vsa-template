namespace Umpire.Vsa.Definition;

using Umpire.Architecture;
using Umpire.Vsa.Handlers.Commands;
using Umpire.Vsa.Handlers.Events;
using Umpire.Vsa.Handlers.Requests;

public record Handlers : Layer {
    protected override void Configure(Options defaults) {
        defaults.Depth = Depth.Self;
    }

    public record Requests : Layer {
        protected override void Define(Part part) {
            part.OfType(typeof(RequestHandler<,>));
        }
    }

    public record Events : Layer {
        protected override void Define(Part part) {
            part.OfType(typeof(IntegrationEventHandler<>))
                .OfType(typeof(BeforeEventHook<>))
                .OfType(typeof(AfterEventHook<>));
        }
    }

    public record Commands : Layer {
        protected override void Define(Part part) {
            part.OfType(typeof(CommandHandler<,>))
                .OfType(typeof(BeforeCommandHook<>))
                .OfType(typeof(AfterCommandHook<,>));
        }
    }
}

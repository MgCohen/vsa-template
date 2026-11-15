namespace Umpire.Vsa.Definition;

using Umpire.Architecture;
using Umpire.Vsa.Contracts.Commands;
using Umpire.Vsa.Contracts.Events;
using Umpire.Vsa.Contracts.Requests;

public record Contracts : Layer {
    protected override void Configure(Options defaults) {
        defaults.Depth = Depth.Self;
    }

    public record Requests : Layer {
        protected override void Define(Part part) {
            part.OfType(typeof(IRequest)).OfType<IResponse>();
        }
    }

    public record Events : Layer {
        protected override void Define(Part part) {
            part.OfType<IntegrationEvent>();
        }
    }

    public record Commands : Layer {
        protected override void Define(Part part) {
            part.OfType(typeof(ICommand<>));
        }
    }
}

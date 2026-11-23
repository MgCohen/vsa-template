namespace Umpire.Vsa.Definition;

using Umpire.Architecture;
using Umpire.Vsa.Contracts.Commands;
using Umpire.Vsa.Contracts.Events;
using Umpire.Vsa.Contracts.Requests;

public record Contracts : Layer {
    protected override void Configure(PartOptions defaults) {
        defaults.Depth = Depth.Self;
    }

    public record Requests : Layer {
        public override IReadOnlyList<Type> OfTypes => [typeof(IRequest), typeof(IResponse)];
    }

    public record Events : Layer {
        public override IReadOnlyList<Type> OfTypes => [typeof(IntegrationEvent)];
    }

    public record Commands : Layer {
        public override IReadOnlyList<Type> OfTypes => [typeof(ICommand<>)];
    }
}

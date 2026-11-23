namespace Umpire.Vsa.Definition;

using Umpire.Architecture;
using Umpire.Vsa.Handlers.Commands;
using Umpire.Vsa.Handlers.Events;
using Umpire.Vsa.Handlers.Requests;

public record Handlers : Layer {
    protected override void Configure(PartOptions defaults) {
        defaults.Depth = Depth.Self;
    }

    public record Requests : Layer {
        public override IReadOnlyList<Type> OfTypes => [typeof(RequestHandler<,>)];
    }

    public record Events : Layer {
        public override IReadOnlyList<Type> OfTypes => [typeof(IntegrationEventHandler<>)];

        public record Hooks : Layer {
            public override IReadOnlyList<Type> OfTypes => [typeof(BeforeEventHook<>), typeof(AfterEventHook<>)];
            public override string NameEndsWith => "Hook";
        }
    }

    public record Commands : Layer {
        public override IReadOnlyList<Type> OfTypes => [typeof(CommandHandler<,>)];

        public record Hooks : Layer {
            public override IReadOnlyList<Type> OfTypes => [typeof(BeforeCommandHook<>), typeof(AfterCommandHook<,>)];
            public override string NameEndsWith => "Hook";
        }
    }
}

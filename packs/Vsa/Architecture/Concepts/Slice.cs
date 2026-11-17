namespace Umpire.Vsa.Definition;

using Umpire.Architecture;
using Umpire.Messages.Handlers;

public record Slice : Layer {
    public override string GroupFolder => "Slices";

    protected override void Define(Part part) {
        part.Add<Contracts.Requests>().And<Contracts.Events>().And<Contracts.Commands>()
            .And<Handlers.Requests>().And<Handlers.Events>().And<Handlers.Commands>()
            .And<Handlers.Events.Hooks>().And<Handlers.Commands.Hooks>()
            .And<Validators>()
            .InParentFolder();
    }

    protected override void Rules(RuleBook rules) {
        rules.For<Slice>().Has<Handlers>().AtLeast(1);
        rules.For<Slice>().Has<Handlers>().OfOneKind();
        rules.For<Handlers.Requests>().Takes<Contracts.Requests>().InSame<Slice>();
        rules.For<Contracts.Requests>().Has<Handlers.Requests>(Depth.Self).Exactly(1);
        rules.For<Handlers.Commands>().Takes<Contracts.Commands>().InSame<Slice>();
        rules.For<Contracts.Commands>().Has<Handlers.Commands>(Depth.Self).Exactly(1);
        rules.For<Validators>().Uses<Handlers>().And<Contracts>().And<Shared>()
            .And<Domain>(Access.Internal).And<Domain.Shared>();
        rules.For<Validators>().Uses<Contracts.Requests>().And<Contracts.Commands>().And<Contracts.Events>()
            .InSame<Slice>();
    }

    public record Shared : Layer {
        public override string Folder => "Slices/Shared";
    }

    public record Validators : Layer {
        protected override void Define(Part part) {
            part.OfType(typeof(Validator<>));
        }
    }
}

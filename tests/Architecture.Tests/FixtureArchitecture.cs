namespace EngineCore.Tests;

using Kit;
using Umpire.Architecture;
using Umpire.Architecture.Blocks;

public record KitLayer : External {
    public override string Folder => "Kit";
    public override string Assembly => "Kit";
}

public record Feature : Concept {
    public override string GroupFolder => "Features";
}

public record Slice : Concept {
    public override string GroupFolder => "Slices";
}

public record Domain : Concept;

public record Commands : Concept {
    protected override void Rules(RuleBook rules) {
        rules.For<Commands>().Are<Umpire.Architecture.Traits.Static>();
        rules.Type<CommandAttribute>().OnlyIn<Commands>();
    }
}

public record Presenters : Concept {
    protected override void Define(Part part) {
        part.OfType<IPresenter>();
    }
}

public record Contracts : Concept {
    public record Requests : Concept {
        protected override void Define(Part part) {
            part.OfType<IRequest>();
        }
    }

    public record Events : Concept {
        protected override void Define(Part part) {
            part.OfType<IEvent>();
        }
    }
}

public record Handlers : Concept {
    public record Requests : Concept {
        protected override void Define(Part part) {
            part.OfType<IRequestHandler>();
        }
    }

    public record Events : Concept {
        protected override void Define(Part part) {
            part.OfType<IEventHandler>();
        }
    }
}

public abstract class FixtureArchitecture : Architecture {
    protected override void Configure(Options defaults) {
        defaults.DefaultPermission = Permission.Deny;
        defaults.Boundary = Boundary.Closed;
    }

    protected override void Define(Block block) {
        block.Add<KitLayer>();
        var feature = block.AddGroup<Feature>();
        feature.Add<Module>().OfType<IModuleRoot>();
        feature.Add<Domain>();
        feature.Add<Commands>().NamedAfterGroup();
        feature.Add<Presenters>();
        feature.AddGroup<Slice>()
            .Add<Contracts.Requests>().And<Contracts.Events>()
            .And<Handlers.Requests>().And<Handlers.Events>()
            .InParentFolder();
    }

    protected override void Rules(RuleBook rules) {
        rules.For<Feature>().Uses<Feature>();
        rules.For<Feature>().Uses<KitLayer>();
        rules.For<Domain>().Denies<KitLayer>();
        rules.Type<System.Text.StringBuilder>().OnlyIn<Domain>();
        rules.For<Slice>().Uses<Contracts.Events>().InOther<Feature>();
        rules.For<Slice>().Denies<Presenters>().And<Module>().InSame<Feature>();
        rules.For<Presenters>().Uses<Domain>(Access.Internal);
        rules.For<Presenters>().Uses<Contracts.Events>(Access.Internal);
        rules.For<Feature>().Has<Commands>().AtMost(1);
    }
}

public sealed class GoodArchitecture : FixtureArchitecture;

public sealed class BadArchitecture : FixtureArchitecture;

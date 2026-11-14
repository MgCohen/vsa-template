using Kit;
using VSA.Architecture;

namespace Architecture.Tests;

public record KitLayer : External {
    public override string Folder => "Kit";
    public override string Assembly => "Kit";
}

public record Module : Anchor;

public record Feature : Concept {
    public override string GroupFolder => "Features";
}

public record Slice : Concept {
    public override string GroupFolder => "Slices";
}

public record Domain : Concept;

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

public abstract class FixtureDefinition(string app) : ArchitectureDefinition(app) {
    protected override void Configure(Options options) {
        options.DefaultPermission = Permission.Deny;
        options.Boundary = Boundary.Closed;
    }

    protected override void Define(Root root) {
        root.Add<KitLayer>();
        var feature = root.AddGroup<Feature>();
        feature.Add<Module>().OfType<IModuleRoot>();
        feature.Add<Domain>();
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
        rules.For<Slice>().Uses<Contracts.Events>().InOther<Feature>();
        rules.For<Slice>().Denies<Presenters>().And<Module>().InSame<Feature>();
        rules.For<Presenters>().Uses<Domain>(Access.Internal);
        rules.For<Presenters>().Uses<Contracts.Events>(Access.Internal);
    }
}

public sealed class GoodDefinition() : FixtureDefinition("GoodApp");

public sealed class BadDefinition() : FixtureDefinition("BadApp");

using Kit;
using VSA.Architecture;

namespace Architecture.Tests;

public record KitLayer : External {
    public override string Folder => "Kit";
    public override string Assembly => "Kit";
}

public record RootClass : Layer;

public record Feature : Layer {
    public override string GroupFolder => "Features";
}

public record Slice : Layer {
    public override string GroupFolder => "Slices";
}

public record Domain : Layer;

public record Presenters : Layer {
    protected override void Define(Part part) {
        part.OfType<IPresenter>();
    }
}

public record Contracts : Layer {
    public record Requests : Layer {
        protected override void Define(Part part) {
            part.OfType<IRequest>();
        }
    }

    public record Events : Layer {
        protected override void Define(Part part) {
            part.OfType<IEvent>();
        }
    }
}

public record Handlers : Layer {
    public record Requests : Layer {
        protected override void Define(Part part) {
            part.OfType<IRequestHandler>();
        }
    }

    public record Events : Layer {
        protected override void Define(Part part) {
            part.OfType<IEventHandler>();
        }
    }
}

public abstract class FixtureDefinition(string app) : ArchitectureDefinition(app) {
    protected override void Configure(Options options) {
        options.Unlisted = Permission.Deny;
        options.Own = Own.AllGroups;
    }

    protected override void Define(Root root) {
        root.Add<KitLayer>();
        var feature = root.AddMany<Feature>();
        feature.Add<RootClass>().NoFolder().OfType<IModuleRoot>();
        feature.Add<Domain>();
        feature.Add<Presenters>();
        feature.AddMany<Slice>()
            .Add<Contracts.Requests>().And<Contracts.Events>()
            .And<Handlers.Requests>().And<Handlers.Events>()
            .NoFolder();
    }

    protected override void Rules(Map map) {
        map.For<Feature>().Uses<KitLayer>();
        map.For<Domain>().Denies<KitLayer>();
        map.For<Slice>().Uses<Contracts.Events>().InOther<Feature>();
        map.For<Slice>().Denies<Presenters>().And<RootClass>().InOwn<Feature>();
        map.For<Presenters>().Uses<Domain>(Access.Internal);
    }
}

public sealed class GoodDefinition() : FixtureDefinition("GoodApp");

public sealed class BadDefinition() : FixtureDefinition("BadApp");

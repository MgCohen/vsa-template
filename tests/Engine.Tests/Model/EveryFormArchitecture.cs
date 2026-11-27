namespace CheckModel.Tests;

using Umpire.Architecture;
using Umpire.Architecture.Blocks;
using Umpire.Architecture.Traits;

public sealed class EveryFormArchitecture : Architecture {
    protected override void Configure(ArchitectureConfig config) {
        config.Defaults.UseSameConcept = true;
        config.FolderAliases.Add(new FolderAlias("Assets", ""));
    }

    protected override void Define(Part root, Part kernel) {
        kernel.Add<Inner>();
        root.AddBlockGroup<Tool>("Tools");
        var module = root.AddGroup<Module>(Boundary.Closed);
        module.Add<Inner>().OfType<IDisposable>(required: true).NameStartsWith("I").NameEndsWith("r");
        var slice = module.AddGroup<Slice>();
        slice.Add<Request>().NamedAfterGroup("Request").And<Reply>().NamedAfterGroup<Module>().InParentFolder();
        slice.Add<Inner>();
    }

    protected override void Rules(RuleBook rules) {
        rules.For<Module>().Uses<Inner>().Under<Kernel>();
        rules.For<Slice>().Uses<Inner>(Access.Internal).InSame<Module>();
        rules.For<Slice>().Uses<Request>(Depth.Self).And<Reply>(Boundary.Open).InOther<Module>();
        rules.For<Module>(Depth.Self).Denies<Slice>(Depth.Self).InOther<Slice>();
        rules.For<Inner>().Under<Slice>().Denies<Module>(Depth.Self);
        rules.For<Slice>().Uses<Library>();
        rules.For<Module>().Has<Request>().AtLeast(1).AtMost(3);
        rules.For<Slice>().Has<Request>(Depth.Self).OfOneKind();
        rules.For<Slice>(Depth.Self).HasTypes<IDisposable>().Exactly(1);
        rules.For<Slice>().HasTypes<object>().AtMost(9);
        rules.For<Reply>().Takes<Request>().InSame<Slice>();
        rules.For<Reply>().Takes<Request>();
        rules.For<Request>().Are(!Trait.Static & (Trait.Record | Trait.Struct)).And<Sealed>();
        rules.Type<System.Text.StringBuilder>().OnlyIn<Inner>().And<Request>();
        rules.Type<ObsoleteAttribute>().OnlyIn<Module>().Under<Module>();
    }

    public record Tool : Concept;

    public record Module : Concept;

    public record Slice : Concept;

    public record Inner : Concept;

    public record Request : Concept {
        public override IReadOnlyList<Type> OfTypes => [typeof(IComparable)];
    }

    public record Reply : Concept {
        public override IReadOnlyList<Type> OfTypes => [typeof(IFormattable)];
    }

    public record Library : External {
        public override string Assembly => "Some.Library";
    }
}

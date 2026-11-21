namespace EngineCore.Tests;

using Umpire.Architecture;

public interface ICounted;

public interface ISmallCounted;

public interface ITagged;

public interface INeeded;

public record Box : Concept {
    public override string GroupFolder => "Boxes";
}

public record Item : Concept {
    protected override void Define(Part part) {
        part.OfType<ICounted>();
    }

    public record Small : Concept {
        protected override void Define(Part part) {
            part.OfType<ISmallCounted>();
        }
    }
}

public record Tag : Concept {
    protected override void Define(Part part) {
        part.OfType<ITagged>();
    }
}

public record NeededByConcept : Concept {
    protected override void Configure(Options defaults) {
        defaults.Required = true;
    }

    protected override void Define(Part part) {
        part.OfType<INeeded>();
    }
}

public sealed class CountArchitecture(Action<RuleBook>? rules = null, Action<PartGroup>? parts = null, bool? required = null)
    : Architecture {
    public const string BlockFolder = "C:/count-app/App";
    public const string OutsideFolder = "C:/count-app/Other";

    public IReadOnlyList<CountResult> Counts(params TypeFacts[] types) {
        var resolved = new ResolvedArchitecture(this);
        return resolved.Counts(resolved.Place(types, BlockFolder));
    }

    public IReadOnlyList<MatchResult> Matches(params TypeFacts[] types) {
        var resolved = new ResolvedArchitecture(this);
        return resolved.Matches(resolved.Place(types, BlockFolder));
    }

    public IReadOnlyList<ContentResult> Names(params TypeFacts[] types) {
        var resolved = new ResolvedArchitecture(this);
        return resolved.Names(resolved.Place(types, BlockFolder));
    }

    public IReadOnlyList<ContentResult> Kinds(params TypeFacts[] types) {
        var resolved = new ResolvedArchitecture(this);
        return resolved.Kinds(resolved.Place(types, BlockFolder));
    }

    public IReadOnlyList<ContentResult> Owners(params TypeFacts[] types) {
        var resolved = new ResolvedArchitecture(this);
        return resolved.Owners(resolved.Place(types, BlockFolder));
    }

    public static TypeFacts Fact(string name, string box, Type? baseType = null, params string[] arguments) =>
        new($"App.{box}.{name}", name, "App", new Source($"{BlockFolder}/Boxes/{box}/{name}.cs", 1),
            baseType is null ? [] : [baseType.FullName!], [], [], []) { BaseArguments = arguments };

    public static TypeFacts Outside(string name) =>
        new($"Other.{name}", name, "App", new Source($"{OutsideFolder}/Boxes/a/{name}.cs", 1), [typeof(ICounted).FullName!], [], [], []);

    public static TypeFacts Using(TypeFacts user, TypeFacts helper) => user with { Mentions = [helper.FullName] };

    protected override void Configure(Options defaults) {
        defaults.Required = required;
    }

    protected override void Define(Block block) {
        var box = block.AddGroup<Box>();
        if (parts is null) {
            box.Add<Item>().And<Item.Small>().And<Tag>().InParentFolder();
        } else {
            parts(box);
        }
    }

    protected override void Rules(RuleBook book) => rules?.Invoke(book);
}

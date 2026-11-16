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
    : Architecture(RootFolder) {
    public const string RootFolder = "App";
    public const string AppRoot = "C:/count-app";

    public IReadOnlyList<CountResult> Counts(params TypeFacts[] types) {
        var resolved = new ResolvedArchitecture(this);
        return resolved.Counts(resolved.Place(types, AppRoot));
    }

    public IReadOnlyList<MatchResult> Matches(params TypeFacts[] types) {
        var resolved = new ResolvedArchitecture(this);
        return resolved.Matches(resolved.Place(types, AppRoot));
    }

    public static TypeFacts Fact(string name, string box, Type? baseType = null, params string[] arguments) =>
        new($"App.{box}.{name}", name, "App", new Source($"{AppRoot}/{RootFolder}/Boxes/{box}/{name}.cs", 1),
            baseType is null ? [] : [baseType.FullName!], [], [], []) { BaseArguments = arguments };

    public static TypeFacts Outside(string name) =>
        new($"Other.{name}", name, "App", new Source($"{AppRoot}/Other/Boxes/a/{name}.cs", 1), [typeof(ICounted).FullName!], [], [], []);

    public static TypeFacts Using(TypeFacts user, TypeFacts helper) => user with { Mentions = [helper.FullName] };

    protected override void Configure(Options defaults) {
        defaults.Required = required;
    }

    protected override void Define(Root root) {
        var box = root.AddGroup<Box>();
        if (parts is null) {
            box.Add<Item>().And<Item.Small>().And<Tag>().InParentFolder();
        } else {
            parts(box);
        }
    }

    protected override void Rules(RuleBook book) => rules?.Invoke(book);
}

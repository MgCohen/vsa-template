namespace Umpire.Architecture.Blocks;

public abstract record Block : Concept {
    protected virtual void Define(Part part) { }

    protected virtual void Rules(RuleBook rules) { }

    internal void DefinePart(Part part) => Levels.Call<Block, Part>(this, nameof(Define), part);

    internal void AddRules(RuleBook rules) => Levels.Call<Block, RuleBook>(this, nameof(Rules), rules);
}

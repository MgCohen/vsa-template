namespace VSA.Architecture.Data;

public class JsonDefinition : ArchitectureDefinition {
    readonly string file;
    readonly MissingTypes missing;
    DefinitionFile? data;
    TypeResolver? types;

    public JsonDefinition(string file, MissingTypes missing = MissingTypes.Fail) {
        this.file = file;
        this.missing = missing;
    }

    public JsonDefinition(DefinitionFile data, MissingTypes missing = MissingTypes.Fail) {
        file = "";
        this.data = data;
        this.missing = missing;
    }

    public DefinitionFile Data => data ??= DefinitionJson.Read(File.ReadAllText(FilePath));

    public string FilePath => Path.IsPathRooted(file)
        ? file
        : Path.Combine(Path.GetDirectoryName(GetType().Assembly.Location) ?? AppContext.BaseDirectory, file);

    protected override string RootPath => Data.Root;

    protected override void Configure(Options options) {
        options.DefaultPermission = Data.Options.DefaultPermission;
        options.Boundary = Data.Options.Boundary;
        options.UseSameConcept = Data.Options.UseSameConcept;
        options.Depth = Data.Options.Depth;
        options.Access = Data.Options.Access;
    }

    protected override void Define(Root root) {
        foreach (var part in Data.Parts) {
            Add(root, part);
        }
    }

    protected override void Rules(RuleBook rules) {
        foreach (var rule in Data.Rules) {
            Replay(rules, rule);
        }
    }

    Type TypeOf(string name) => (types ??= new TypeResolver(Data, missing)).Of(name);

    void Add(Part parent, PartNode node) {
        var concept = (Concept)Activator.CreateInstance(TypeOf(node.Concept))!;
        var part = parent.AddData(concept, node.Folder, node.Group, node.Depth, node.Access, node.Boundary);
        foreach (var type in node.Types) {
            part.OfType(TypeOf(type));
        }
        foreach (var child in node.Children) {
            Add(part, child);
        }
    }

    void Replay(RuleBook rules, RuleNode rule) {
        var first = rule.From[0];
        var from = rules.For(TypeOf(first.Concept), first.Depth);
        Filters(from, first);
        Rest(from, rule.From);
        var to = rule.To[0];
        if (rule.Verb == RuleVerb.Uses) {
            var uses = from.Uses(TypeOf(to.Concept), to.Access, to.Depth, to.Boundary);
            Filters(uses, to);
            Rest(uses, rule.To);
            return;
        }
        var denies = from.Denies(TypeOf(to.Concept), to.Access, to.Depth, to.Boundary);
        Filters(denies, to);
        Rest(denies, rule.To);
    }

    void Rest<TSelf>(Chain<TSelf> chain, List<TermNode> terms) where TSelf : Chain<TSelf> {
        foreach (var term in terms.Skip(1)) {
            chain.And(TypeOf(term.Concept), term.Access, term.Depth, term.Boundary);
            Filters(chain, term);
        }
    }

    void Filters<TSelf>(Chain<TSelf> chain, TermNode term) where TSelf : Chain<TSelf> {
        foreach (var filter in term.Filters) {
            chain.Filter(filter.Kind, TypeOf(filter.Concept));
        }
    }
}

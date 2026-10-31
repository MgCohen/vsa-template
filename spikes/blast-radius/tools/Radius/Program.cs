// Radius: predicts the blast radius of a change to one member, and applies that change for verification.
//   list    <root>                                   members that can be changed (declared in src/)
//   predict <graphDir> <root> <Type> <Member> [--naive]
//   mutate  <srcRoot> <Type> <Member> <rename|body>
//   export  <graphDir> <root>                         improved graph as JSON (for the prototype)
// Graph: ArchUnitNET call edges (IL). Improved mode adds: interface/override dispatch edges,
// const references and type names in string literals (both from Roslyn, because IL does not keep them).
using ArchUnitNET.Domain;
using ArchUnitNET.Domain.Dependencies;
using ArchUnitNET.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

switch (args[0])
{
    case "list": List(args[1]); break;
    case "predict": Predict(args[1], args[2], args[3], args[4], args.Contains("--naive")); break;
    case "mutate": Mutate(args[1], args[2], args[3], args[4]); break;
    case "export": Export(args[1], args[2]); break;
}

// Member key: "Type.Member" without parameters or accessor prefix, so "get_Doubled()" and "Doubled" match.
static string Short(string name)
{
    name = name.Split('(')[0];
    foreach (var p in new[] { "get_", "set_", "add_", "remove_" }) if (name.StartsWith(p)) return name[p.Length..];
    return name;
}
static string Key(IMember m) => $"{m.DeclaringType.Name}.{Short(m.Name)}";

static IEnumerable<SyntaxTree> Sources(string root, params string[] dirs) =>
    dirs.SelectMany(d => Directory.GetFiles(Path.Combine(root, d), "*.cs", SearchOption.AllDirectories))
        .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") && !f.Contains("/obj/"))
        .Select(f => CSharpSyntaxTree.ParseText(File.ReadAllText(f), path: f));

static void List(string root)
{
    foreach (var tree in Sources(root, "src"))
        foreach (var type in tree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>())
            foreach (var m in type.Members)
            {
                var name = m switch
                {
                    MethodDeclarationSyntax md => md.Identifier.Text,
                    PropertyDeclarationSyntax pd when pd.ExpressionBody != null => pd.Identifier.Text,
                    FieldDeclarationSyntax fd when fd.Modifiers.Any(SyntaxKind.ConstKeyword) => fd.Declaration.Variables[0].Identifier.Text,
                    _ => null
                };
                if (name != null) Console.WriteLine($"{type.Identifier.Text} {name}");
            }
}

static (Dictionary<string, (string Asm, bool Test, bool Public)> Nodes, List<(string From, string To, string Kind)> Edges, Architecture Arch)
    BuildGraph(string dir, string root, bool naive)
{
    var arch = new ArchLoader().LoadFilteredDirectory(dir, "Foo.*.dll").Build();
    var nodes = new Dictionary<string, (string Asm, bool Test, bool Public)>();
    foreach (var t in arch.Types.Where(t => !t.Name.Contains('<')))
        foreach (var m in t.Members)
            nodes[Key(m)] = (t.Assembly.Name, t.Assembly.Name.EndsWith(".Tests"),
                m.Visibility == Visibility.Public && t.Visibility == Visibility.Public);

    // Edges point from user to used: (From, To, Kind).
    var edges = arch.Types.SelectMany(t => t.Dependencies).OfType<MethodCallDependency>()
        .Select(c => (From: Key(c.OriginMember), To: Key(c.TargetMember), Kind: "call")).ToList();

    if (!naive)
    {
        // Dispatch: a call to an interface or base member can run the implementation.
        foreach (var t in arch.Classes)
            foreach (var m in t.Members)
            {
                foreach (var i in t.ImplementedInterfaces)
                    foreach (var im in i.Members.Where(im => Short(im.Name) == Short(m.Name)))
                        edges.Add((Key(im), Key(m), "dispatch"));
                if (t.BaseClass is { } b)
                    foreach (var bm in b.Members.Where(bm => Short(bm.Name) == Short(m.Name) && !bm.Name.StartsWith(".ctor")))
                        edges.Add((Key(bm), Key(m), "dispatch"));
            }

        // Roslyn: const references and type names in strings.
        var refs = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
            .Select(p => (MetadataReference)MetadataReference.CreateFromFile(p)).ToList();
        var trees = Sources(root, "src", "tests").ToList();
        var comp = CSharpCompilation.Create("all", trees, refs);
        var typeByFullName = arch.Types.ToDictionary(t => t.FullName, t => t);
        foreach (var tree in trees)
        {
            var model = comp.GetSemanticModel(tree);
            foreach (var node in tree.GetRoot().DescendantNodes())
            {
                var from = Enclosing(node, model);
                if (from == null) continue;
                if (node is IdentifierNameSyntax id && model.GetSymbolInfo(id).Symbol is IFieldSymbol { IsConst: true } f)
                    edges.Add((from, $"{f.ContainingType.Name}.{f.Name}", "const"));
                if (node is LiteralExpressionSyntax lit && lit.IsKind(SyntaxKind.StringLiteralExpression)
                    && typeByFullName.TryGetValue(lit.Token.ValueText.Split(',')[0].Trim(), out var named))
                    foreach (var nm in named.Members) edges.Add((from, Key(nm), "string"));
            }
        }
    }

    return (nodes, edges, arch);
}

// Writes the improved graph as JSON for the interactive prototype. Edges of kind "call" are the naive graph.
static void Export(string dir, string root)
{
    var (nodes, edges, arch) = BuildGraph(dir, root, naive: false);
    var layerOf = arch.Types.Where(t => !t.Name.Contains('<')).ToDictionary(t => t.Name, t => t.Namespace.FullName.Split('.').Last());
    var source = new HashSet<string>();
    foreach (var tree in Sources(root, "src", "tests"))
        foreach (var type in tree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>())
            foreach (var m in type.Members)
                switch (m)
                {
                    case MethodDeclarationSyntax md: source.Add($"{type.Identifier.Text}.{md.Identifier.Text}"); break;
                    case PropertyDeclarationSyntax pd: source.Add($"{type.Identifier.Text}.{pd.Identifier.Text}"); break;
                    case FieldDeclarationSyntax fd when fd.Modifiers.Any(SyntaxKind.ConstKeyword):
                        source.Add($"{type.Identifier.Text}.{fd.Declaration.Variables[0].Identifier.Text}"); break;
                }
    var keep = nodes.Where(n => source.Contains(n.Key)).ToDictionary(n => n.Key, n => n.Value);
    var json = new
    {
        nodes = keep.Select(n => new
        {
            id = n.Key, module = n.Value.Asm.Split('.')[1], layer = n.Value.Test ? "Tests" : layerOf[n.Key.Split('.')[0]],
            test = n.Value.Test, @public = n.Value.Public
        }),
        edges = edges.Where(e => keep.ContainsKey(e.From) && keep.ContainsKey(e.To) && e.From != e.To).Distinct()
            .Select(e => new { from = e.From, to = e.To, kind = e.Kind })
    };
    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(json));
}

static void Predict(string dir, string root, string type, string member, bool naive)
{
    var (nodes, edges, _) = BuildGraph(dir, root, naive);
    var key = $"{type}.{member}";
    var info = nodes.GetValueOrDefault(key);

    // Compile radius: code that names the member directly.
    var compile = edges.Where(e => e.To == key && e.Kind is "call" or "const")
        .Select(e => e.From.Split('.')[0]).Append(type).Distinct().OrderBy(x => x).ToList();

    // Behavior radius: everything that can reach the member (reverse graph, transitive).
    var via = new Dictionary<string, (string Next, string Kind)>();
    var seen = new HashSet<string> { key };
    var queue = new Queue<string>(new[] { key });
    while (queue.Count > 0)
    {
        var cur = queue.Dequeue();
        foreach (var e in edges.Where(e => e.To == cur && seen.Add(e.From)))
        {
            via[e.From] = (cur, e.Kind);
            queue.Enqueue(e.From);
        }
    }
    bool IsTest(string k) => nodes.TryGetValue(k, out var n) && n.Test;
    var exits = seen.Where(k => k != key && !IsTest(k) && nodes.TryGetValue(k, out var n) && n.Public).OrderBy(x => x).ToList();
    var tests = seen.Where(k => IsTest(k) && !k.Contains(".ctor")).OrderBy(x => x).ToList();

    Console.WriteLine($"target: {key}  [{(info.Public ? "public API" : "internal")}] in {info.Asm}  mode={(naive ? "naive" : "improved")}");
    Console.WriteLine($"compile: {string.Join(", ", compile)}");
    Console.WriteLine($"public-exits: {string.Join(", ", exits)}");
    Console.WriteLine($"behavior-tests: {(tests.Count > 0 ? string.Join(", ", tests) : "<none: not covered by any test>")}");
    foreach (var t in tests)
    {
        var chain = new List<string> { t };
        var c = t;
        while (via.TryGetValue(c, out var p)) { chain.Add($"-{p.Kind}-> {p.Next}"); c = p.Next; }
        Console.WriteLine($"  path: {string.Join(" ", chain)}");
    }
}

// The member that contains a syntax node, as a key ("Type.Member"). Field initializers count as the constructor.
static string? Enclosing(SyntaxNode node, SemanticModel model)
{
    var decl = node.Ancestors().FirstOrDefault(a => a is BaseMethodDeclarationSyntax or PropertyDeclarationSyntax or FieldDeclarationSyntax);
    var type = node.Ancestors().OfType<BaseTypeDeclarationSyntax>().FirstOrDefault();
    if (decl == null || type == null) return null;
    var member = decl switch
    {
        MethodDeclarationSyntax m => m.Identifier.Text,
        PropertyDeclarationSyntax p => p.Identifier.Text,
        _ => ".ctor"
    };
    return $"{type.Identifier.Text}.{member}";
}

static void Mutate(string root, string type, string member, string kind)
{
    var file = Directory.GetFiles(root, type + ".cs", SearchOption.AllDirectories).Single();
    var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(file));
    var rootNode = tree.GetRoot();
    static ExpressionSyntax Bump(ExpressionSyntax e, string returnType) =>
        SyntaxFactory.ParseExpression(returnType == "string" ? $"({e}) + \"!\"" : $"({e}) + 1");
    SyntaxNode oldNode, newNode;
    if (rootNode.DescendantNodes().OfType<MethodDeclarationSyntax>().FirstOrDefault(m => m.Identifier.Text == member) is { } method)
    {
        oldNode = method;
        newNode = kind == "rename" ? method.WithIdentifier(SyntaxFactory.Identifier(member + "Renamed"))
            : method.WithExpressionBody(method.ExpressionBody!.WithExpression(Bump(method.ExpressionBody.Expression, method.ReturnType.ToString())));
    }
    else if (rootNode.DescendantNodes().OfType<PropertyDeclarationSyntax>().FirstOrDefault(p => p.Identifier.Text == member) is { } prop)
    {
        oldNode = prop;
        newNode = kind == "rename" ? prop.WithIdentifier(SyntaxFactory.Identifier(member + "Renamed"))
            : prop.WithExpressionBody(prop.ExpressionBody!.WithExpression(Bump(prop.ExpressionBody.Expression, prop.Type.ToString())));
    }
    else
    {
        var v = rootNode.DescendantNodes().OfType<VariableDeclaratorSyntax>().First(v => v.Identifier.Text == member);
        var fieldType = ((VariableDeclarationSyntax)v.Parent!).Type.ToString();
        oldNode = v;
        newNode = kind == "rename" ? v.WithIdentifier(SyntaxFactory.Identifier(member + "Renamed"))
            : v.WithInitializer(v.Initializer!.WithValue(Bump(v.Initializer.Value, fieldType)));
    }
    File.WriteAllText(file, rootNode.ReplaceNode(oldNode, newNode).ToFullString());
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace VSA.Sdk.Tasks;

// One slice file: its full path and its text.
public sealed class SliceSource(string path, string text) {
    public string Path { get; } = path;
    public string Text { get; } = text;
}

// One routing error. MSBuild reports it as VSA0005 at the file.
public sealed class SliceError(string file, string message) {
    public string File { get; } = file;
    public string Message { get; } = message;
    public override string ToString() => Message;
}

public sealed class SliceRouting {
    // The full path of each slice file -> its layer: Contracts/Requests, Contracts/Events, Contracts/Commands,
    // Handlers/Requests, Handlers/Events or Handlers/Commands.
    public Dictionary<string, string> Layers { get; } = new(StringComparer.Ordinal);
    public List<SliceError> Errors { get; } = [];
}

// Selects the project of each file in Slices/ by the types in the file:
//   IRequest<T>, IResponse                 Contracts/Requests
//   IntegrationEvent                       Contracts/Events
//   ICommand<T>                            Contracts/Commands
//   a type that a contract type uses       the contracts project of that contract type (a DTO, a command result)
//   other files of a slice                 Handlers/Requests (RequestHandler), Handlers/Events (IntegrationEventHandler)
//                                          or Handlers/Commands (CommandHandler)
// The router runs before the compile, so it has no semantic model. It reads the Roslyn syntax tree and matches types by
// their simple names: first in the same slice, then in Slices/Shared/. The compiler and VSA1003 check the result.
// A type can inherit a marker through other types, for example DealDamage : Message : IRequest<X>. The router follows
// the base names through the slice files and the known files (the layer folders of the plugin modules), to any depth.
// A plugin module has no slices: VSA.Sdk compiles its layer folders, and the router does not run.
public static class SliceRouter {
    public const string ContractsRequests = "Contracts/Requests";
    public const string ContractsEvents = "Contracts/Events";
    public const string HandlersRequests = "Handlers/Requests";
    public const string HandlersEvents = "Handlers/Events";
    public const string ContractsCommands = "Contracts/Commands";
    public const string HandlersCommands = "Handlers/Commands";
    public const string Shared = "Shared";

    static readonly CSharpParseOptions Parse = new(LanguageVersion.Preview);

    enum Kind { Other, Request, Response, Event, Command, RequestHandler, EventHandler, CommandHandler }

    sealed class Declaration(string name, List<string> bases, HashSet<string> uses) {
        public string Name { get; } = name;
        public List<string> Bases { get; } = bases;
        public Kind Kind { get; set; }
        public HashSet<string> Uses { get; } = uses;
        public string File { get; set; } = "";
        public string Slice { get; set; } = "";
    }

    public static SliceRouting Route(string slicesDir, IEnumerable<SliceSource> sources) => Route(slicesDir, sources, []);

    // known: files that are not routed, but whose types can be bases (the layer folders of the plugin modules).
    public static SliceRouting Route(string slicesDir, IEnumerable<SliceSource> sources, IEnumerable<SliceSource> known) {
        var routing = new SliceRouting();
        var root = Path.GetFullPath(slicesDir);
        var declarations = new List<Declaration>();
        var sliceOf = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var source in sources) {
            var path = Path.GetFullPath(source.Path);
            var relative = Relative(root, path);
            var slash = relative.IndexOf('/');
            if (slash < 0) {
                routing.Errors.Add(new(path, $"{relative} is directly in Slices/. Move it into a slice folder, for example Slices/<Slice>/{relative}."));
                continue;
            }
            var slice = relative.Substring(0, slash);
            sliceOf[path] = slice;
            foreach (var declaration in Scan(source.Text)) {
                declaration.File = path;
                declaration.Slice = slice;
                declarations.Add(declaration);
            }
        }
        var bases = declarations.Concat(known.SelectMany(k => Scan(k.Text))).GroupBy(d => d.Name)
            .ToDictionary(g => g.Key, g => g.SelectMany(d => d.Bases).Distinct().ToList(), StringComparer.Ordinal);
        foreach (var d in declarations) {
            d.Kind = KindOf(d.Bases, bases);
        }
        var byFile = declarations.GroupBy(d => d.File).ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);
        var byName = declarations.GroupBy(d => d.Name).ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);

        // 1. The marked contract types. 2. The types that they use, to any depth.
        var queue = new Queue<string>();
        foreach (var d in declarations) {
            if (ContractLayer(d.Kind) is { } layer && Assign(routing, byFile, d.File, layer, d.Name)) {
                queue.Enqueue(d.File);
            }
        }
        var visited = new HashSet<string>(StringComparer.Ordinal);
        while (queue.Count > 0) {
            var file = queue.Dequeue();
            if (!visited.Add(file)) {
                continue;
            }
            var layer = routing.Layers[file];
            foreach (var d in byFile[file]) {
                foreach (var name in d.Uses) {
                    if (name == d.Name || !byName.TryGetValue(name, out var found) || Pick(routing, d, name, found) is not { } used || used.File == file) {
                        continue;
                    }
                    if (IsHandler(used.Kind)) {
                        routing.Errors.Add(new(d.File, $"{d.Name} is a contract type, but it uses the handler {used.Name}. A contract does not use a handler."));
                    } else if (routing.Layers.TryGetValue(used.File, out var current)) {
                        if (current != layer) {
                            var user = layer switch {
                                ContractsRequests => "a request or a response",
                                ContractsCommands => "a command",
                                _ => "an event",
                            };
                            routing.Errors.Add(new(used.File, $"{used.Name} is used by {user} ({d.Name}) and by a type in {current}. A type can be in one contracts project only. Make two types."));
                        }
                    } else if (Assign(routing, byFile, used.File, layer, used.Name)) {
                        queue.Enqueue(used.File);
                    }
                }
            }
        }

        // 3. The other files of a slice go to the handlers project of the slice. A slice has one kind of handler.
        foreach (var slice in sliceOf.GroupBy(p => p.Value, p => p.Key)) {
            var open = slice.Where(f => !routing.Layers.ContainsKey(f)).OrderBy(f => f, StringComparer.Ordinal).ToList();
            if (open.Count == 0) {
                continue;
            }
            if (slice.Key == Shared) {
                routing.Errors.AddRange(open.Select(f => new SliceError(f,
                    $"Slices/Shared/ holds only contracts: requests, responses, events, commands and the types that they use. {Path.GetFileName(f)} is none of them. Move it into the slice that uses it.")));
                continue;
            }
            var kinds = open.Where(byFile.ContainsKey).SelectMany(f => byFile[f]).Select(d => d.Kind).Where(IsHandler).Distinct().ToList();
            if (kinds.Count > 1) {
                routing.Errors.Add(new(open[0],
                    $"Slice {slice.Key} has {string.Join(" and ", kinds.OrderBy(k => k).Select(HandlerName))}. A slice has one kind of handler. Move one of them to its own slice."));
            } else if (kinds.Count == 0) {
                routing.Errors.AddRange(open.Select(f => new SliceError(f,
                    $"Slice {slice.Key} has no handler, so {Path.GetFileName(f)} has no project. Add a RequestHandler, an IntegrationEventHandler or a CommandHandler to the slice, or move the file.")));
            } else {
                var handlers = kinds[0] switch {
                    Kind.RequestHandler => HandlersRequests,
                    Kind.CommandHandler => HandlersCommands,
                    _ => HandlersEvents,
                };
                foreach (var f in open) {
                    routing.Layers[f] = handlers;
                }
            }
        }
        return routing;
    }

    // The declaration that a name means: the same slice first, then Slices/Shared/. A type of another slice is an error.
    static Declaration? Pick(SliceRouting routing, Declaration user, string name, List<Declaration> found) {
        var same = found.Where(f => f.Slice == user.Slice).ToList();
        if (same.Count == 0) {
            same = found.Where(f => f.Slice == Shared).ToList();
        }
        if (same.Count == 1) {
            return same[0];
        }
        if (same.Count > 1) {
            routing.Errors.Add(new(user.File, $"{user.Name} uses {name}, and slice {same[0].Slice} has more than one type with that name. Rename one of them."));
            return null;
        }
        routing.Errors.Add(new(user.File,
            $"{user.Name} in slice {user.Slice} uses {name} from slice {found[0].Slice}. A slice uses only its own types and Slices/Shared/. Move {name} to Slices/Shared/."));
        return null;
    }

    static bool Assign(SliceRouting routing, Dictionary<string, List<Declaration>> byFile, string file, string layer, string reason) {
        if (routing.Layers.TryGetValue(file, out var current)) {
            if (current != layer) {
                routing.Errors.Add(new(file, $"{Path.GetFileName(file)} has types for {current} and for {layer} ({reason}). Put each in its own file."));
            }
            return false;
        }
        if (byFile[file].FirstOrDefault(d => IsHandler(d.Kind)) is { } handler) {
            routing.Errors.Add(new(file, $"{Path.GetFileName(file)} has the contract type {reason} and the handler {handler.Name}. Put each in its own file."));
            return false;
        }
        routing.Layers[file] = layer;
        return true;
    }

    static string? ContractLayer(Kind kind) => kind switch {
        Kind.Request or Kind.Response => ContractsRequests,
        Kind.Event => ContractsEvents,
        Kind.Command => ContractsCommands,
        _ => null,
    };

    static bool IsHandler(Kind kind) => kind is Kind.RequestHandler or Kind.EventHandler or Kind.CommandHandler;

    static string HandlerName(Kind kind) => kind switch {
        Kind.RequestHandler => "a request handler",
        Kind.CommandHandler => "a command handler",
        _ => "an event handler",
    };

    public static string Relative(string root, string path) {
        var r = root.Replace('\\', '/').TrimEnd('/') + "/";
        var p = path.Replace('\\', '/');
        return p.StartsWith(r, StringComparison.OrdinalIgnoreCase) ? p.Substring(r.Length) : p;
    }

    // ---- The syntax scan ----

    static IEnumerable<Declaration> Scan(string text) {
        var tree = CSharpSyntaxTree.ParseText(text, Parse);
        foreach (var type in tree.GetRoot().DescendantNodes().OfType<BaseTypeDeclarationSyntax>()) {
            var bases = type.BaseList?.Types.Select(b => SimpleName(b.Type)).OfType<string>().ToList() ?? [];
            var uses = new HashSet<string>(type.DescendantNodes().OfType<SimpleNameSyntax>()
                .Where(n => SyntaxFacts.IsInTypeOnlyContext(n))
                .Select(n => n.Identifier.ValueText), StringComparer.Ordinal);
            yield return new Declaration(type.Identifier.ValueText, bases, uses);
        }
    }

    // The kind of a type from its base names. A base that is not a marker counts with its own bases, to any depth.
    // seen stops a loop, for example A : B and B : A.
    static Kind KindOf(List<string> names, Dictionary<string, List<string>> bases) {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var all = new List<string>();
        var queue = new Queue<string>(names);
        while (queue.Count > 0) {
            var name = queue.Dequeue();
            if (!seen.Add(name)) {
                continue;
            }
            all.Add(name);
            if (bases.TryGetValue(name, out var next)) {
                foreach (var b in next) {
                    queue.Enqueue(b);
                }
            }
        }
        return all.Contains("RequestHandler") ? Kind.RequestHandler
            : all.Contains("IntegrationEventHandler") ? Kind.EventHandler
            : all.Contains("CommandHandler") ? Kind.CommandHandler
            : all.Contains("IRequest") ? Kind.Request
            : all.Contains("ICommand") ? Kind.Command
            : all.Contains("IResponse") ? Kind.Response
            : all.Contains("IntegrationEvent") ? Kind.Event
            : Kind.Other;
    }

    // VSA.Contracts.Requests.IRequest<X> gives IRequest.
    static string? SimpleName(TypeSyntax type) => type switch {
        SimpleNameSyntax simple => simple.Identifier.ValueText,
        QualifiedNameSyntax qualified => qualified.Right.Identifier.ValueText,
        AliasQualifiedNameSyntax alias => alias.Name.Identifier.ValueText,
        _ => null,
    };
}

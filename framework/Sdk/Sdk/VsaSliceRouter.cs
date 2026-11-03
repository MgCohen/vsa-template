// The slice router of VSA.Sdk. MSBuild compiles this file as an inline task (RoslynCodeTaskFactory),
// so it uses only .NET Standard 2.0 and C# 7.3. Rules.Tests compiles it with VSA_ROUTER_TESTS to test SliceRouter alone.
//
// A slice module keeps each feature in one folder, Slices/<Slice>/, with any file names. The type in a file selects its project:
//   IRequest<...>, IResponse               Contracts/Requests
//   IntegrationEvent                       Contracts/Events
//   a type that a contract type uses       the contracts project of that contract type (DTOs)
//   other files of a slice                 Handlers/Requests (RoutedHandler) or Handlers/Events (IntegrationEventHandler)
// The scanner reads the syntax only, with no compiler. It matches types by their simple names.
#if VSA_ROUTER_TESTS
#nullable disable
#endif
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
#if !VSA_ROUTER_TESTS
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
#endif

namespace VSA.Sdk
{
#if !VSA_ROUTER_TESTS
    // Gives the files of one layer. With ReportErrors, it also reports each routing error as VSA0003.
    public class VsaRouteSliceFiles : Task
    {
        [Required] public string SlicesDir { get; set; }
        public ITaskItem[] Files { get; set; }
        public string Layer { get; set; }
        public bool ReportErrors { get; set; }
        [Output] public ITaskItem[] LayerFiles { get; set; }

        public override bool Execute()
        {
            var sources = (Files ?? new ITaskItem[0])
                .Select(f => f.GetMetadata("FullPath"))
                .Select(path => new SliceSource(path, File.ReadAllText(path)))
                .ToList();
            var routing = SliceRouter.Route(SlicesDir, sources);
            if (ReportErrors)
            {
                foreach (var error in routing.Errors)
                {
                    Log.LogError(null, "VSA0003", null, error.File, 0, 0, 0, 0, error.Message);
                }
            }
            var root = Path.GetFullPath(SlicesDir);
            LayerFiles = routing.Layers
                .Where(pair => pair.Value == Layer)
                .Select(pair =>
                {
                    var item = new TaskItem(pair.Key);
                    item.SetMetadata("Link", "Slices/" + SliceRouter.Relative(root, pair.Key));
                    return (ITaskItem)item;
                })
                .ToArray();
            return !Log.HasLoggedErrors;
        }
    }
#endif

    public sealed class SliceSource
    {
        public SliceSource(string path, string text) { Path = path; Text = text; }
        public string Path { get; private set; }
        public string Text { get; private set; }
    }

    public sealed class SliceError
    {
        public SliceError(string file, string message) { File = file; Message = message; }
        public string File { get; private set; }
        public string Message { get; private set; }
        public override string ToString() { return Message; }
    }

    public sealed class SliceRouting
    {
        // Full path of each file -> its layer: Contracts/Requests, Contracts/Events, Handlers/Requests or Handlers/Events.
        public Dictionary<string, string> Layers = new Dictionary<string, string>(StringComparer.Ordinal);
        public List<SliceError> Errors = new List<SliceError>();
    }

    public static class SliceRouter
    {
        public const string ContractsRequests = "Contracts/Requests";
        public const string ContractsEvents = "Contracts/Events";
        public const string HandlersRequests = "Handlers/Requests";
        public const string HandlersEvents = "Handlers/Events";
        public const string Shared = "Shared";

        enum Kind { Other, Request, Response, Event, RequestHandler, EventHandler }

        sealed class Declaration
        {
            public string Name;
            public Kind Kind;
            public string File;
            public string Slice;
            public HashSet<string> Uses;
        }

        public static SliceRouting Route(string slicesDir, IEnumerable<SliceSource> sources)
        {
            var routing = new SliceRouting();
            var root = Path.GetFullPath(slicesDir);
            var declarations = new List<Declaration>();
            var sliceOf = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var source in sources)
            {
                var path = Path.GetFullPath(source.Path);
                var relative = Relative(root, path);
                var slash = relative.IndexOf('/');
                if (slash < 0)
                {
                    routing.Errors.Add(new SliceError(path, relative + " is directly in Slices/. Move it into a slice folder, for example Slices/<Feature>/" + relative + "."));
                    continue;
                }
                var slice = relative.Substring(0, slash);
                sliceOf[path] = slice;
                foreach (var declaration in Scan(source.Text))
                {
                    declaration.File = path;
                    declaration.Slice = slice;
                    declarations.Add(declaration);
                }
            }
            var byFile = declarations.GroupBy(d => d.File).ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);
            var byName = declarations.GroupBy(d => d.Name).ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);

            // 1. The marked contract types, then 2. the types that they use, to any depth.
            var queue = new Queue<string>();
            foreach (var d in declarations)
            {
                var layer = ContractLayer(d.Kind);
                if (layer != null && Assign(routing, byFile, d.File, layer, d.Name))
                {
                    queue.Enqueue(d.File);
                }
            }
            var visited = new HashSet<string>(StringComparer.Ordinal);
            while (queue.Count > 0)
            {
                var file = queue.Dequeue();
                if (!visited.Add(file)) continue;
                var layer = routing.Layers[file];
                foreach (var d in byFile[file])
                {
                    foreach (var name in d.Uses)
                    {
                        List<Declaration> found;
                        if (name == d.Name || !byName.TryGetValue(name, out found)) continue;
                        var used = Pick(routing, d, name, found);
                        if (used == null || used.File == file) continue;
                        if (IsHandler(used.Kind))
                        {
                            routing.Errors.Add(new SliceError(d.File, d.Name + " is a contract type, but it uses the handler " + used.Name + ". A contract does not use a handler."));
                            continue;
                        }
                        string current;
                        if (routing.Layers.TryGetValue(used.File, out current))
                        {
                            if (current != layer)
                            {
                                routing.Errors.Add(new SliceError(used.File, used.Name + " is used by " + (layer == ContractsRequests ? "a request or a response" : "an event") + " (" + d.Name + ") and by a type in " + current + ". A type can be in one contracts project only. Make two types."));
                            }
                            continue;
                        }
                        if (Assign(routing, byFile, used.File, layer, used.Name)) queue.Enqueue(used.File);
                    }
                }
            }

            // 3. The other files of a slice go to the handlers project of the slice. A slice has one handler kind.
            foreach (var slice in sliceOf.GroupBy(p => p.Value, p => p.Key))
            {
                var open = slice.Where(f => !routing.Layers.ContainsKey(f)).OrderBy(f => f, StringComparer.Ordinal).ToList();
                if (open.Count == 0) continue;
                if (slice.Key == Shared)
                {
                    foreach (var f in open)
                    {
                        routing.Errors.Add(new SliceError(f, "Slices/Shared/ holds only contracts: requests, responses, events and the types that they use. " + Path.GetFileName(f) + " is none of them. Move it into the slice that uses it."));
                    }
                    continue;
                }
                var kinds = open.Where(byFile.ContainsKey).SelectMany(f => byFile[f]).Select(d => d.Kind).Where(IsHandler).Distinct().ToList();
                if (kinds.Count == 2)
                {
                    routing.Errors.Add(new SliceError(open[0], "Slice " + slice.Key + " has a request handler and an event handler. A slice has one kind of handler. Move one of them to its own slice."));
                    continue;
                }
                if (kinds.Count == 0)
                {
                    foreach (var f in open)
                    {
                        routing.Errors.Add(new SliceError(f, "Slice " + slice.Key + " has no handler, so " + Path.GetFileName(f) + " has no project. Add a RoutedHandler or an IntegrationEventHandler to the slice, or move the file."));
                    }
                    continue;
                }
                var handlers = kinds[0] == Kind.RequestHandler ? HandlersRequests : HandlersEvents;
                foreach (var f in open) routing.Layers[f] = handlers;
            }
            return routing;
        }

        // The declaration that a name means: the same slice first, then Slices/Shared/. A type in another slice is an error.
        static Declaration Pick(SliceRouting routing, Declaration user, string name, List<Declaration> found)
        {
            var same = found.Where(f => f.Slice == user.Slice).ToList();
            if (same.Count == 0) same = found.Where(f => f.Slice == Shared).ToList();
            if (same.Count == 1) return same[0];
            if (same.Count > 1)
            {
                routing.Errors.Add(new SliceError(user.File, user.Name + " uses " + name + ", and slice " + same[0].Slice + " has more than one type with that name. Rename one of them."));
                return null;
            }
            var other = found[0];
            routing.Errors.Add(new SliceError(user.File, user.Name + " in slice " + user.Slice + " uses " + name + " from slice " + other.Slice + ". A slice uses only its own types and Slices/Shared/. Move " + name + " to Slices/Shared/."));
            return null;
        }

        static bool Assign(SliceRouting routing, Dictionary<string, List<Declaration>> byFile, string file, string layer, string reason)
        {
            string current;
            if (routing.Layers.TryGetValue(file, out current))
            {
                if (current == layer) return false;
                routing.Errors.Add(new SliceError(file, Path.GetFileName(file) + " has types for " + current + " and for " + layer + " (" + reason + "). Put each in its own file."));
                return false;
            }
            var handler = byFile[file].FirstOrDefault(d => IsHandler(d.Kind));
            if (handler != null)
            {
                routing.Errors.Add(new SliceError(file, Path.GetFileName(file) + " has the contract type " + reason + " and the handler " + handler.Name + ". Put each in its own file."));
                return false;
            }
            routing.Layers[file] = layer;
            return true;
        }

        static string ContractLayer(Kind kind)
        {
            if (kind == Kind.Request || kind == Kind.Response) return ContractsRequests;
            if (kind == Kind.Event) return ContractsEvents;
            return null;
        }

        static bool IsHandler(Kind kind) { return kind == Kind.RequestHandler || kind == Kind.EventHandler; }

        public static string Relative(string root, string path)
        {
            var r = root.Replace('\\', '/').TrimEnd('/') + "/";
            var p = path.Replace('\\', '/');
            return p.StartsWith(r, StringComparison.OrdinalIgnoreCase) ? p.Substring(r.Length) : p;
        }

        // ---- The scanner ----

        static readonly Regex DeclarationStart = new Regex(@"\b(?:class|struct|interface|record)\s+(?:(?:class|struct)\s+)?([A-Za-z_]\w*)", RegexOptions.Compiled);
        static readonly Regex TypeName = new Regex(@"\b[A-Z]\w*\b", RegexOptions.Compiled);
        static readonly Regex Where = new Regex(@"\bwhere\b", RegexOptions.Compiled);

        static IEnumerable<Declaration> Scan(string text)
        {
            var code = StripCode(text);
            foreach (Match match in DeclarationStart.Matches(code))
            {
                var name = match.Groups[1].Value;
                var end = HeaderEnd(code, match.Index + match.Length);
                var header = code.Substring(match.Index + match.Length, end - (match.Index + match.Length));
                var body = "";
                if (end < code.Length && code[end] == '{')
                {
                    var close = Matching(code, end);
                    body = code.Substring(end, close - end);
                }
                var bases = BaseNames(header);
                var uses = new HashSet<string>(StringComparer.Ordinal);
                foreach (Match m in TypeName.Matches(header + " " + body)) uses.Add(m.Value);
                yield return new Declaration { Name = name, Kind = KindOf(bases), Uses = uses };
            }
        }

        static Kind KindOf(List<string> bases)
        {
            if (bases.Contains("RoutedHandler")) return Kind.RequestHandler;
            if (bases.Contains("IntegrationEventHandler")) return Kind.EventHandler;
            if (bases.Contains("IRequest")) return Kind.Request;
            if (bases.Contains("IResponse")) return Kind.Response;
            if (bases.Contains("IntegrationEvent")) return Kind.Event;
            return Kind.Other;
        }

        // The simple names of the base list: "IRequest<X>, VSA.Contracts.Requests.IResponse" gives IRequest, IResponse.
        static List<string> BaseNames(string header)
        {
            var names = new List<string>();
            int parens = 0, angles = 0, colon = -1;
            for (var i = 0; i < header.Length; i++)
            {
                var c = header[i];
                if (c == '(') parens++;
                else if (c == ')') parens--;
                else if (c == '<') angles++;
                else if (c == '>') angles--;
                else if (c == ':' && parens == 0 && angles == 0) { colon = i; break; }
            }
            if (colon < 0) return names;
            var list = header.Substring(colon + 1);
            var where = Where.Match(list);
            if (where.Success) list = list.Substring(0, where.Index);
            var start = 0;
            angles = 0;
            parens = 0;
            for (var i = 0; i <= list.Length; i++)
            {
                var c = i < list.Length ? list[i] : ',';
                if (c == '<') angles++;
                else if (c == '>') angles--;
                else if (c == '(') parens++;
                else if (c == ')') parens--;
                else if (c == ',' && angles == 0 && parens == 0)
                {
                    var part = list.Substring(start, i - start).Trim();
                    var generic = part.IndexOf('<');
                    if (generic >= 0) part = part.Substring(0, generic);
                    var paren = part.IndexOf('(');
                    if (paren >= 0) part = part.Substring(0, paren);
                    var dot = part.LastIndexOf('.');
                    if (dot >= 0) part = part.Substring(dot + 1);
                    if (part.Length > 0) names.Add(part.Trim());
                    start = i + 1;
                }
            }
            return names;
        }

        // The index of the first '{' or ';' that is not in parentheses, or the end of the text.
        static int HeaderEnd(string code, int from)
        {
            var parens = 0;
            for (var i = from; i < code.Length; i++)
            {
                var c = code[i];
                if (c == '(' || c == '[') parens++;
                else if (c == ')' || c == ']') parens--;
                else if ((c == '{' || c == ';') && parens <= 0) return i;
            }
            return code.Length;
        }

        static int Matching(string code, int open)
        {
            var depth = 0;
            for (var i = open; i < code.Length; i++)
            {
                if (code[i] == '{') depth++;
                else if (code[i] == '}' && --depth == 0) return i + 1;
            }
            return code.Length;
        }

        // Replaces comments, strings and characters with spaces, so the scanner sees only code.
        static string StripCode(string text)
        {
            var sb = new StringBuilder(text.Length);
            var i = 0;
            while (i < text.Length)
            {
                var c = text[i];
                var next = i + 1 < text.Length ? text[i + 1] : '\0';
                if (c == '/' && next == '/')
                {
                    while (i < text.Length && text[i] != '\n') i++;
                    continue;
                }
                if (c == '/' && next == '*')
                {
                    var end = text.IndexOf("*/", i + 2, StringComparison.Ordinal);
                    i = end < 0 ? text.Length : end + 2;
                    sb.Append(' ');
                    continue;
                }
                if (c == '"' && next == '"' && i + 2 < text.Length && text[i + 2] == '"')
                {
                    var end = text.IndexOf("\"\"\"", i + 3, StringComparison.Ordinal);
                    i = end < 0 ? text.Length : end + 3;
                    sb.Append("\"\"");
                    continue;
                }
                if (c == '@' && next == '"' || (c == '$' || c == '@') && (next == '@' || next == '$') && i + 2 < text.Length && text[i + 2] == '"')
                {
                    i = text.IndexOf('"', i) + 1;
                    while (i < text.Length && !(text[i] == '"' && (i + 1 >= text.Length || text[i + 1] != '"')))
                    {
                        i += text[i] == '"' ? 2 : 1;
                    }
                    i++;
                    sb.Append("\"\"");
                    continue;
                }
                if (c == '"' || c == '\'')
                {
                    i++;
                    while (i < text.Length && text[i] != c && text[i] != '\n')
                    {
                        i += text[i] == '\\' ? 2 : 1;
                    }
                    i++;
                    sb.Append(c).Append(c);
                    continue;
                }
                sb.Append(c);
                i++;
            }
            return sb.ToString();
        }
    }
}

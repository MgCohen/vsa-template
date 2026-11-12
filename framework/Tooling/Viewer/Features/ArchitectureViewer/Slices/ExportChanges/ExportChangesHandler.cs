using ArchitectureViewer.Domain;
using ArchitectureViewer.Domain.Shared;
using ArchitectureViewer.Infrastructure;
using VSA.Contracts;
using VSA.Handlers.Requests;

namespace ArchitectureViewer.Slices.ExportChanges;

internal sealed class ExportChangesHandler : RequestHandler<ExportChangesRequest, ExportChangesResponse>
{
    public override Task<Result<ExportChangesResponse>> HandleAsync(Envelope<ExportChangesRequest> envelope)
    {
        ExportChangesRequest request = envelope.Message;
        Dictionary<string, Node> nodes = request.App.Nodes.ToDictionary(n => n.Id);
        List<WhatIf> whatIf = [.. request.Links.Select(l => WhatIfOf(l, nodes))];
        string json = ChangesJson.Write(request.Definition, request.Changes, whatIf);
        string code = string.Join("\n", RuleCode.Lines(request.Definition, request.Changes));
        return Task.FromResult<Result<ExportChangesResponse>>(new ExportChangesResponse(json, code));
    }

    private static WhatIf WhatIfOf(LinkChange link, Dictionary<string, Node> nodes)
    {
        Node from = nodes[link.FromNode];
        Node to = nodes[link.ToNode];
        string op = link.Kind.ToString();
        string fromLabel = Links.Label(from, link.FromRow);
        return new WhatIf(op, fromLabel, from.Layer, Links.Label(to, link.ToRow), to.Layer, WhatIf.AppLink);
    }
}

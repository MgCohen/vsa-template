using ArchitectureViewer.Domain.Shared;
using ArchitectureViewer.Presenters.Gestures;
using ArchitectureViewer.Presenters.Items;
using ArchitectureViewer.Slices.ChangeShape;
using ArchitectureViewer.Slices.LoadArchitecture;
using ArchitectureViewer.Slices.SelectItem;
using VSA.Contracts;
using VSA.Presenters;

namespace ArchitectureViewer.Presenters;

public sealed partial class GraphPresenter : Presenter
{
    private readonly Dictionary<string, Offset> offsets = [];
    private readonly Dictionary<string, IReadOnlyList<double[]>> routes = [];
    private readonly List<LinkChange> changes = [];
    private readonly OpState notice = new();
    private SelectItemResponse selected = SelectItemResponse.Nothing;
    private LayoutData data = Mapping.Empty;
    private LayoutData layout = Mapping.Empty;

    public GraphPresenter(IBus bus)
        : base(bus)
    {
        Input = new Canvas(this);
    }

    public Canvas Input { get; }

    public Frame Window { get; } = new(0, 0, 2044, 1060);

    public IReadOnlyList<string> Shapes { get; private set; } = [];

    public string Shape { get; private set; } = GraphKind.App.Shape;

    public IReadOnlyList<CardItem> Cards { get; private set; } = [];

    public IReadOnlyList<GroupItem> Groups { get; private set; } = [];

    public IReadOnlyList<LinkItem> Links { get; private set; } = [];

    public IReadOnlyList<LinkItem> Hot => [.. Links.Where(l => selected.Hot.Contains(l.Id))];

    public int LinkChangeCount => changes.Count;

    public Viewport View { get; private set; } = new(10, 10, 1);

    public bool Editing { get; private set; }

    public Target Selection { get; private set; } = Target.None;

    public async Task Load()
    {
        Result<LoadArchitectureResponse> loaded = await Bus.Send(new LoadArchitectureRequest(Shape));
        if (!loaded.IsSuccess)
        {
            return;
        }
        Shapes = loaded.Value.Shapes;
        data = loaded.Value.Data;
        View = Viewport.Fit(loaded.Value.Layout.Size.W, loaded.Value.Layout.Size.H, Window);
        changes.Clear();
        Selection = Target.None;
        await Show(loaded.Value.Layout);
        if (!loaded.Value.Fits)
        {
            await UseShape(Shape);
        }
    }

    public async Task UseShape(string shape)
    {
        Shape = shape;
        Result<ChangeShapeResponse> changed = await Bus.Send(new ChangeShapeRequest(data, shape));
        if (changed.IsSuccess)
        {
            await Show(changed.Value.Layout);
        }
    }

    public Task ToggleEditing()
    {
        Editing = !Editing;
        return Notify();
    }

    public void Pan(double deltaX, double deltaY)
    {
        View = View.Pan(deltaX, deltaY);
    }

    public void ShowView(Viewport view)
    {
        View = view;
    }

    public Task ResetView()
    {
        View = Viewport.Fit(layout.Size.W, layout.Size.H, Window);
        return Notify();
    }

    public async Task Select(Target target)
    {
        Result<SelectItemResponse> result = await Bus.Send(new SelectItemRequest(layout, Mapping.FocusOf(target)));
        selected = result.IsSuccess ? result.Value : SelectItemResponse.Nothing;
        Selection = selected == SelectItemResponse.Nothing ? Target.None : target;
        await Notify();
    }

    public bool IsDimmed(string nodeId)
    {
        return !Selection.IsNone && !selected.Related.Contains(nodeId);
    }

    public double OpacityOf(LinkItem link)
    {
        return Selection.IsNone || selected.Hot.Contains(link.Id) ? 1 : 0.1;
    }

    public double[] ToCanvas(double clientX, double clientY)
    {
        return [(clientX - Window.Left - View.X) / View.Scale, (clientY - Window.Top - View.Y) / View.Scale];
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Input.Dispose();
        }
        base.Dispose(disposing);
    }

    private async Task Show(LayoutData shown)
    {
        layout = shown;
        data = data with { Groups = shown.Groups, Nodes = shown.Nodes, Size = shown.Size };
        Cards = [.. shown.Nodes.Select(Mapping.CardOf)];
        Groups = [.. shown.Groups.Select(Mapping.GroupOf)];
        Links = [.. shown.Edges.Select(Mapping.LinkOf)];
        offsets.Clear();
        routes.Clear();
        await Select(Selection);
    }

    private Task Notify()
    {
        return Run(notice, () => Task.FromResult<Error?>(null));
    }
}

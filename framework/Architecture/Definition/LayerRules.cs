namespace VSA.Architecture;

public sealed class LayerRules {
    readonly From from;

    internal LayerRules(Type layer, List<IRuleEntries> rules) {
        from = new From(layer, null, $"layer {Layer.Display(layer)}", rules);
    }

    public Uses Uses<T>(Depth? depth = null) where T : Layer => from.Uses<T>(depth);

    public Uses Uses<T>(Access? access, Depth? depth = null) where T : Layer => from.Uses<T>(access, depth);

    public Denies Denies<T>(Depth? depth = null) where T : Layer => from.Denies<T>(depth);
}

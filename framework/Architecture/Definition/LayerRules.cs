namespace VSA.Architecture;

public sealed class LayerRules {
    readonly From from;

    internal LayerRules(Type layer, List<IRuleEntries> rules) {
        from = new From(layer, null, $"layer {Layer.Display(layer)}", rules);
    }

    public Uses Uses<T>(Reach? reach = null) where T : Layer => from.Uses<T>(reach);

    public Uses Uses<T>(Access? access, Reach? reach = null) where T : Layer => from.Uses<T>(access, reach);

    public Denies Denies<T>(Reach? reach = null) where T : Layer => from.Denies<T>(reach);
}

namespace Facts.Infrastructure;

public interface IFactSource<in TQuery, out TFacts> {
    TFacts Read(TQuery query);
}

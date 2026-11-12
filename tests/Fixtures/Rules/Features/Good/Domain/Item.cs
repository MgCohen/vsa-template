using VSA.Domain;

namespace Good.Domain;

public sealed class Item : AggregateRoot<Guid> {
    public Item(Guid id) {
        Id = id;
    }
}

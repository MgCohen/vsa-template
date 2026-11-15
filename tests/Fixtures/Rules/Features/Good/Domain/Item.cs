using Umpire.Vsa.Domain;

namespace Good.Domain;

public sealed class Item : Entity<Guid> {
    public Item(Guid id) {
        Id = id;
    }
}

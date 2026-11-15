namespace Good.Domain;

using Umpire.Vsa.Domain;

public sealed class Item : Entity<Guid> {
    public Item(Guid id) {
        Id = id;
    }
}

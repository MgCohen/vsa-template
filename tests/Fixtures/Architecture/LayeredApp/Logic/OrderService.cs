using LayeredApp.Data;

namespace LayeredApp.Logic;

public sealed class OrderService(OrderStore store) {
    public string Describe(int id) => $"Order {id}: {store.Load(id)}";
}

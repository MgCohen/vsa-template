namespace LayeredApp.Logic;

using LayeredApp.Data;

public sealed class OrderService(OrderStore store) {
    public string Describe(int id) => $"Order {id}: {store.Load(id)}";
}

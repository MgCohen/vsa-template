namespace LayeredApp.Ui;

using LayeredApp.Logic;

public sealed class OrderScreen(OrderService service) {
    public string Show(int id) => service.Describe(id);
}

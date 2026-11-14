using LayeredApp.Logic;

namespace LayeredApp.Ui;

public sealed class OrderScreen(OrderService service) {
    public string Show(int id) => service.Describe(id);
}

using LayeredApp.Ui;

namespace LayeredApp.Data;

public sealed class OrderStore {
    public OrderScreen? LastScreen { get; set; }

    public string Load(int id) => id.ToString(System.Globalization.CultureInfo.InvariantCulture);
}

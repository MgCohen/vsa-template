namespace LayeredApp.Data;

using LayeredApp.Ui;

public sealed class OrderStore {
    public OrderScreen? LastScreen { get; set; }

    public string Load(int id) => id.ToString(System.Globalization.CultureInfo.InvariantCulture);
}

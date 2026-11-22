namespace GoodApp.Billing.Commands;

public static class Billing {
    public enum Mode {
        Total,
    }

    [Kit.Command]
    public static string Total() => Mode.Total.ToString();
}

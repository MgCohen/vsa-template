namespace BadApp.Billing.Presenters;

public static class Formatting {
    public static string Money(decimal amount) {
        return amount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
    }
}

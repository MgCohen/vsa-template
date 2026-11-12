namespace BadApp.Billing.Utils;

public static class Clock {
    public static DateTime Now() {
        return DateTime.UtcNow;
    }
}

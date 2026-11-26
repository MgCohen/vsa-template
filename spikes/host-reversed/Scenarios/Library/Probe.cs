namespace Greeter.Library;

static class Probe {
    public static void Show(string label, Func<string> action) {
        try {
            Console.WriteLine($"{label}: {action()}");
        } catch (Exception e) {
            Console.WriteLine($"{label}: {e.GetType().Name}: {e.Message}");
        }
    }

    public static async Task ShowAsync(string label, Func<Task<string>> action) {
        try {
            Console.WriteLine($"{label}: {await action()}");
        } catch (Exception e) {
            Console.WriteLine($"{label}: {e.GetType().Name}: {e.Message}");
        }
    }

    public static void Section(string title) => Console.WriteLine($"{Environment.NewLine}== {title}");
}

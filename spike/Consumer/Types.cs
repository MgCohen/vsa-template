namespace Consumer;

public sealed class Alpha {
    public int One { get; set; }
}

public sealed class Bravo {
    public void Run() {
    }

    public void Stop() {
    }
}

public interface ICharlie;

public record Delta(string Name);

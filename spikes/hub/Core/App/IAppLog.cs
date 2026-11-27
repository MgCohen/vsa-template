namespace Spike.Core;

public interface IAppLog {
    void Warning(string text);

    void Error(string text);
}

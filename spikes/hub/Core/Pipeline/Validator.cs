namespace Spike.Core;

public abstract class Validator<TMessage> : IValidator where TMessage : class {
    public Type Message => typeof(TMessage);

    public string? Check(object message) => Check((TMessage)message);

    protected abstract string? Check(TMessage message);
}

namespace Fw;

public interface IRequest;
public abstract class Handler<TRequest> where TRequest : IRequest
{
    public abstract void Handle(TRequest request);
}
public abstract class ModuleBase;
public abstract class PluginBase;

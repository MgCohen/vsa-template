namespace Kit;

public interface IModuleRoot;

public interface IRequest;

public interface IEvent;

public interface IRequestHandler;

public interface IEventHandler;

public interface IPresenter;

public interface IEntity;

public interface IPresenterHandler<in TEvent> : IPresenter {
    System.Threading.Tasks.Task HandleAsync(TEvent e);
}

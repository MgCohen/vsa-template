using VSA.Presenters;

namespace Shelf.Presenters;

public sealed class ShelfPresenter(IBus bus) : Presenter(bus);

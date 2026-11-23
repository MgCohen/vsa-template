namespace Shelf.Presenters;

using Umpire.Vsa.Contracts.Requests;
using Umpire.Vsa.Presenters;

public sealed class ShelfPresenter(IRequestSender requests) : Presenter(requests);

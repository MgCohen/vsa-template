namespace Cached.Presenters;

using Umpire.Markers;
using Umpire.Vsa.Contracts.Requests;
using Umpire.Vsa.Presenters;

[AutoRegister(Lifetime.Singleton)]
public sealed class CachedPresenter(IRequestSender requests) : Presenter(requests);

namespace VsaApp.Features.Shop.Presenters;

using Umpire.Messages.Handlers;
using Umpire.Vsa.Contracts.Requests;
using Umpire.Vsa.Presenters;

sealed class PipelinePresenter(IRequestSender requests, Pipelines pipelines) : Presenter(requests) {
    public bool IsSame(Pipelines other) => ReferenceEquals(other, pipelines);
}

using VSA.Architecture;
using VSA.Contracts.Commands;
using VSA.Contracts.Events;
using VSA.Contracts.Requests;

namespace VSA.Architecture.Vsa;

public record Contracts : Layer {
    protected override void Configure(ConceptOptions options) {
        options.Depth = Depth.Self;
    }

    public record Requests : Layer {
        protected override void Define(Part part) {
            part.OfType(typeof(IRoutedRequest)).OfType<IResponse>();
        }
    }

    public record Events : Layer {
        protected override void Define(Part part) {
            part.OfType<IntegrationEvent>();
        }
    }

    public record Commands : Layer {
        protected override void Define(Part part) {
            part.OfType(typeof(ICommand<>));
        }
    }
}

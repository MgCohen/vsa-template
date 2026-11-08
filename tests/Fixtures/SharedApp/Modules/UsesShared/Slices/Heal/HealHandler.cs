using Pipeline.Handlers.Commands.Steps;

namespace UsesShared.Slices.Heal;

// The router finds the handler kind through the Shared base: StepHandler -> CommandHandler.
sealed class HealHandler : StepHandler<Heal, HealResult> {
    protected override HealResult Run(Heal step) => new(step.Amount);
}

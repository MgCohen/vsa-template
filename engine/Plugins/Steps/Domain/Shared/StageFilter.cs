namespace Steps.Domain.Shared;

public static class StageFilter {
    public static bool Runs<TStep>(this Stage stage) where TStep : IStepStage => (stage & TStep.Stages) != 0;
}

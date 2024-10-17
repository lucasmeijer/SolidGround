using SolidGround;

public record RunExperimentForm(KeyValuePair<string, string>[] Values) : TurboFrame(TurboFrameId)
{
    public new static string TurboFrameId => "run_experiment_form";
}
namespace Xtb.XApi.Commands;

public sealed class StepRulesCommand : BaseCommand
{
    public StepRulesCommand(bool prettyPrint = false)
        : base(prettyPrint)
    {
    }

    public override string CommandName => "getStepRules";

    public override string[] RequiredArguments => [];
}
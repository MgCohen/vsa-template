using VSA.Architecture;
using VSA.Architecture.Vsa;
using VSA.Composition;

public class ViewerApp : VsaApp<ViewerApp>
{
    protected override void Rules(RuleBook rules)
    {
        rules.For<Infrastructure>().Uses<Core>(Depth.Self);
        rules.For<VSA.Architecture.Vsa.Host>().Uses<Core>(Depth.Self);
    }
}

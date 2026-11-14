using VSA.Architecture;
using VSA.Architecture.Vsa;

namespace App.Host;

public class ViewerDefinition : VsaDefinition {
    protected override void Rules(RuleBook rules) {
        base.Rules(rules);
        rules.For<Infrastructure>().Uses<Core>(Depth.Self);
        rules.For<VSA.Architecture.Vsa.Host>().Uses<Core>(Depth.Self);
    }
}

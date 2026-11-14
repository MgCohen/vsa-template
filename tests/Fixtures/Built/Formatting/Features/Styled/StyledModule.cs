using VSA.Module;
using VSA.Kernel;

namespace Styled;

// A built fixture: FormattingBuildTests builds it from the packed VSA.Sdk. Each file in Infrastructure/ breaks one
// formatting rule. The build must pass with a warning for each rule, and with no warning for Infrastructure/Migrations/.
public sealed class StyledModule : Module {
    public override void Configure(AnchorBuilder module) {
    }
}

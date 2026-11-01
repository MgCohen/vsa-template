using VSA.Module;

namespace TwoModuleClasses;

public sealed class FirstModule : Module {
    public override void Configure(ModuleBuilder module) { }
}

public sealed class SecondModule : Module {
    public override void Configure(ModuleBuilder module) { }
}

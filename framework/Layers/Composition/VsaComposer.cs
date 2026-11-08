using VSA.Handlers.Commands;
using VSA.Handlers.Events;
using VSA.Module;
#if NET
using VSA.Presenters;
#endif

namespace VSA.Composition;

public static class VsaComposer {
    public static void Install(VsaContext context) {
        EventHandlerInstaller.Install(context);
        CommandHandlerInstaller.Install(context);
#if NET
        PresenterInstaller.Install(context);
#endif
        ModuleInstaller.Install(context);
    }
}

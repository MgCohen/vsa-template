namespace BadApp.App.Host;

public class Startup
{
    public object[] Roots => [new Modules.Billing.BillingModule(), new Modules.Notes.NotesModule(), new Plugins.Audit.AuditPlugin()];
}

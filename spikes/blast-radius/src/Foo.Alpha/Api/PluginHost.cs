namespace Foo.Alpha.Api;

public static class PluginHost
{
    public static int RunByName(string typeName, string method, int x) =>
        (int)Type.GetType(typeName, true)!.GetMethod(method)!.Invoke(Activator.CreateInstance(Type.GetType(typeName, true)!), new object[] { x })!;
}

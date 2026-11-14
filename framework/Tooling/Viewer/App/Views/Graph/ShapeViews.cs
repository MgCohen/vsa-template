using System.Reflection;

namespace App.Views.Graph;

public static class ShapeViews
{
    private static readonly Dictionary<string, Type> views = typeof(ShapeViews).Assembly.GetTypes()
        .Where(t => t.IsSubclassOf(typeof(ShapeView)) && t.GetCustomAttribute<NodeShapeAttribute>() is not null)
        .ToDictionary(t => t.GetCustomAttribute<NodeShapeAttribute>()!.Name);

    public static Type For(string shape)
    {
        return views[shape];
    }
}

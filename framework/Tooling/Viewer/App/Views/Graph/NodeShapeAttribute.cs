namespace App.Views.Graph;

[AttributeUsage(AttributeTargets.Class)]
public sealed class NodeShapeAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}

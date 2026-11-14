using Mvvm.Engine.Model;

namespace Mvvm.App.Model;

public sealed class Greeting : ModelBase {
    public string Name { get; set; } = "world";
}

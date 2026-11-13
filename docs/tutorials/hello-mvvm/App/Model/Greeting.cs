using Hello.Engine.Model;

namespace Hello.App.Model;

public sealed class Greeting : ModelBase {
    public string Name { get; set; } = "world";
}

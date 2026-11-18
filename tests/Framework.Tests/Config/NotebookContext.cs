namespace Framework.Tests.Config;

using Tomlyn.Serialization;

[TomlSerializable(typeof(NotebookSettings))]
sealed partial class NotebookContext : TomlSerializerContext;

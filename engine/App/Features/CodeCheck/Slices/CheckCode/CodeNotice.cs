namespace CodeCheck.Slices.CheckCode;

using System.Globalization;
using Operations.Domain.Shared;

sealed class CodeNotice(string id, string title, string message) {
    public string Id { get; } = id;

    public string Title { get; } = title;

    public string Message { get; } = message;

    public string Text(params object[] arguments) => string.Format(CultureInfo.InvariantCulture, Message, arguments);

    public Finding Finding(params object[] arguments) => new(Id, FindingSeverity.Warning, Text(arguments));
}

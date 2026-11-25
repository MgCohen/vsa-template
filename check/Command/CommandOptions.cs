namespace Umpire.CheckModel.Command;

using Umpire.Config;

sealed record CommandOptions(string Root, string? State) {
    const string RootFlag = "--root";
    const string StateFlag = "--state";

    public static CommandOptions? Of(string[] args) {
        var root = Directory.GetCurrentDirectory();
        string? state = null;
        for (var i = 0; i < args.Length; i += 2) {
            if (i + 1 == args.Length) {
                return null;
            }
            switch (args[i]) {
                case RootFlag:
                    root = Path.GetFullPath(args[i + 1]);
                    break;
                case StateFlag:
                    state = Path.GetFullPath(args[i + 1]);
                    break;
                default:
                    return null;
            }
        }
        return new CommandOptions(root, state);
    }

    public StateFolder Folder() {
        if (State is not null) {
            return StateFolder.At(State);
        }
        var config = UmpireConfig.Find(Root);
        return config.Value(StateFolder.Key) is { Length: > 0 } configured
            ? StateFolder.Of(Path.GetDirectoryName(config.FilePath!)!, configured)
            : StateFolder.Of(Root);
    }
}

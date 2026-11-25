namespace Umpire.CheckModel.Command;

using Umpire.Config;

sealed record CommandOptions(string Root, string? State, CheckMode? Mode) {
    const string RootFlag = "--root";
    const string StateFlag = "--state";
    const string ModeFlag = "--mode";

    public static CommandOptions? Of(string[] args) {
        try {
            return Read(args);
        } catch (Exception e) when (e is ArgumentException or NotSupportedException or PathTooLongException) {
            return null;
        }
    }

    public CheckMode ReportMode() {
        try {
            return Mode ?? Folder().ReadBlock().Mode;
        } catch (Exception e) when (e is not OperationCanceledException) {
            return ConfiguredMode();
        }
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

    static CommandOptions? Read(string[] args) {
        var root = Directory.GetCurrentDirectory();
        string? state = null;
        CheckMode? mode = null;
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
                case ModeFlag when CheckModes.Parse(args[i + 1]) is { } given:
                    mode = given;
                    break;
                default:
                    return null;
            }
        }
        return new CommandOptions(root, state, mode);
    }

    CheckMode ConfiguredMode() {
        try {
            return CheckModes.Of(UmpireConfig.Find(Root).Value(CheckModes.Key));
        } catch (Exception e) when (e is not OperationCanceledException) {
            return CheckModes.Default;
        }
    }
}

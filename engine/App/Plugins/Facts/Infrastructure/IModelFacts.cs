namespace Facts.Infrastructure;

using Umpire.CheckModel;

public interface IModelFacts {
    string? Folder { get; }

    CurrentProject Current { get; }

    Snapshot Full();
}

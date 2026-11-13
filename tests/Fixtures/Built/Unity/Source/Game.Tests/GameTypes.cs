namespace Game.Tests;

public static class GameTypes {
    public static IReadOnlyList<Type> All { get; } = [typeof(Board.BoardModule), typeof(Board.Domain.Cell), typeof(Score.ScoreModule)];
}

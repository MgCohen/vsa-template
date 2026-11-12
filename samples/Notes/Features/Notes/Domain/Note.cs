using VSA.Domain;

namespace Notes.Domain;

public sealed class Note : AggregateRoot<NoteId> {
    public const int MaxTitleLength = 200;

    Note() { }

    public AuthorId Author { get; private set; }
    public string Title { get; private set; } = "";

    public static bool IsValidTitle(string title) =>
        !string.IsNullOrWhiteSpace(title) && title.Trim().Length <= MaxTitleLength;

    public static Note Create(AuthorId author, string title) =>
        IsValidTitle(title)
            ? new Note { Id = new(Guid.NewGuid()), Author = author, Title = title.Trim() }
            : throw new ArgumentException($"A note title has 1 to {MaxTitleLength} characters.", nameof(title));
}

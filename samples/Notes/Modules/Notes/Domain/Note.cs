using VSA.Domain;

namespace Notes.Domain;

public sealed class Note : AggregateRoot<NoteId> {
    public const int MaxTitleLength = 200;

    Note() { }

    public AuthorId Author { get; private set; }
    public string Title { get; private set; } = "";

    public static Result<Note> Create(AuthorId author, string title) =>
        string.IsNullOrWhiteSpace(title) ? NoteErrors.TitleRequired
        : title.Trim().Length > MaxTitleLength ? NoteErrors.TitleTooLong
        : new Note { Id = new(Guid.NewGuid()), Author = author, Title = title.Trim() };
}

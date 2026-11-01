using VSA.Domain;

namespace Notes.Domain;

public static class NoteErrors {
    public static readonly Error TitleRequired = new("notes.title_required", "A note must have a title.");
    public static readonly Error TitleTooLong = new("notes.title_too_long", $"A title must have {Note.MaxTitleLength} characters or fewer.");
    public static readonly Error NotFound = new("notes.not_found", "The note does not exist.");
}

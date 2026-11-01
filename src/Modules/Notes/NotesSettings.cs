using System.ComponentModel.DataAnnotations;

namespace Notes;

// Bound from the Modules:Notes section. Data only: NotesModule holds the rules.
public sealed class NotesSettings {
    [Required]
    public string ConnectionString { get; init; } = "";
}

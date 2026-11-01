using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notes.Domain;

namespace Notes.Infrastructure.Mappings;

// Optional: database mapping that the convention cannot do (EF Core standard, found automatically).
sealed class NoteMapping : IEntityTypeConfiguration<Note> {
    public void Configure(EntityTypeBuilder<Note> note) {
        note.Property(n => n.Title).HasMaxLength(Note.MaxTitleLength);
        note.HasIndex(n => n.Author);
    }
}

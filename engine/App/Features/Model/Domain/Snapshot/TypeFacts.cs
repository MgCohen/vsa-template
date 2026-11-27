namespace Umpire.CheckModel;

public sealed record TypeFacts(TypeName Name, TypeLocation Location, TypeContent Content, IReadOnlyList<ReferenceFact> References);

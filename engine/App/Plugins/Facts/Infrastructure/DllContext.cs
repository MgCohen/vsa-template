namespace Facts.Infrastructure;

sealed record DllContext(SymbolReader Symbols, MetadataNames Names, BodyUses Bodies);

namespace Facts.Infrastructure;

sealed record SourcePoint(int Offset, string File, int Line, int Column);

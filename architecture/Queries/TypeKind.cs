namespace Umpire.Architecture;

[Flags]
public enum TypeKind {
    None = 0,
    Class = 1,
    StaticClass = 2,
    AbstractClass = 4,
    Record = 8,
    Struct = 16,
    Interface = 32,
    Enum = 64,
    Delegate = 128,
}

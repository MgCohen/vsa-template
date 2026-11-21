namespace Umpire.Architecture.Traits;

public sealed record Static : Trait {
    internal override string Text => "static";

    internal override bool Fits(TypeFacts type) => type.IsStatic;
}

public sealed record Abstract : Trait {
    internal override string Text => "abstract";

    internal override bool Fits(TypeFacts type) => type.IsAbstract && !type.IsInterface;
}

public sealed record Sealed : Trait {
    internal override string Text => "sealed";

    internal override bool Fits(TypeFacts type) => type.IsSealed;
}

public sealed record Record : Trait {
    internal override string Text => "record";

    internal override bool Fits(TypeFacts type) => type.IsRecord;
}

public sealed record Struct : Trait {
    internal override string Text => "struct";

    internal override bool Fits(TypeFacts type) => type.IsStruct;
}

public sealed record Interface : Trait {
    internal override string Text => "interface";

    internal override bool Fits(TypeFacts type) => type.IsInterface;
}

public sealed record Enum : Trait {
    internal override string Text => "enum";

    internal override bool Fits(TypeFacts type) => type.IsEnum;
}

public sealed record Delegate : Trait {
    internal override string Text => "delegate";

    internal override bool Fits(TypeFacts type) => type.IsDelegate;
}

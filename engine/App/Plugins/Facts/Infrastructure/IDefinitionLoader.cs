namespace Facts.Infrastructure;

using Facts.Domain.Shared;
using Umpire.Architecture;

public interface IDefinitionLoader {
    bool TakesPacked(TypeName name);

    Architecture CreateArchitecture(TypeName name, TypeName? packed = null);
}

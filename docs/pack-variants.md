# Pack variants

A pack variant is a pack that inherits the architecture of another pack and adds parts and rules to it. A VSA variant inherits `VsaArchitecture`, adds only, and keeps each decision of the base. The fixture `tests/Fixtures/PackVariant/` is the full example: the pack `Ecs` adds the module kind `EcsFeature`.

## When to write a variant

| Need | Form |
|---|---|
| Some rules about the code of one app | An architecture class in the app, for example `engine/Definition/EngineArchitecture.cs` |
| A new module kind, its bases and its rules | A pack variant in `App/Packs/<Name>/` |
| A part in each module of a base kind | A pack variant that overrides a define method |
| A different architecture, not VSA | A new definition that inherits `Architecture` (`tests/Fixtures/Architecture/LayeredApp`) |

## The pack and its two checks

```
PackVariant/                             app block: check entry PackVariantApp, definition EcsArchitecture
└─ App/                                  the App root
   ├─ PackVariantApp.cs                  public class PackVariantApp : App<Ecs<AppHost>>;
   ├─ Features/Inventory/                a feature module
   ├─ EcsFeatures/Combat/                an EcsFeature module
   │  └─ CombatModule.cs, Components/, Systems/
   └─ Packs/Ecs/                         pack block: check entry Ecs<THost>, definition PackArchitecture
      ├─ EcsPack.cs                      the pack class
      ├─ Definition/                     EcsArchitecture and its concept records
      ├─ Kernel/                         EcsSystem: the base of each system
      └─ Tests/                          Ecs.Tests: the tests of EcsArchitecture
```

A pack variant is not a package, so the app keeps the default pack `Umpire.Vsa`. The pack class derives from `Vsa<...>`, so the app class selects VSA, and the VSA analyzers run. The pack class is the check entry of the pack block (`Pack : Block<PackArchitecture>`):

```csharp
public class Ecs<THost> : Vsa<EcsArchitecture, THost> where THost : AppHost, new();
```

| Check | Block folder | Definition | Checks |
|---|---|---|---|
| App | `PackVariant/` | `EcsArchitecture`, from the app class | `App/`, `App/Features/`, `App/EcsFeatures/`. `App/Packs/Ecs/` is one part |
| Pack | `PackVariant/App/Packs/Ecs/` | `PackArchitecture`, from `Ecs<THost>` | The pack class, `Definition/`, `Kernel/`, `Tests/` |

`VsaArchitecture` marks `App/Packs/` with `AddBlockGroup<Pack>("Packs")`. So the app check binds each type of `App/Packs/<Name>/` to the part `Pack`, and the pack check checks its uses. The app class, the app host (`App/Host/`) and each feature and plugin module may use `Pack`. The app views (`Views/`) may not use `Pack`: a view uses only presenters. A pack place uses nothing in the app. The root project compiles `App/Packs/`, so the pack types are in the app DLL.

The build of the app checks only the app block ([MgCohen/vsa-template#150](https://github.com/MgCohen/vsa-template/issues/150)). Run the pack check by hand:

```bash
dotnet umpire-engine.dll --project App/Packs/Ecs --bin <host output>
```

## The variant architecture

A variant overrides `Define` and `Rules`, and writes only its additions. It does not call the base: the engine calls each level of the chain, from the base down (`UMP010107`).

```csharp
public sealed class EcsArchitecture : VsaArchitecture {
    protected override void Define(Part root, Part kernel) {
        var ecs = root.AddGroup<EcsFeature>();
        ecs.Add<Components>();
        ecs.Add<Systems>().OfType<EcsSystem>();
    }

    protected override void Rules(RuleBook rules) {
        rules.For<Systems>().Uses<Components>().And<Pack>();
        rules.For<EcsFeature>(Depth.Self).Uses<EcsFeature>(Access.Internal);
        rules.For<EcsFeature>().Has<Systems>().AtLeast(1);
    }
}
```

`EcsFeature` derives from the record `Module` (`Umpire.Kernels.Blocks`). The rule of `Module` also covers it: exactly one `Umpire.Kernels.Module` class at the top of each `EcsFeature` folder (`HasTypes`).

The concept records are in `Definition/`, next to the architecture:

```csharp
public record EcsFeature : Module {
    public override string GroupFolder => "EcsFeatures";
}
public record Components : Layer;
public record Systems : Layer;
```

The host finds `CombatModule`, because it inherits `Module`. A new module kind needs no host change.

### What a new module kind gets from the base

| Base rule | Applies to `EcsFeature` |
|---|---|
| A rule that names a concept, for example `For<Domain>()` | Yes, if the kind has that part |
| A rule with `Under<Feature>()` or `InOther<Feature>()` | No. Write the rule again with `EcsFeature` |
| A rule with `Feature` as its subject, for example `For<Feature>().And<Plugin>().Uses<Pack>()` | No. `EcsArchitecture` writes `Systems` uses `Pack` again |
| The closed boundary of a group | Yes: one `EcsFeature` does not use another `EcsFeature` |
| A rule on `Module`, the base of `EcsFeature` | Yes: a rule on a concept covers each derived concept |
| `For<Module>(Depth.Self).Uses<Kernel>()` | Yes, for the module class of the kind |

## Overlays and derived records

The body of each base group is in its record: `Feature.Define`, `Plugin.Define` and `Slice.Define`. The kernel body is in `VsaArchitecture.Define`, on the `kernel` part. A variant adds a part to each placed block of a kind with an overlay:

```csharp
protected override void Define(Part root, Part kernel) {
    root.For<Feature>().Add<Systems>();
}
```

To stack the body of a block record on each found part, use `root.For<Feature>().ExtendedBy<TBlock>()` (see "Extend a base architecture" in `architecture-engine.md`).

A new kind can derive from a base record to get its body: `record EcsFeature : Feature`. Then the new group has the feature parts and adds its own in its `Define`. The rules that name `Feature` also cover it.

A variant does not remove or move a base part. `PackVariantTests` checks that the variant keeps each decision of `VsaArchitecture`.

## Tests of the variant

A pack tests its variant in `App/Packs/<Name>/Tests/`, with `VsaArchitectureTest<TArchitecture>`. The test project is nested, so the root project of the app does not compile it.

```csharp
public sealed class EcsArchitectureTests : VsaArchitectureTest<EcsArchitecture> {
    [Test]
    public void A_system_uses_the_components_of_its_own_module() {
        var system = At<DamageSystem>("App/EcsFeatures/Combat/Systems");

        Assert.That(MayUse(system, At("App/EcsFeatures/Combat/Components")).Permission, Is.EqualTo(Permission.Allow));
        Assert.That(MayUse(system, At("App/EcsFeatures/Loot/Components")).Permission, Is.EqualTo(Permission.Deny));
    }
}
```

See "Tests of a definition" in `architecture-engine.md`.

## The `using` steps

The concept records have the names of folders, so a name can resolve to a namespace or to a framework class. Use the next step only if the step before it is not enough (see "`using` placement" in `architecture-engine.md`):

1. Put the `using` directives inside the namespace.
2. If the file is inside the namespace of the concept records, put some `using` directives outside the namespace.
3. Add an alias inside the namespace.

In the fixture, step 1 is enough. `EcsArchitecture` is in `PackVariant.Packs.Ecs.Definition`, and the `using` inside the namespace wins over the sibling namespace `PackVariant.Packs.Ecs.Kernel`:

```csharp
namespace PackVariant.Packs.Ecs.Definition;

using PackVariant.Packs.Ecs.Kernel.Domain;
using Umpire.Architecture;
using Umpire.Kernels.Blocks;
using Umpire.Vsa.Definition;
```

| Clash | Step |
|---|---|
| `Kernel` is the record `Umpire.Architecture.Blocks.Kernel` and a folder of the pack | 1 |
| `App` is the record and the namespace `<App>.App` | 1 |
| `Pack` is the record `Umpire.Kernels.Blocks.Pack` and the class `Umpire.Kernels.Pack` | 3: `using Pack = Umpire.Kernels.Blocks.Pack;` |
| `Module` is the record `Umpire.Kernels.Blocks.Module` and the class `Umpire.Kernels.Module` | 3: `using Module = Umpire.Kernels.Blocks.Module;` |
| `Plugin` is the record and the class `Umpire.Vsa.Composition.Plugin` | 3: `using Plugin = Umpire.Vsa.Definition.Plugin;` |
| A variant in `Umpire.Vsa.Definition.*` | 2 |

Write each real type with its full name, for example `HasTypes<Umpire.Kernels.Module>()`. Then the file does not import its namespace.

## A plugin with its own folders

A plugin module has the layer folders of the framework. Another folder gives `UMP020202`. The definition is in the architecture, not in the module, so the app architecture adds the folder:

| Case | Form |
|---|---|
| Each plugin can have the folder | Add the concept with `For<Plugin>().Add<T>()`, and write rules with `Under<Plugin>()` |
| Only some plugins have the folder | Add a new plugin kind with its own group folder, as `EcsFeature` |

```csharp
protected override void Define(Part root, Part kernel) {
    root.For<Plugin>().Add<Runners>();
}

protected override void Rules(RuleBook rules) {
    rules.For<Runners>().Under<Plugin>().Uses<Handlers.Commands>();
}
```

## `umpire add pack`

`umpire add pack <Name>` writes two files:

| File | Content |
|---|---|
| `App/Packs/<Name>/<Name>Pack.cs` | `public class <Name><THost> : Vsa<<Name>Architecture, THost> where THost : AppHost, new();` |
| `App/Packs/<Name>/Definition/<Name>Architecture.cs` | `public class <Name>Architecture : VsaArchitecture;` |

Then do these steps:

1. Change the app class to `App<<Name><WebHost>>`.
2. Add the concept records to `Definition/`.
3. Add the parts and the rules to `<Name>Architecture`.
4. Put the bases of the new module kind in `Kernel/`.

## Non-goals

- No edit API that removes or moves a base part. Reason: a variant keeps each decision of its base.
- No rule methods for each group (`FeatureRules<TFeature>`). Reason: no use needs them before the composition review.
- No plugin with its own piece of definition. Reason: that is composition of packs, after 0.1.0.

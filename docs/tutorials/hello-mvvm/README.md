# Hello MVVM

A small MVVM app with its own VSA architecture definition. The tutorial page is https://claude.ai/artifact/XawEXWnWp1f6JQSF53F7go, and this folder has the same code.

| Folder | Contains |
|---|---|
| `Engine/` | The base classes `ModelBase`, `ViewModelBase<TModel>`, `ViewBase<TViewModel>`, and the definition in `Architecture/` |
| `App/` | One class for each layer, `HelloApp : VsaApp<HelloApp>` and `Program.cs` |
| `Tests/` | One test. `VSA.Testing.Architecture` adds the architecture tests |

The folder uses the published packages (`0.1.0-preview.28`), not the source of this repository. The repository build files change the build here, so copy the folder out of the repository before you build it.

1. Copy the folder to a folder outside the repository.
2. Set `GITHUB_TOKEN` to a token with `read:packages`.
3. Run `dotnet run --project App`. The output is `Hello world`.
4. Run `dotnet test --solution HelloMvvm.slnx`. 13 tests pass.

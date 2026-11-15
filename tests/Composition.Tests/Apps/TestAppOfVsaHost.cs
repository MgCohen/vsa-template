namespace Composition.Tests;

using Umpire.Vsa.Composition;
using Umpire.Kernel;

public sealed class TestApp(params Module[] modules) : TestApp<VsaHost>(modules);

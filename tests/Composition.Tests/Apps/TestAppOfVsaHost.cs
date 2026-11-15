namespace Composition.Tests;

using VSA.Composition;
using VSA.Kernel;

public sealed class TestApp(params Module[] modules) : TestApp<VsaHost>(modules);

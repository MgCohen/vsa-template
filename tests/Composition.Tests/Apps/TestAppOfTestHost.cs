namespace Composition.Tests;

using Umpire.Kernels;

public sealed class TestApp(params Module[] modules) : TestApp<TestHost>(modules);

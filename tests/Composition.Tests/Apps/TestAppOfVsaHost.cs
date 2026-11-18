namespace Composition.Tests;

using Umpire.Vsa.Composition;
using Umpire.Kernels;

public sealed class TestApp(params Module[] modules) : TestApp<VsaHost>(modules);

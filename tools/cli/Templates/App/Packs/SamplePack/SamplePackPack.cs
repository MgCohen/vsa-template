namespace SampleApp.Packs.SamplePack;

using SampleApp.Packs.SamplePack.Architecture;
using Umpire.Kernels;
using Umpire.Vsa;

public class SamplePack<THost> : Vsa<SamplePackArchitecture, THost> where THost : AppHost, new();

namespace SampleApp.Packs.SamplePack;

using SampleApp.Packs.SamplePack.Architecture;
using Umpire.Vsa;
using Umpire.Vsa.Composition;

public class SamplePack<THost> : Vsa<SamplePackArchitecture, THost> where THost : VsaHost, new();

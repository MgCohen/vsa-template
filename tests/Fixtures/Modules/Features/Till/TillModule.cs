namespace Till;

using Microsoft.Extensions.DependencyInjection;
using Till.Domain;
using Till.Domain.Shared;
using Umpire.Kernels;

public sealed class TillModule : Module {
    protected override void Configure(IServiceCollection services) {
        services.AddSingleton<TillLog>();
        services.AddSingleton<Register>();
        services.AddSingleton(typeof(IStock<>), typeof(Stock<>));
        services.AddTransient(typeof(IEntry<>), typeof(Entry<>));
        services.AddSingleton(typeof(IShelf<>), typeof(Shelf<>));
        services.AddSingleton<IShelf<Coin>, Shelf<Coin>>();
        services.AddSingleton<ICounter<Score>, Counter<Score>>();
    }
}

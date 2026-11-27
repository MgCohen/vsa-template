namespace Composition.Tests.Operations.Shop;

using Microsoft.Extensions.DependencyInjection;
using Umpire.Kernels;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Composition;
using Umpire.Vsa.Contracts.Commands;
using Umpire.Vsa.Contracts.Requests;
using Umpire.Vsa.Handlers.Requests;

public sealed class RequestConverterTests {
    static readonly RequestConverter Converter = RequestConverter.Of(new AppTypes(new FixedModules(new ShopModule())));

    [Test]
    public void The_full_name_binds_each_value_to_the_request() {
        var item = Guid.NewGuid();

        var result = Converter.Convert(Op("shop/buy", ("item", item.ToString()), ("count", "3"), ("colors", "red, blue"), ("size", "Large")));

        Assert.That(result.Value, Is.EqualTo(new BuyRequest(item, 3, Colors.Red | Colors.Blue, Size.Large)));
    }

    [Test]
    public void A_unique_short_name_and_a_default_value_work() {
        var item = Guid.NewGuid();

        var result = Converter.Convert(Op("buy", ("Item", item.ToString()), ("colors", "red")));

        Assert.That(result.Value, Is.EqualTo(new BuyRequest(item, 1, Colors.Red, null)));
    }

    [Test]
    public void A_null_value_gives_null() {
        var result = Converter.Convert(Op("shop/buy", ("item", Guid.Empty.ToString()), ("colors", "red"), ("size", null)));

        Assert.That(((BuyRequest)result.Value).Size, Is.Null);
    }

    [Test]
    public void A_missing_value_with_no_default_is_not_valid() {
        var result = Converter.Convert(Op("shop/buy", ("colors", "red")));

        Assert.That(result.Error?.Code, Is.EqualTo("request.not_valid"));
        Assert.That(result.Error?.Message, Is.EqualTo("BuyRequest needs the value 'Item'."));
    }

    [TestCase("count", "many")]
    [TestCase("colors", "green")]
    [TestCase("item", "not-a-guid")]
    public void A_value_that_does_not_convert_is_not_valid(string key, string text) {
        var values = new Dictionary<string, string?> { ["item"] = Guid.Empty.ToString(), ["colors"] = "red", [key] = text };

        var result = Converter.Convert(new Operation("shop/buy", values));

        Assert.That(result.Error?.Code, Is.EqualTo("request.not_valid"));
        Assert.That(result.Error?.Message, Does.Contain($"'{key}'").IgnoreCase);
    }

    [Test]
    public void A_null_for_a_value_type_is_not_valid() {
        var result = Converter.Convert(Op("shop/buy", ("item", null), ("colors", "red")));

        Assert.That(result.Error?.Code, Is.EqualTo("request.not_valid"));
        Assert.That(result.Error?.Message, Does.Contain("must not be null"));
    }

    [Test]
    public void A_value_with_no_text_form_is_not_valid() {
        var result = Converter.Convert(Op("shop/tag", ("tags", "[\"a\",\"b\"]")));

        Assert.That(result.Error?.Code, Is.EqualTo("request.not_valid"));
        Assert.That(result.Error?.Message, Does.Contain("has no conversion from text"));
    }

    [Test]
    public void Two_requests_with_one_name_fail_only_when_a_caller_uses_that_name() {
        var result = Converter.Convert(Op("shop/twin"));

        Assert.That(result.Error?.Code, Is.EqualTo(RequestConverter.AmbiguousCode));
        Assert.That(Converter.Convert(Op("shop/buy", ("item", Guid.Empty.ToString()))).IsSuccess, Is.True);
    }

    [Test]
    public void A_handler_of_an_abstract_request_accepts_each_derived_request() {
        Assert.That(Converter.Accepts(typeof(ShapeRequest)), Is.True);
        Assert.That(Converter.Accepts(typeof(CircleRequest)), Is.True);
        Assert.That(Converter.Names, Does.Not.Contain("shop/shape"));
    }

    [Test]
    public void An_unknown_name_is_not_found_and_lists_the_names() {
        var result = Converter.Convert(Op("shop/steal"));

        Assert.That(result.Error?.Code, Is.EqualTo("operation.not_found"));
        Assert.That(result.Error?.Message, Does.Contain("shop/buy"));
    }

    [Test]
    public void The_converter_accepts_only_requests() {
        Assert.That(Converter.Accepts(typeof(BuyRequest)), Is.True);
        Assert.That(Converter.Accepts(typeof(RestockCommand)), Is.False);
        Assert.That(Converter.Names, Does.Contain("shop/buy"));
    }

    [Test]
    public async Task An_operation_with_no_converter_gives_an_error() {
        using var services = new ServiceCollection().BuildServiceProvider();

        var result = await services.SendAsync(Op("shop/buy"));

        Assert.That(result.Error?.Code, Is.EqualTo(OperationSends.NoConverterCode));
    }

    static Operation Op(string name, params (string Key, string? Value)[] values) =>
        new(name, values.ToDictionary(v => v.Key, v => v.Value));

    public sealed class ShopModule : Module;

    [Flags]
    public enum Colors {
        None = 0,
        Red = 1,
        Blue = 2,
    }

    public enum Size {
        Small,
        Large,
    }

    public sealed record BuyRequest(Guid Item, int Count = 1, Colors Colors = Colors.None, Size? Size = null) : IRequest<BuyResponse> {
        public static string Group => "shop";
    }

    public sealed record BuyResponse(bool Done) : IResponse;

    public sealed record TagRequest(IReadOnlyList<string> Tags) : IRequest<BuyResponse>;

    public sealed class TagHandler : RequestHandler<TagRequest, BuyResponse> {
        public override Task<Result<BuyResponse>> HandleAsync(IEnvelope<TagRequest> envelope) =>
            Task.FromResult<Result<BuyResponse>>(new BuyResponse(true));
    }

    public sealed record FirstTwinRequest : IRequest<BuyResponse> {
        public static string Name => "twin";
    }

    public sealed record SecondTwinRequest : IRequest<BuyResponse> {
        public static string Name => "twin";
    }

    public sealed class FirstTwinHandler : RequestHandler<FirstTwinRequest, BuyResponse> {
        public override Task<Result<BuyResponse>> HandleAsync(IEnvelope<FirstTwinRequest> envelope) =>
            Task.FromResult<Result<BuyResponse>>(new BuyResponse(true));
    }

    public sealed class SecondTwinHandler : RequestHandler<SecondTwinRequest, BuyResponse> {
        public override Task<Result<BuyResponse>> HandleAsync(IEnvelope<SecondTwinRequest> envelope) =>
            Task.FromResult<Result<BuyResponse>>(new BuyResponse(true));
    }

    public abstract record ShapeRequest : IRequest<BuyResponse>;

    public sealed record CircleRequest(int Radius) : ShapeRequest;

    public sealed class ShapeHandler : RequestHandler<ShapeRequest, BuyResponse> {
        public override Task<Result<BuyResponse>> HandleAsync(IEnvelope<ShapeRequest> envelope) =>
            Task.FromResult<Result<BuyResponse>>(new BuyResponse(true));
    }

    public sealed record RestockCommand(int Count) : ICommand<int>;

    public sealed class BuyHandler : RequestHandler<BuyRequest, BuyResponse> {
        public override Task<Result<BuyResponse>> HandleAsync(IEnvelope<BuyRequest> envelope) =>
            Task.FromResult<Result<BuyResponse>>(new BuyResponse(true));
    }
}

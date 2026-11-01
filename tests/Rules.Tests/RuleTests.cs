using VSA.Testing;
using VSA.Testing.Rules;
using static Rules.Tests.Fixtures;

namespace Rules.Tests;

public sealed class OneHandlerPerRequestTests {
    readonly OneHandlerPerRequest rule = new();

    [Fact]
    public void Good_fixture_passes() => VsaRules.Assert(rule, App("Good"));

    [Fact]
    public void Request_with_no_handler_fails() {
        var message = Fails(rule, App("NoHandler"));
        Assert.Contains("VSA2001", message);
        Assert.Contains("NoHandler.Contracts.Requests.OrphanRequest has no handler", message);
        Assert.Contains("RoutedHandler<OrphanRequest, TResponse>", message);
    }

    [Fact]
    public void Request_with_two_handlers_fails() {
        var message = Fails(rule, App("TwoHandlers"));
        Assert.Contains("TwoHandlers.Contracts.Requests.TwiceRequest has 2 handlers", message);
        Assert.Contains("TwoHandlers.Handlers.Requests.FirstHandler", message);
        Assert.Contains("TwoHandlers.Handlers.Requests.SecondHandler", message);
    }
}

public sealed class UniqueRoutesTests {
    readonly UniqueRoutes rule = new();

    // Good has GET and DELETE on the same route: a different method is not a conflict.
    [Fact]
    public void Good_fixture_passes() => VsaRules.Assert(rule, App("Good"));

    // The routes differ only in case, parameter name and a trailing "/".
    [Fact]
    public void Same_method_and_route_fails() {
        var message = Fails(rule, App("SameRoute"));
        Assert.Contains("VSA2002", message);
        Assert.Contains("SameRoute.Contracts.Requests.FirstRequest", message);
        Assert.Contains("SameRoute.Contracts.Requests.SecondRequest", message);
        Assert.Contains("GET /same/{id}", message);
    }

    // The rule reads the requests of all modules: two modules with different routes pass.
    [Fact]
    public void Two_modules_with_different_routes_pass() => VsaRules.Assert(rule, App("Good", "TwoHandlers"));
}

public sealed class OneModuleClassTests {
    readonly OneModuleClass rule = new();

    [Fact]
    public void Good_fixture_passes() => VsaRules.Assert(rule, App("Good"));

    [Fact]
    public void Module_root_with_no_Module_class_fails() {
        var message = Fails(rule, App("NoModuleClass"));
        Assert.Contains("VSA2003", message);
        Assert.Contains("Module root NoModuleClass has no Module class", message);
    }

    [Fact]
    public void Module_root_with_two_Module_classes_fails() {
        var message = Fails(rule, App("TwoModuleClasses"));
        Assert.Contains("Module root TwoModuleClasses has 2 Module classes", message);
        Assert.Contains("TwoModuleClasses.FirstModule", message);
        Assert.Contains("TwoModuleClasses.SecondModule", message);
    }

    [Fact]
    public void Module_class_that_is_not_sealed_fails() {
        var message = Fails(rule, App("OpenModuleClass"));
        Assert.Contains("OpenModuleClass.OpenModuleClassModule must be public and sealed", message);
    }
}

public sealed class AllRulesTests {
    [Fact]
    public void Good_fixture_passes_all_rules() {
        foreach (var rule in VsaRules.All) {
            VsaRules.Assert(rule, App("Good"));
        }
    }
}

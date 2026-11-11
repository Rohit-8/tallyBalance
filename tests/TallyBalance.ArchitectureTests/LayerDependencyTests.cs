using NetArchTest.Rules;

namespace TallyBalance.ArchitectureTests;

/// <summary>
/// Executable enforcement of <c>docs/architecture/dependency-rules.md</c>.
///
/// These tests exist because a written rule about dependency direction is a
/// convention, and conventions erode the first time someone is under pressure: a
/// controller reaches for a <c>DbContext</c>, it compiles, the tests pass, and the
/// architecture is quietly wrong. Making that a red build is what keeps the modular
/// monolith modular — and it is the precondition for ever splitting a module out,
/// which only works if modules were never allowed to reach through each other.
///
/// They run in CI with no external dependencies, so they cost seconds and fail fast.
/// </summary>
public class LayerDependencyTests
{
    private const string Domain = "TallyBalance.Domain";
    private const string Application = "TallyBalance.Application";
    private const string Infrastructure = "TallyBalance.Infrastructure";
    private const string Api = "TallyBalance.Api";
    private const string Worker = "TallyBalance.Worker";

    [Fact]
    public void Domain_has_no_dependencies_on_any_other_project()
    {
        // The innermost layer must be pure. If Domain ever references EF Core or ASP.NET,
        // the entities stop being a description of the business and start being a
        // description of the storage mechanism — and the state machines and invariants
        // that are this system's real value become untestable without a database.
        var result = Types.InAssembly(Domain_Assemblies.Domain)
            .ShouldNot()
            .HaveDependencyOnAny(Application, Infrastructure, Api, Worker,
                "Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore")
            .GetResult();

        AssertSuccess(result);
    }

    [Fact]
    public void Application_does_not_depend_on_infrastructure_or_aspnet()
    {
        // The dependency inversion that defines Clean Architecture. Application declares
        // ports (ILLMProvider, ICurrentUser, IClock); Infrastructure implements them. If
        // Application could reference Infrastructure, ports would be unnecessary and the
        // use cases would become untestable without SQL Server running.
        var result = Types.InAssembly(Domain_Assemblies.Application)
            .ShouldNot()
            .HaveDependencyOnAny(Infrastructure, Api, Worker,
                "Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore",
                "StackExchange.Redis", "RabbitMQ.Client")
            .GetResult();

        AssertSuccess(result);
    }

    [Fact]
    public void Infrastructure_does_not_depend_on_api_or_worker()
    {
        // Infrastructure is a library of adapters consumed by hosts. If it referenced a
        // host, the two hosts could no longer both use it, and the worker would end up
        // depending on the web stack.
        var result = Types.InAssembly(Domain_Assemblies.Infrastructure)
            .ShouldNot()
            .HaveDependencyOnAny(Api, Worker)
            .GetResult();

        AssertSuccess(result);
    }

    [Fact]
    public void Only_composition_root_in_api_references_infrastructure()
    {
        // The composition-root exception: Infrastructure may be referenced by exactly one
        // place in a host — the wiring at startup. Everywhere else, code must depend on
        // Application's abstractions. Without this rule, "just this once" injects a
        // concrete adapter into a service and the abstraction stops meaning anything.
        var result = Types.InAssembly(Domain_Assemblies.Api)
            .That()
            .DoNotResideInNamespaceContaining("Health")
            .And()
            .DoNotHaveName("Program")
            .ShouldNot()
            .HaveDependencyOn(Infrastructure)
            .GetResult();

        AssertSuccess(result);
    }

    [Fact]
    public void Application_abstractions_do_not_leak_infrastructure_types()
    {
        // Ports must stay abstract. A type under an Abstractions namespace that reaches
        // into Infrastructure means an implementation detail (HTTP, retries, a connection
        // string, a provider's wire format) has leaked inward — at which point the port
        // can no longer be swapped, and the fake provider that makes AI testable stops
        // working. Note this checks dependencies rather than "is it an interface":
        // sealed records legitimately live here as message contracts (ChatRequest,
        // ChatResult), and forbidding them would only encourage stringly-typed parameters.
        var result = Types.InAssembly(Domain_Assemblies.Application)
            .That()
            .ResideInNamespaceContaining("Abstractions")
            .ShouldNot()
            .HaveDependencyOnAny(Infrastructure, Api, Worker)
            .GetResult();

        AssertSuccess(result);
    }

    [Fact]
    public void Every_non_empty_scanned_assembly_actually_contains_types()
    {
        // This test exists to stop the others from lying.
        //
        // "ShouldNot have a forbidden dependency" passes when there are no types to
        // check. If an assembly failed to load, got renamed, or was emptied, every rule
        // above would report success while enforcing nothing — the worst possible failure
        // mode for a guard rail, because it looks identical to a healthy build. Asserting
        // a minimum type count turns that silent failure into a loud one.
        //
        // It earned its place immediately: on first run it failed, because Domain has no
        // entities yet. Domain is therefore excluded below, and the exclusion is stated
        // rather than glossed over.
        //
        // TODO(Phase 2): add Assert.NotEmpty(Domain_Assemblies.Domain.GetTypes()) once the
        // first Identity entities exist. Until then the Domain purity rule above is
        // vacuously true, and that is a fact about Phase 1, not a passing grade.
        Assert.True(Domain_Assemblies.Application.GetTypes().Length > 5,
            "Application should contain the port abstractions; too few types suggests the wrong assembly was loaded.");
        Assert.NotEmpty(Domain_Assemblies.Infrastructure.GetTypes());
        Assert.NotEmpty(Domain_Assemblies.Api.GetTypes());
        Assert.NotEmpty(Domain_Assemblies.Worker.GetTypes());
    }

    [Fact]
    public void Hosts_do_not_reference_each_other()
    {
        // Api and Worker are peers. If the worker starts calling into the API's types,
        // shared logic is living in the wrong layer and belongs in Application, where
        // both can use it and both can test it.
        var apiResult = Types.InAssembly(Domain_Assemblies.Api)
            .ShouldNot().HaveDependencyOn(Worker).GetResult();
        var workerResult = Types.InAssembly(Domain_Assemblies.Worker)
            .ShouldNot().HaveDependencyOn(Api).GetResult();

        AssertSuccess(apiResult);
        AssertSuccess(workerResult);
    }

    private static void AssertSuccess(NetArchTest.Rules.TestResult result)
    {
        if (!result.IsSuccessful)
        {
            // The default failure message lists types but not what they depended on,
            // which makes the fix a guessing game. Naming the offenders explicitly is
            // what makes these tests actually usable when they fail at 6pm.
            var offenders = result.FailingTypeNames is null
                ? "(none reported)"
                : string.Join(", ", result.FailingTypeNames);

            Assert.Fail($"Architecture rule violated. Offending types: {offenders}");
        }
    }
}

/// <summary>
/// Loads the assemblies under test by name rather than by <c>typeof(X)</c>.
///
/// Referencing types directly would work, but it creates a compile-time dependency
/// from the test project to every type it mentions, and — more importantly — a test
/// that fails to load an assembly because a type was deleted is a confusing failure.
/// Loading by name gives a clear "assembly not found" instead, and keeps this file
/// stable as the codebase grows.
/// </summary>
internal static class Domain_Assemblies
{
    public static readonly System.Reflection.Assembly Domain =
        Load("TallyBalance.Domain");

    public static readonly System.Reflection.Assembly Application =
        Load("TallyBalance.Application");

    public static readonly System.Reflection.Assembly Infrastructure =
        Load("TallyBalance.Infrastructure");

    public static readonly System.Reflection.Assembly Api =
        Load("TallyBalance.Api");

    public static readonly System.Reflection.Assembly Worker =
        Load("TallyBalance.Worker");

    private static System.Reflection.Assembly Load(string name)
    {
        try
        {
            return System.Reflection.Assembly.Load(name);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Could not load assembly '{name}' for architecture tests. Ensure the project " +
                $"is referenced by TallyBalance.ArchitectureTests and has been built.", ex);
        }
    }
}

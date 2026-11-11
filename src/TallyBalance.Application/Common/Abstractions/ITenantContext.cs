namespace TallyBalance.Application.Common.Abstractions;

/// <summary>
/// Mutable holder for the current tenant, set once at the edge of the system
/// (middleware for HTTP, the message consumer for RabbitMQ) and read by the EF Core
/// query filter that enforces tenant isolation.
///
/// Separated from <see cref="ICurrentUser"/> because the two have different lifetimes
/// and different failure modes: identity is established by authentication, whereas the
/// active tenant is a <em>selection</em> a user with several memberships makes — and a
/// request with an authenticated user but no resolved tenant must fail closed rather
/// than silently read across organizations.
/// </summary>
public interface ITenantContext
{
    Guid? OrganizationId { get; }

    /// <summary>
    /// True once a tenant has been resolved and authorized for this scope.
    /// Repository and query code must treat <c>false</c> as "deny", not "no filter".
    /// </summary>
    bool HasTenant { get; }

    /// <summary>
    /// Sets the active tenant. Intended for the composition root, middleware, and tests only —
    /// never callable from a use case, which must inherit the tenant rather than choose it.
    /// </summary>
    void SetTenant(Guid organizationId);
}

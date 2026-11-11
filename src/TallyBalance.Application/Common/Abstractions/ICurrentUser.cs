namespace TallyBalance.Application.Common.Abstractions;

/// <summary>
/// Identity of the caller for the current request or background job.
///
/// Application code depends on this abstraction rather than on
/// <c>HttpContext.User</c>: that is what keeps the Application layer free of
/// ASP.NET Core, and what lets a <c>BackgroundService</c> consume the same use cases
/// as an HTTP endpoint. A background job has no logged-in user, so the fields are
/// nullable and the job supplies a system identity explicitly.
/// </summary>
public interface ICurrentUser
{
    /// <summary>Id of the authenticated user, or <c>null</c> for unauthenticated/system work.</summary>
    Guid? UserId { get; }

    /// <summary>
    /// Tenant this request is scoped to. Populated from membership claims after the
    /// user's organization access has been verified — never from a query string or
    /// request body, which would let a caller switch tenants by editing a parameter.
    /// </summary>
    Guid? OrganizationId { get; }

    /// <summary>True when the caller is authenticated and an active member of <see cref="OrganizationId"/>.</summary>
    bool IsAuthenticated { get; }

    /// <summary>True when the caller is a platform operator rather than an organization user.</summary>
    bool IsPlatformAdmin { get; }
}

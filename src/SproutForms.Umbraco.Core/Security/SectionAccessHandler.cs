using Microsoft.AspNetCore.Authorization;
using Umbraco.Cms.Api.Management.Security.Authorization;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Security.Authorization;
using Umbraco.Extensions;

namespace SproutForms.Umbraco.Core.Security
{
    /// <summary>
    /// Checks the sections of the user's groups as they are now, the same way Umbraco authorizes its own sections
    /// </summary>
    public sealed class SectionAccessHandler : MustSatisfyRequirementAuthorizationHandler<SectionAccessRequirement>
    {
        private readonly IAuthorizationHelper _authorizationHelper;

        public SectionAccessHandler(IAuthorizationHelper authorizationHelper)
        {
            _authorizationHelper = authorizationHelper;
        }

        protected override Task<bool> IsAuthorized(AuthorizationHandlerContext context, SectionAccessRequirement requirement)
        {
            var allowed = _authorizationHelper.TryGetUmbracoUser(context.User, out IUser? user)
                && user.AllowedSections.ContainsAny(requirement.SectionAliases);
            return Task.FromResult(allowed);
        }
    }
}

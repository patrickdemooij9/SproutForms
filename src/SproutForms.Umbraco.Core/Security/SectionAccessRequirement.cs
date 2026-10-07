using Microsoft.AspNetCore.Authorization;

namespace SproutForms.Umbraco.Core.Security
{
    /// <summary>
    /// The user has at least one of these backoffice sections. Umbraco's own requirement for this is internal
    /// </summary>
    public sealed class SectionAccessRequirement : IAuthorizationRequirement
    {
        public SectionAccessRequirement(params string[] sectionAliases)
        {
            SectionAliases = sectionAliases;
        }

        public IReadOnlyCollection<string> SectionAliases { get; }
    }
}

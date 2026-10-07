using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Validation.AspNetCore;
using Umbraco.Cms.Core.DependencyInjection;
using UmbConstants = Umbraco.Cms.Core.Constants;

namespace SproutForms.Umbraco.Core.Security
{
    public static class SproutFormsAuthorization
    {
        public const string SectionAlias = "sproutForms";

        /// <summary>
        /// The user has the SproutForms section: forms, their submissions, workflows and the recycle bin
        /// </summary>
        public const string SectionAccessPolicy = "SproutForms.SectionAccess";

        /// <summary>
        /// The user can browse the tree of forms, which the form picker needs in the sections where content is edited.
        /// The tree only holds the names of forms and folders
        /// </summary>
        public const string TreeAccessPolicy = "SproutForms.TreeAccess";

        /// <summary>
        /// Registers the policies the way Umbraco registers those of its own sections, so a backoffice login alone isn't enough
        /// </summary>
        public static IUmbracoBuilder AddSproutFormsAuthorization(this IUmbracoBuilder builder)
        {
            builder.Services.AddSingleton<IAuthorizationHandler, SectionAccessHandler>();
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy(SectionAccessPolicy, policy =>
                {
                    policy.AuthenticationSchemes.Add(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
                    policy.Requirements.Add(new SectionAccessRequirement(SectionAlias));
                });

                options.AddPolicy(TreeAccessPolicy, policy =>
                {
                    policy.AuthenticationSchemes.Add(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
                    policy.Requirements.Add(new SectionAccessRequirement(
                        SectionAlias,
                        UmbConstants.Applications.Content,
                        UmbConstants.Applications.Media,
                        UmbConstants.Applications.Members));
                });
            });

            return builder;
        }
    }
}

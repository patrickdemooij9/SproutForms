using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Cms.Infrastructure.Migrations;
using UmbConstants = Umbraco.Cms.Core.Constants;

public class FormsUserGroupMigration : AsyncMigrationBase
{
    private const string UserGroupAlias = "SproutForms";

    private readonly IShortStringHelper _shortStringHelper;
    private readonly IUserGroupService _userGroupService;

    public FormsUserGroupMigration(
        IMigrationContext context,
        IShortStringHelper shortStringHelper,
        IUserGroupService userGroupService)
       : base(context)
    {
        _shortStringHelper = shortStringHelper;
        _userGroupService = userGroupService;
    }

    protected override async Task MigrateAsync()
    {
        var userGroup = (await _userGroupService.GetAllAsync(0, int.MaxValue)).Items.FirstOrDefault(it => it.Alias == UmbConstants.Security.AdminGroupAlias);

        if (userGroup != null && !userGroup.AllowedSections.Contains("sproutForms"))
        {
            userGroup.AddAllowedSection("sproutForms");

            await _userGroupService.UpdateAsync(userGroup, UmbConstants.Security.SuperUserKey);
        }
    }
}
using Umbraco.Cms.Core.Services;

namespace SproutForms.Umbraco.Core.Services
{
    /// <summary>
    /// Finds the name to show for a stored user key: a backoffice user's key, or "System" for changes made by code-first forms.
    /// </summary>
    public class BackofficeUserNameResolver
    {
        public const string SystemUserKey = "System";

        private readonly IUserService _userService;

        public BackofficeUserNameResolver(IUserService userService)
        {
            _userService = userService;
        }

        // Pass the same dictionary for every item of a list, so each user is looked up once
        public async Task<string> GetNameAsync(string userKey, IDictionary<string, string> knownNames)
        {
            if (knownNames.TryGetValue(userKey, out var known)) return known;

            var name = userKey switch
            {
                SystemUserKey => "System",
                _ when Guid.TryParse(userKey, out var key) => (await _userService.GetAsync(key))?.Name ?? "Deleted user",
                _ => "Unknown user"
            };
            knownNames[userKey] = name;
            return name;
        }
    }
}

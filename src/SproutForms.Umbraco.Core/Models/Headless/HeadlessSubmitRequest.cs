using System.Text.Json;

namespace SproutForms.Umbraco.Core.Models.Headless
{
    /// <summary>
    /// A submission to the headless API. With uploads it is sent as multipart/form-data instead: "values" and "guard" as JSON parts,
    /// "pageUrl" as a text part, and each file as a part named after its field.
    /// </summary>
    public sealed class HeadlessSubmitRequest
    {
        // Keyed by field alias. Texts, numbers and booleans are all taken as text, like a posted HTML form
        public Dictionary<string, JsonElement> Values { get; init; } = [];

        // The page of the front-end the form was on; only stored when it is on one of SproutForms:Headless:AllowedOrigins
        public string? PageUrl { get; init; }

        // What the submission guard checks, such as { "g-recaptcha-response": token } or the empty honeypot field
        public Dictionary<string, string> Guard { get; init; } = [];
    }
}

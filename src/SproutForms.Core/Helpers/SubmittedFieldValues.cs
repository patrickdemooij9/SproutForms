using System.Text.Json;
using System.Text.Json.Nodes;

namespace SproutForms.Core.Helpers
{
    public static class SubmittedFieldValues
    {
        /// <summary>
        /// The values of a posted HTML form. The inputs of a field group's entries are named by their path, such as "people[0].firstName",
        /// and come together in the group's list of entries. Which of them the form's fields take is up to the submission service.
        /// </summary>
        public static Dictionary<string, JsonElement> FromPostedForm(IEnumerable<KeyValuePair<string, string>> values)
        {
            var result = new JsonObject();
            foreach (var (name, value) in values)
            {
                if (FieldPath.TryParse(name, out var path))
                    path.SetValue(result, JsonValue.Create(value));
            }
            return result.ToDictionary(it => it.Key, it => JsonSerializer.SerializeToElement(it.Value));
        }
    }
}

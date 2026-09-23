using SproutForms.Core.Fields.Configs;
using SproutForms.Core.Models.FormTypes;
using SproutForms.Core.Models.Outcomes;
using SproutForms.Core.Repositories;
using System.Text.Json;

namespace SproutForms.Site.Examples.Poll
{
    /// <summary>
    /// Example outcome: after voting, the visitor sees how everyone voted. It counts the votes of all the form's submissions,
    /// so it shows how an outcome can use a repository. The "pollResults" handler in wwwroot/examples/form-type-examples.js
    /// draws the bars.
    /// </summary>
    public class PollResultsOutcomeType : IFormSubmitOutcomeType, IRestrictedToFormTypes
    {
        public const string TypeAlias = "pollResults";

        private readonly IFormSubmissionRepository _submissions;

        public PollResultsOutcomeType(IFormSubmissionRepository submissions)
        {
            _submissions = submissions;
        }

        public string Alias => TypeAlias;

        public Type ConfigurationType => typeof(PollResultsOutcomeConfig);

        public IReadOnlyCollection<string> FormTypeAliases => [PollFormType.TypeAlias];

        public object GetDefaultConfiguration() => new PollResultsOutcomeConfig();

        public Task<OutcomeResult> HandleAsync(FormSubmitOutcomeContext context, CancellationToken cancellationToken)
        {
            // Loads every submission of the form; for a busy poll, cache the counts or keep a running total instead
            var submissions = _submissions.GetByForm(context.Version.FormId, 0, int.MaxValue, out var totalVotes);

            var questions = context.Version.Definition.Fields
                .Where(field => field.Configuration is RadioFieldConfig)
                .Select(field =>
                {
                    var options = ((RadioFieldConfig)field.Configuration).Options;
                    // Distinct: an editor can give two options the same value
                    var votes = options.Select(option => option.Value).Distinct().ToDictionary(value => value, _ => 0);
                    foreach (var submission in submissions)
                    {
                        if (submission.Values.TryGetValue(field.Alias, out var value)
                            && value.ValueKind == JsonValueKind.String
                            && votes.ContainsKey(value.GetString()!))
                        {
                            votes[value.GetString()!]++;
                        }
                    }

                    var questionTotal = votes.Values.Sum();
                    return new
                    {
                        question = field.Label,
                        votedFor = context.Submission.Values.TryGetValue(field.Alias, out var own) && own.ValueKind == JsonValueKind.String ? own.GetString() : null,
                        options = options.Select(option => new
                        {
                            value = option.Value,
                            label = option.Label,
                            votes = votes[option.Value],
                            percentage = questionTotal == 0 ? 0 : Math.Round(100.0 * votes[option.Value] / questionTotal)
                        })
                    };
                })
                .ToList();

            return Task.FromResult(new OutcomeResult
            {
                Data = new Dictionary<string, object?>
                {
                    ["totalVotes"] = totalVotes,
                    ["questions"] = questions,
                    // Shown when JavaScript is off
                    ["message"] = $"Thanks for voting! {totalVotes} people have voted so far."
                }
            });
        }
    }

    public class PollResultsOutcomeConfig
    {
    }
}

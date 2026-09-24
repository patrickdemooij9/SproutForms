using SproutForms.Core.Models.FormTypes;
using SproutForms.Core.Models.Outcomes;
using SproutForms.Core.Registry;
using SproutForms.Site.Examples.Poll;
using SproutForms.Site.Examples.ProductFinder;
using SproutForms.Site.Examples.Quiz;
using SproutForms.Umbraco.Core.Descriptors.FormTypes;
using SproutForms.Umbraco.Core.Descriptors.Outcomes;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace SproutForms.Site.Examples
{
    /// <summary>
    /// Registers the example form types. Each one needs its type, a descriptor for the backoffice, and the outcome that shows
    /// its result. The outcomes' front-end handlers are in wwwroot/examples/form-type-examples.js.
    /// </summary>
    public class ExamplesComposer : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
        {
            builder.Services.AddSingleton<IFormDefinitionType, QuizFormType>();
            builder.Services.AddSingleton<IFormDefinitionTypeDescriptor, QuizFormTypeDescriptor>();
            builder.Services.AddSingleton<IFormSubmitOutcomeType, QuizResultOutcomeType>();
            builder.Services.AddSingleton<IOutcomeDescriptor, QuizResultOutcomeDescriptor>();

            builder.Services.AddSingleton<IFormDefinitionType, PollFormType>();
            builder.Services.AddSingleton<IFormDefinitionTypeDescriptor, PollFormTypeDescriptor>();
            builder.Services.AddSingleton<IFormSubmitOutcomeType, PollResultsOutcomeType>();
            builder.Services.AddSingleton<IOutcomeDescriptor, PollResultsOutcomeDescriptor>();

            builder.Services.AddSingleton<IFormDefinitionType, ProductFinderFormType>();
            builder.Services.AddSingleton<IFormDefinitionTypeDescriptor, ProductFinderFormTypeDescriptor>();
            builder.Services.AddSingleton<IFormSubmitOutcomeType, ProductRecommendationOutcomeType>();
            builder.Services.AddSingleton<IOutcomeDescriptor, ProductRecommendationOutcomeDescriptor>();

            builder.Services.AddCodeFirstForms(forms =>
            {
                forms.Add<ExampleQuizForm>();
                forms.Add<ExamplePollForm>();
                forms.Add<ExampleProductFinderForm>();
            });
        }
    }
}

using SproutForms.Core.Models;
using SproutForms.Core.Models.Calculations;
using SproutForms.Core.Models.ClientModels;
using SproutForms.Core.Models.FormTypes;
using SproutForms.Core.Models.SubmissionGuard;

namespace SproutForms.Core.Services
{
    /// <summary>
    /// Builds the <see cref="FormClientModel"/> of a published form: what the headless API returns, and what the Razor view model is built from.
    /// </summary>
    public class FormClientModelBuilder
    {
        private readonly IFormFieldType[] _fieldTypes;
        private readonly IFormDefinitionType[] _formTypes;
        private readonly IFormSubmissionGuard? _submissionGuard;

        public FormClientModelBuilder(
            IEnumerable<IFormFieldType> fieldTypes,
            IEnumerable<IFormDefinitionType> formTypes,
            IFormSubmissionGuard? submissionGuard = null)
        {
            _fieldTypes = [.. fieldTypes];
            _formTypes = [.. formTypes];
            _submissionGuard = submissionGuard;
        }

        public FormClientModel Build(Form form, FormVersion version)
        {
            var definition = version.Definition;
            var formType = _formTypes.FirstOrDefault(it => it.Alias == definition.Type.TypeAlias);

            // A variable that isn't exposed or needed by a condition stays on the server, with its rules, so they can't give away answers
            var clientVariables = new CalculationDependencies(definition).GetClientVariables();

            return new FormClientModel
            {
                Id = form.Id,
                Alias = form.Alias,
                VersionId = version.Id,
                FormType = definition.Type.TypeAlias,
                FormTypeSettings = formType?.GetClientSettings(definition),
                SubmitLabel = string.IsNullOrWhiteSpace(definition.SubmitLabel) ? FormTexts.Submit : definition.SubmitLabel,
                ShowProgress = definition.ShowProgress,
                Pages = definition.Pages.Select((page, index) => BuildPage(version, page, index, formType)).ToList(),
                Variables = definition.Variables
                    .Where(variable => clientVariables.Contains(variable.Alias))
                    .Select(variable => new FormClientVariable
                    {
                        Alias = variable.Alias,
                        Type = variable.Type,
                        InitialValue = variable.InitialValue,
                        Decimals = variable.Decimals
                    }).ToList(),
                Calculations = definition.Calculations.Where(rule => clientVariables.Contains(rule.VariableAlias)).ToList(),
                SubmissionGuard = _submissionGuard is null ? null : new FormClientSubmissionGuard
                {
                    Alias = _submissionGuard.Alias,
                    Settings = _submissionGuard.GetFrontendSettings()
                },
                Texts = new FormClientTexts()
            };
        }

        private FormClientPage BuildPage(FormVersion version, FormPage page, int index, IFormDefinitionType? formType)
            => new()
            {
                Index = index,
                Title = page.Title,
                ProgressLabel = string.IsNullOrWhiteSpace(page.Title) ? FormTexts.Step(index) : page.Title,
                NextLabel = string.IsNullOrWhiteSpace(page.NextLabel) ? FormTexts.Next : page.NextLabel,
                PreviousLabel = string.IsNullOrWhiteSpace(page.PreviousLabel) ? FormTexts.Previous : page.PreviousLabel,
                Visibility = page.Visibility,
                Rows = BuildRows(version, page, version.Definition.Fields, formType)
            };

        private List<FormClientRow> BuildRows(FormVersion version, IFormLayout layout, IReadOnlyList<FormField> fields, IFormDefinitionType? formType)
            => layout.Rows.Select(row => new FormClientRow
            {
                Columns = row.Columns.Select(column => new FormClientColumn
                {
                    Width = column.Width,
                    Field = BuildField(version, fields.First(field => field.Alias == column.FieldAlias), formType)
                }).ToList()
            }).ToList();

        private FormClientField BuildField(FormVersion version, FormField field, IFormDefinitionType? formType)
        {
            // Fail closed, as submitting does: a field whose type is no longer registered can't be rendered safely
            var fieldType = _fieldTypes.FirstOrDefault(it => it.Alias == field.FieldTypeAlias)
                ?? throw new InvalidOperationException($"Form '{version.FormId}' has field '{field.Alias}' with field type '{field.FieldTypeAlias}', which is not registered.");

            return new FormClientField
            {
                Alias = field.Alias,
                Label = field.Label,
                Type = fieldType.Alias,
                Required = field.Required,
                RendersOwnLabel = fieldType.RendersOwnLabel,
                Configuration = fieldType.GetClientConfiguration(field.Configuration),
                Rules = field.Rules,
                ValidationRules = GetValidationRules(field, fieldType),
                Extension = field.Extension is null ? null : formType?.GetClientFieldExtension(field),
                Rows = field.Configuration is IFormFieldGroupConfiguration group ? BuildRows(version, group, group.Fields, formType) : null
            };
        }

        // Required comes first, so an empty field shows the same message the server would
        private static List<ValidationRule> GetValidationRules(FormField field, IFormFieldType fieldType)
        {
            var rules = new List<ValidationRule>();
            if (field.Required)
            {
                rules.Add(new ValidationRule
                {
                    Type = "required",
                    Message = FormTexts.Required
                });
            }
            rules.AddRange(fieldType.GetValidationRules(field.Configuration));
            return rules;
        }
    }
}

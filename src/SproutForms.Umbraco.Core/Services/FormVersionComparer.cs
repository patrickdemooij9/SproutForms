using SproutForms.Core.Models;
using SproutForms.Core.Models.Flows;
using SproutForms.Core.Repositories;
using SproutForms.Umbraco.Core.Descriptors.Fields;
using SproutForms.Umbraco.Core.Descriptors.Flows;
using SproutForms.Umbraco.Core.Descriptors.FormTypes;
using SproutForms.Umbraco.Core.Descriptors.Outcomes;
using SproutForms.Umbraco.Core.Models.ViewModels;
using System.Text.Json;

namespace SproutForms.Umbraco.Core.Services
{
    /// <summary>
    /// Lists what changes in the current definition of a form when it is rolled back to an older one, named the way the backoffice shows them.
    /// </summary>
    public class FormVersionComparer
    {
        private readonly IEnumerable<IFieldDescriptor> _fieldDescriptors;
        private readonly IEnumerable<IFlowDescriptor> _flowDescriptors;
        private readonly IEnumerable<IOutcomeDescriptor> _outcomeDescriptors;
        private readonly IEnumerable<IFormDefinitionTypeDescriptor> _formTypeDescriptors;
        private readonly IWorkflowTemplateRepository _templateRepository;

        public FormVersionComparer(
            IEnumerable<IFieldDescriptor> fieldDescriptors,
            IEnumerable<IFlowDescriptor> flowDescriptors,
            IEnumerable<IOutcomeDescriptor> outcomeDescriptors,
            IEnumerable<IFormDefinitionTypeDescriptor> formTypeDescriptors,
            IWorkflowTemplateRepository templateRepository)
        {
            _fieldDescriptors = fieldDescriptors;
            _flowDescriptors = flowDescriptors;
            _outcomeDescriptors = outcomeDescriptors;
            _formTypeDescriptors = formTypeDescriptors;
            _templateRepository = templateRepository;
        }

        public List<FormChangeBackofficeModel> Compare(FormDefinition current, FormDefinition version)
        {
            var formTypeDescriptor = _formTypeDescriptors.FirstOrDefault(it => it.FormTypeAlias == current.Type.TypeAlias);
            var changes = new List<FormChangeBackofficeModel>();

            AddChange(changes, FormChangeSection.Form, "Settings", DescribeSettings(current, formTypeDescriptor), DescribeSettings(version, formTypeDescriptor));

            var pageCount = Math.Max(current.Pages.Count, version.Pages.Count);
            for (var i = 0; i < pageCount; i++)
            {
                AddChange(changes, FormChangeSection.Page, PageName(current, version, i),
                    i < current.Pages.Count ? DescribePage(current.Pages[i], current) : null,
                    i < version.Pages.Count ? DescribePage(version.Pages[i], version) : null);
            }

            foreach (var alias in MatchByAlias(current.Fields.Select(it => it.Alias), version.Fields.Select(it => it.Alias)))
            {
                var currentField = current.Fields.FirstOrDefault(it => it.Alias == alias);
                var versionField = version.Fields.FirstOrDefault(it => it.Alias == alias);
                var label = versionField?.Label ?? currentField!.Label;
                AddChange(changes, FormChangeSection.Field, ItemName(label, alias),
                    currentField is null ? null : DescribeField(currentField, formTypeDescriptor),
                    versionField is null ? null : DescribeField(versionField, formTypeDescriptor));
            }

            foreach (var alias in MatchByAlias(current.Workflows.Select(it => it.Alias), version.Workflows.Select(it => it.Alias)))
            {
                var currentWorkflow = current.Workflows.FirstOrDefault(it => it.Alias == alias);
                var versionWorkflow = version.Workflows.FirstOrDefault(it => it.Alias == alias);
                var workflowTypeAlias = (versionWorkflow ?? currentWorkflow)!.WorkflowTypeAlias;
                var displayName = _flowDescriptors.FirstOrDefault(it => it.FlowTypeAlias == workflowTypeAlias)?.DisplayName ?? workflowTypeAlias;
                AddChange(changes, FormChangeSection.Workflow, ItemName(displayName, alias),
                    currentWorkflow is null ? null : DescribeWorkflow(currentWorkflow),
                    versionWorkflow is null ? null : DescribeWorkflow(versionWorkflow));
            }

            AddChange(changes, FormChangeSection.Outcome, "After submitting", DescribeOutcome(current.SubmitOutcome), DescribeOutcome(version.SubmitOutcome));

            return changes;
        }

        // A null description means the item doesn't exist on that side
        private static void AddChange(List<FormChangeBackofficeModel> changes, FormChangeSection section, string name,
            Dictionary<string, string?>? current, Dictionary<string, string?>? version)
        {
            if (current is null && version is null) return;

            var properties = new List<FormPropertyChangeBackofficeModel>();
            foreach (var key in (current?.Keys ?? Enumerable.Empty<string>()).Union(version?.Keys ?? Enumerable.Empty<string>()))
            {
                string? currentValue = null;
                string? versionValue = null;
                current?.TryGetValue(key, out currentValue);
                version?.TryGetValue(key, out versionValue);
                if (currentValue == versionValue) continue;

                properties.Add(new FormPropertyChangeBackofficeModel
                {
                    Name = key,
                    CurrentValue = currentValue,
                    VersionValue = versionValue
                });
            }

            if (current != null && version != null && properties.Count == 0) return;

            changes.Add(new FormChangeBackofficeModel
            {
                Section = section,
                Name = name,
                ChangeType = current is null ? FormChangeType.Added : version is null ? FormChangeType.Removed : FormChangeType.Changed,
                Properties = properties
            });
        }

        // Keeps the order of the current version, followed by what only the older version has
        private static IEnumerable<string> MatchByAlias(IEnumerable<string> current, IEnumerable<string> version)
            => current.Union(version, StringComparer.OrdinalIgnoreCase);

        // The backoffice gives fields and workflows a generated alias, which means nothing to an editor
        private static string ItemName(string name, string alias)
            => Guid.TryParse(alias, out _) ? name : $"{name} ({alias})";

        private static string PageName(FormDefinition current, FormDefinition version, int index)
        {
            var title = (index < version.Pages.Count ? version.Pages[index].Title : null)
                ?? (index < current.Pages.Count ? current.Pages[index].Title : null);
            return string.IsNullOrWhiteSpace(title) ? $"Page {index + 1}" : $"Page {index + 1}: {title}";
        }

        private static Dictionary<string, string?> DescribeSettings(FormDefinition definition, IFormDefinitionTypeDescriptor? formTypeDescriptor)
        {
            var description = new Dictionary<string, string?>
            {
                ["Submit button label"] = definition.SubmitLabel,
                ["Show progress"] = Format(definition.ShowProgress)
            };
            AddProperties(description, formTypeDescriptor is null ? null : formTypeDescriptor.FromConfig, definition.Type.Settings, "Form type settings");
            return description;
        }

        private static Dictionary<string, string?> DescribePage(FormPage page, FormDefinition definition)
        {
            // The layout reads like the canvas: columns side by side, rows below each other
            var layout = string.Join(" / ", page.Rows.Select(row => string.Join(" + ", row.Columns.Select(column =>
            {
                var label = definition.Fields.FirstOrDefault(it => it.Alias == column.FieldAlias)?.Label ?? column.FieldAlias;
                return $"{label} ({column.Width})";
            }))));

            return new Dictionary<string, string?>
            {
                ["Title"] = page.Title,
                ["Fields"] = layout,
                ["Next button label"] = page.NextLabel,
                ["Previous button label"] = page.PreviousLabel,
                ["Visibility"] = page.Visibility is null ? null : JsonSerializer.Serialize(page.Visibility)
            };
        }

        private Dictionary<string, string?> DescribeField(FormField field, IFormDefinitionTypeDescriptor? formTypeDescriptor)
        {
            var descriptor = _fieldDescriptors.FirstOrDefault(it => it.FieldTypeAlias == field.FieldTypeAlias);
            var description = new Dictionary<string, string?>
            {
                ["Label"] = field.Label,
                ["Field type"] = descriptor?.DisplayName ?? field.FieldTypeAlias,
                ["Required"] = Format(field.Required)
            };
            AddProperties(description, descriptor is null ? null : descriptor.FromConfig, field.Configuration, "Configuration");

            description["Conditions"] = field.Conditions is null ? null : JsonSerializer.Serialize(field.Conditions);

            if (field.Extension != null)
            {
                var extensionDescriptor = formTypeDescriptor?.FieldExtensions.FirstOrDefault(it => it.FieldTypeAlias == field.FieldTypeAlias);
                AddProperties(description, extensionDescriptor is null ? null : extensionDescriptor.FromConfig, field.Extension.Settings, "Form type settings");
            }
            return description;
        }

        private Dictionary<string, string?> DescribeWorkflow(FormWorkflow workflow)
        {
            var descriptor = _flowDescriptors.FirstOrDefault(it => it.FlowTypeAlias == workflow.WorkflowTypeAlias);
            var description = new Dictionary<string, string?>
            {
                ["Order"] = workflow.Order.ToString(),
                ["Template"] = workflow.TemplateId is { } templateId
                    ? _templateRepository.GetById(templateId)?.Name ?? $"Deleted template ({templateId})"
                    : null
            };
            AddProperties(description, descriptor is null ? null : descriptor.FromConfig, workflow.Configuration, "Configuration");
            return description;
        }

        private Dictionary<string, string?> DescribeOutcome(FormSubmitOutcome outcome)
        {
            var descriptor = _outcomeDescriptors.FirstOrDefault(it => it.OutcomeTypeAlias == outcome.OutcomeTypeAlias);
            var description = new Dictionary<string, string?>
            {
                ["Type"] = descriptor?.DisplayName ?? outcome.OutcomeTypeAlias
            };
            AddProperties(description, descriptor is null ? null : descriptor.FromConfig, outcome.Configuration, "Configuration");
            return description;
        }

        // A type that's no longer registered left its configuration as raw JSON, which its descriptor can't read
        private static void AddProperties(Dictionary<string, string?> description, Func<object, FormPropertyBackofficeModel[]>? fromConfig, object? configuration, string rawName)
        {
            if (configuration is null) return;

            if (fromConfig is null || configuration is JsonElement)
            {
                description[rawName] = JsonSerializer.Serialize(configuration);
                return;
            }

            foreach (var property in fromConfig(configuration))
            {
                description[property.DisplayName] = Format(property.Value);
            }
        }

        private static string? Format(object? value) => value switch
        {
            null => null,
            string text => text.Length == 0 ? null : text,
            bool flag => flag ? "Yes" : "No",
            _ => JsonSerializer.Serialize(value)
        };
    }
}

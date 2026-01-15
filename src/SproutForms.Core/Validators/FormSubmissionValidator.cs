using SproutForms.Core.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace SproutForms.Core.Validators
{
    public class FormSubmissionValidator
    {
        /*private readonly IFormFieldTypeRegistry _registry;

        public FormSubmissionValidator(IFormFiel)
        {
            _registry = registry;
        }

        public ValidationResult Validate(FormDefinition definition, FormSubmission submission)
        {
            var errors = new List<string>();

            foreach (var field in definition.Fields)
            {
                submission.Values.TryGetValue(field.Id, out var value);

                var fieldType = _registry.Get(field.Type);
                var result = fieldType.Validate(value, field);

                if (!result.IsValid)
                    errors.Add($"{field.Name}: {result.Error}");
            }

            return errors.Any()
                ? ValidationResult.Fail(errors)
                : ValidationResult.Success();
        }*/
    }
}

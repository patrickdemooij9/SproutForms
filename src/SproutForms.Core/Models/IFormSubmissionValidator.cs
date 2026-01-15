using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace SproutForms.Core.Models
{
    public interface IFormSubmissionValidator
    {
        ValidationResult Validate(FormDefinition definition, FormSubmission submission);
    }
}

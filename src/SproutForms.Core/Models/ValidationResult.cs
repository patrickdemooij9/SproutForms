using System;
using System.Collections.Generic;
using System.Text;

namespace SproutForms.Core.Models
{
    public sealed class ValidationResult
    {
        public bool IsValid { get; }
        public IReadOnlyList<string> Errors { get; }

        private ValidationResult(bool isValid, IReadOnlyList<string> errors)
        {
            IsValid = isValid;
            Errors = errors;
        }

        public static ValidationResult Success() =>
            new(true, Array.Empty<string>());

        public static ValidationResult Fail(string error) =>
            new(false, new[] { error });

        public static ValidationResult Fail(IEnumerable<string> errors) =>
            new(false, errors.ToList());
    }

}

namespace SproutForms.Core.Models.Outcomes
{
    public interface IFormSubmitOutcomeType
    {
        public string Alias { get; }
        public Type ConfigurationType { get; }

        public object GetDefaultConfiguration();

        /// <summary>
        /// Decides what the visitor sees after a successful submit. The data goes to the outcome handler registered for this alias in
        /// forms.js; without JavaScript, only a "url" (redirect) or a "message" is used.
        /// </summary>
        public Task<OutcomeResult> HandleAsync(FormSubmitOutcomeContext context, CancellationToken cancellationToken);
    }
}

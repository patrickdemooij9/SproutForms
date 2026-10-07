namespace SproutForms.Core.Models.SubmissionGuard
{
    public class RecaptchaV3Options
    {
        public string SiteKey { get; set; }
        public string SecretKey { get; set; }
        public double MinimumScore { get; set; } = 0.5;
        public string Action { get; set; } = "submit";
    }
}

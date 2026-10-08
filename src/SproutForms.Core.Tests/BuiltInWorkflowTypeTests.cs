using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SproutForms.Core.Builders;
using SproutForms.Core.Fields.Configs;
using SproutForms.Core.Flows;
using SproutForms.Core.Flows.Configs;
using SproutForms.Core.Models;
using SproutForms.Core.Models.Flows;
using SproutForms.Core.Models.Flows.Email;
using SproutForms.Core.Services;
using System.Net;
using System.Text.Json;

namespace SproutForms.Core.Tests
{
    public class BuiltInWorkflowTypeTests
    {
        private static readonly FormValueFormatter Formatter = new(TestForms.FieldTypes);

        [TestCase(HttpStatusCode.ServiceUnavailable, true)]
        [TestCase(HttpStatusCode.InternalServerError, true)]
        [TestCase(HttpStatusCode.TooManyRequests, true)]
        [TestCase(HttpStatusCode.RequestTimeout, true)]
        [TestCase(HttpStatusCode.BadRequest, false)]
        [TestCase(HttpStatusCode.NotFound, false)]
        public async Task An_http_workflow_only_retries_an_answer_that_may_pass(HttpStatusCode statusCode, bool retryable)
        {
            var workflow = Slack(new StubHandler(_ => new HttpResponseMessage(statusCode)));

            var result = await workflow.ExecuteAsync(Context(new SlackWorkflowConfig { WebhookUrl = "https://hooks.example.com/x", Message = "Hi" }), CancellationToken.None);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Retryable, Is.EqualTo(retryable));
        }

        [Test]
        public async Task An_http_workflow_retries_when_the_server_cant_be_reached()
        {
            var workflow = Slack(new StubHandler(_ => throw new HttpRequestException("Connection refused")));

            var result = await workflow.ExecuteAsync(Context(new SlackWorkflowConfig { WebhookUrl = "https://hooks.example.com/x", Message = "Hi" }), CancellationToken.None);

            Assert.That(result.Retryable, Is.True);
        }

        [Test]
        public async Task An_http_workflow_without_a_url_is_not_retried()
        {
            var result = await Slack(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)))
                .ExecuteAsync(Context(new SlackWorkflowConfig { Message = "Hi" }), CancellationToken.None);

            Assert.That(result.Retryable, Is.False);
        }

        [TestCase(typeof(IOException), true)]
        [TestCase(typeof(InvalidOperationException), false)]
        [TestCase(typeof(FormatException), false)]
        public async Task An_email_is_retried_unless_its_configuration_is_wrong(Type exceptionType, bool retryable)
        {
            var sender = Substitute.For<IEmailSender>();
            sender.SendAsync(default!, default!, default!, default!, default).ThrowsAsyncForAnyArgs((Exception)Activator.CreateInstance(exceptionType, "Failed")!);

            var result = await Email(sender).ExecuteAsync(Context(new EmailWorkflowConfig { From = "a@example.com", To = "b@example.com", Subject = "Hi" }), CancellationToken.None);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Retryable, Is.EqualTo(retryable));
        }

        [Test]
        public async Task The_email_subject_fills_in_field_values_on_one_line()
        {
            var sender = Substitute.For<IEmailSender>();
            var config = new EmailWorkflowConfig { From = "a@example.com", To = "b@example.com", Subject = "Question from {name}: {message}" };

            await Email(sender).ExecuteAsync(Context(config, new() { ["name"] = "Ann", ["message"] = "Line one\r\nBcc: someone@example.com" }), CancellationToken.None);

            await sender.Received().SendAsync("a@example.com", "b@example.com", "Question from Ann: Line one Bcc: someone@example.com", Arg.Any<string>(), Arg.Any<CancellationToken>());
        }

        [Test]
        public void A_textarea_can_be_configured_in_code()
        {
            var definition = new FormBuilder("contact", "Contact")
                .Row(row => row.Col(12, col => col.Textarea("message", "Message").Set(config => { config.Rows = 3; config.MaxLength = 500; }).Done()))
                .Build();

            var config = (TextAreaConfig)definition.FindField("message")!.Configuration;
            Assert.That((config.Rows, config.MaxLength), Is.EqualTo((3, (int?)500)));
        }

        [Test]
        public void A_code_first_form_can_hide_its_progress()
        {
            var builder = new FormBuilder("order", "Order")
                .Page("One", page => page.Row(row => row.Col(12, col => col.Text("a", "A").Done())))
                .Page("Two", page => page.Row(row => row.Col(12, col => col.Text("b", "B").Done())));

            Assert.That(builder.Build().ShowProgress, Is.True);
            Assert.That(builder.ShowProgress(false).Build().ShowProgress, Is.False);
        }

        private static SlackWorkflowType Slack(HttpMessageHandler handler)
        {
            var factory = Substitute.For<IHttpClientFactory>();
            factory.CreateClient(Arg.Any<string>()).Returns(_ => new HttpClient(handler, disposeHandler: false));
            return new SlackWorkflowType(factory, new WorkflowMessageResolver(Formatter));
        }

        private static EmailWorkflowType Email(IEmailSender sender) => new(sender, Formatter, new WorkflowMessageResolver(Formatter));

        private static WorkflowContext Context(object configuration, Dictionary<string, string>? values = null)
        {
            var definition = new FormBuilder("contact", "Contact")
                .Row(row => row.Col(12, col => col.Text("name", "Name").Done()))
                .Row(row => row.Col(12, col => col.Textarea("message", "Message").Done()))
                .Build();

            return new WorkflowContext
            {
                Workflow = new FormWorkflow { Alias = "notify", WorkflowTypeAlias = "test", Configuration = configuration },
                Submission = new FormSubmission
                {
                    Id = Guid.NewGuid(),
                    Values = (values ?? []).ToDictionary(it => it.Key, it => JsonSerializer.SerializeToElement(it.Value))
                },
                Version = new FormVersion { Id = Guid.NewGuid(), FormId = Guid.NewGuid(), Definition = definition, DefinitionHash = "test", CreatedBy = "test" }
            };
        }

        private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
                => Task.FromResult(respond(request));
        }
    }
}

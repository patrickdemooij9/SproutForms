# Custom workflow types

A workflow does something with a submission after it's saved: send an email, post to Slack, create a lead in a CRM. This page builds one that posts a message to a Discord webhook.

## How workflows run

When a submission is saved, SproutForms queues one execution per workflow of the form. A background job picks up pending executions every 10 seconds and runs them, in the form's order: each waits until the workflows before it have succeeded. Every execution's status (pending, running, succeeded, failed or retrying) and its last error show with the submission in the backoffice, where a failed one can be retried.

A workflow that fails with `Retryable` set is tried again later, up to 5 attempts, with 1, 2, 4 and 8 minutes in between. A workflow that throws counts as failed and isn't retried automatically. The built-in workflows only set `Retryable` for a failure that may pass by itself, such as a network error or a 5xx answer, so that a service isn't sent the same request again for nothing.

## 1. The settings

```csharp
public class DiscordWorkflowConfig
{
    public string WebhookUrl { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
```

The settings are stored as JSON with the form, and again with each execution, so keep them to plain, serializable properties with setters.

## 2. The workflow type

```csharp
using System.Net.Http.Json;
using SproutForms.Core.Models.Flows;
using SproutForms.Core.Services;

public class DiscordWorkflowType : IFormWorkflowType
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly WorkflowMessageResolver _messageResolver;

    public DiscordWorkflowType(IHttpClientFactory httpClientFactory, WorkflowMessageResolver messageResolver)
    {
        _httpClientFactory = httpClientFactory;
        _messageResolver = messageResolver;
    }

    public string Alias => "discord";

    public Type ConfigurationType => typeof(DiscordWorkflowConfig);

    public object GetDefaultConfiguration() => new DiscordWorkflowConfig
    {
        Message = "A new submission has been submitted:\n{AllValues}"
    };

    public async Task<WorkflowExecutionResult> ExecuteAsync(WorkflowContext context, CancellationToken ct)
    {
        var config = (DiscordWorkflowConfig)context.Workflow.Configuration;
        if (string.IsNullOrWhiteSpace(config.WebhookUrl))
            return new WorkflowExecutionResult(false, "The webhook URL is missing.");

        // Fills in {alias}, {AllValues} and {var:alias}
        var content = _messageResolver.ResolveTokens(config.Message, context.Submission, context.Version);

        try
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.PostAsJsonAsync(config.WebhookUrl, new { content }, ct);

            if ((int)response.StatusCode >= 500)
                return new WorkflowExecutionResult(false, $"Discord answered {(int)response.StatusCode}.", Retryable: true);
            if (!response.IsSuccessStatusCode)
                return new WorkflowExecutionResult(false, $"Discord answered {(int)response.StatusCode}.");
        }
        catch (HttpRequestException ex)
        {
            return new WorkflowExecutionResult(false, ex.Message, Retryable: true);
        }

        return new WorkflowExecutionResult(true);
    }
}
```

- `Alias` identifies the type in stored forms and executions, so don't change it once forms use it.
- `ConfigurationType` is the class the settings are read into; `context.Workflow.Configuration` is an instance of it.
- `GetDefaultConfiguration` is what a new workflow of this type starts with in the backoffice.
- `ExecuteAsync` gets the saved submission in `context.Submission` (its `Values`, `Variables`, `Results`, `PageUrl` and, when stored, `IpAddress`) and the form version it was made with in `context.Version`.
- Return a `WorkflowExecutionResult`: `Success`, an `Error` that shows in the backoffice, and whether a failure is `Retryable`.

`WorkflowMessageResolver` and `FormValueFormatter` (both in `SproutForms.Core.Services`) turn submitted values into text, the same way the built-in workflows do. `FormValueFormatter.FormatAll(submission.Values, version.Definition.Fields)` gives each answer's label and value, for a message of your own.

## 3. Register it

With Umbraco, register the type and a descriptor in a composer:

```csharp
using Microsoft.Extensions.DependencyInjection;
using SproutForms.Core.Models.Flows;
using SproutForms.Umbraco.Core.Descriptors.Flows;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

public class DiscordWorkflowDescriptor : BaseFlowDescriptor<DiscordWorkflowConfig>
{
    public override string FlowTypeAlias => "discord";
    public override string DisplayName => "Send to Discord";
    public override string DisplayTemplate => "Send a message to Discord ({webhookUrl})";
    public override string Description => "Posts a message to a Discord channel after a submission.";

    public DiscordWorkflowDescriptor()
    {
        DefineMap(it => it.WebhookUrl, "webhookUrl", "Webhook URL", "Umb.PropertyEditorUi.TextBox");
        DefineMap(it => it.Message, "message", "Message", "sproutForms.propertyEditorUi.tokenTextarea");
    }
}

public class DiscordComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddSingleton<IFormWorkflowType, DiscordWorkflowType>();
        builder.Services.AddSingleton<IFlowDescriptor, DiscordWorkflowDescriptor>();
    }
}
```

The descriptor's `FlowTypeAlias` must match the type's `Alias`. Editors see `DisplayName` and `Description` when they add a step on the Integrations tab, and `DisplayTemplate` as the step's title, with `{alias}` filled in with the setting mapped under that alias (the email workflow's is "Send an email from {from} to {to}"). `sproutForms.propertyEditorUi.tokenTextarea` suggests the form's `{alias}` tokens as the editor types. See [Descriptors](README.md#descriptors).

Without Umbraco, register only the type:

```csharp
builder.Services.AddSingleton<IFormWorkflowType, DiscordWorkflowType>();
```

## Using it in code

`WorkflowBuilder.Add` adds a workflow of any registered type:

```csharp
.OnSubmit(workflows => workflows
    .SendEmail("notify", email => email.To("info@example.com").From("noreply@example.com").Subject("New submission"))
    .Add("discord", "discord", new DiscordWorkflowConfig
    {
        WebhookUrl = "https://discord.com/api/webhooks/...",
        Message = "New submission from {name}"
    }))
```

The first argument is the workflow's own alias in the form, the second the workflow type's alias.

## Headless

Workflows only run on the server, and a headless definition never holds them, so a headless front-end needs nothing for them. See [Custom workflow types for headless](../extending-headless/workflow-types.md).

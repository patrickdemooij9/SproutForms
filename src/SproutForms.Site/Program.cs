using SproutForms.Core.Registry;
using SproutForms.Site.Code;
using System.Security.Cryptography;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsEnvironment("AiTest"))
{
    // Throwaway admin for the unattended install. Random unless run-site.ps1 -AdminPassword sets one for a backoffice session
    if (string.IsNullOrEmpty(builder.Configuration["Umbraco:CMS:Unattended:UnattendedUserPassword"]))
    {
        builder.Configuration["Umbraco:CMS:Unattended:UnattendedUserPassword"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24));
    }

    // Serve the backoffice bundle from SproutForms.Umbraco/wwwroot, which only happens by default in Development
    builder.WebHost.UseStaticWebAssets();
    builder.Services.AddCodeFirstForms(it =>
    {
        it.Add<TestFormCode>();
        it.Add<TestFileFormCode>();
        it.Add<AiTestRequiredCheckboxForm>();
        it.Add<AiTestEdgeCasesForm>();
        it.Add<AiTestFailingWorkflowForm>();
        it.Add<AiTestWorkflowOrderForm>();
        it.Add<AiTestUnknownOutcomeForm>();
        it.Add<AiTestMultiPageForm>();
    });
}

builder.CreateUmbracoBuilder()
    .AddBackOffice()
    .AddWebsite()
    .AddComposers()
    .Build();

/*builder.Services.AddCodeFirstForms((it) =>
{
    it.Add<TestFormCode>();
    it.Add<TestFileFormCode>();
});*/

WebApplication app = builder.Build();

await app.BootUmbracoAsync();

app.UseUmbraco()
    .WithMiddleware(u =>
    {
        u.UseBackOffice();
        u.UseWebsite();
    })
    .WithEndpoints(u =>
    {
        u.UseBackOfficeEndpoints();
        u.UseWebsiteEndpoints();
    });

await app.RunAsync();

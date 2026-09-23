using SproutForms.Core.Registry;
using SproutForms.Site.Code;
using System.Security.Cryptography;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsEnvironment("AiTest"))
{
    // Throwaway admin for the unattended install; nobody signs in to this environment
    builder.Configuration["Umbraco:CMS:Unattended:UnattendedUserPassword"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24));
    builder.Services.AddCodeFirstForms(it =>
    {
        it.Add<TestFormCode>();
        it.Add<TestFileFormCode>();
        it.Add<AiTestRequiredCheckboxForm>();
        it.Add<AiTestEdgeCasesForm>();
        it.Add<AiTestFailingWorkflowForm>();
        it.Add<AiTestWorkflowOrderForm>();
        it.Add<AiTestUnknownOutcomeForm>();
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

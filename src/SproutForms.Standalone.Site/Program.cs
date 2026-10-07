using SproutForms.Core;
using SproutForms.Core.Registry;
using SproutForms.Standalone.Site.Forms;

// A plain ASP.NET Core MVC site with SproutForms and no Umbraco
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services
    .AddSproutFormsStandalone(builder.Configuration)
    .AddSproutFormsInMemoryStorage()
    .AddCodeFirstForms(forms => forms.Add<ContactForm>());

var app = builder.Build();

app.UseRouting();
app.MapStaticAssets();
app.MapDefaultControllerRoute();

app.Run();

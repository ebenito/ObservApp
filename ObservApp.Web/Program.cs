using ObservApp.Shared.Services;
using ObservApp.Shared.State;
using ObservApp.Web.Client.Services;
using ObservApp.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddLocalization();
builder.Services.AddSingleton<ILocalizationService, WebLocalizationService>();
builder.Services.AddSingleton<ISettingsService, WebSettingsService>();
builder.Services.AddSingleton<AppState>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(
		typeof(ObservApp.Shared.AssemblyReference).Assembly,
		typeof(ObservApp.Web.Client._Imports).Assembly);

app.Run();

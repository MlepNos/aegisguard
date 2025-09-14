using AegisGuard.Blazor.Components;
using MudBlazor.Services;
using Microsoft.AspNetCore.Components; // add this

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents(o =>
    {
        o.DetailedErrors = true; // <— see full exception instead of generic circuit error
    });


builder.Services.AddHttpClient("Api", (sp, c) =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    var baseUrl = cfg["ApiBaseUrl"];              // read from env/appsettings
    if (!string.IsNullOrWhiteSpace(baseUrl))
        c.BaseAddress = new Uri(baseUrl, UriKind.Absolute);
    // else you could fall back to Nav.BaseUri, but for separate backend we want absolute
});
/*
builder.Services.AddHttpClient("Api", (sp, c) =>
{
    var nav = sp.GetRequiredService<NavigationManager>();
    c.BaseAddress = new Uri(nav.BaseUri); // e.g., http://localhost:5270/
});*/

builder.Services.AddMudServices();   // <— neu


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

//app.UseHttpsRedirection();


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

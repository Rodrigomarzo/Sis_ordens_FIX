using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

var configFile = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "quickfix-client.cfg");
var fixClient = new FixClient(configFile);
builder.Services.AddSingleton(fixClient);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Lifetime.ApplicationStarted.Register(() =>
{
    fixClient.Start();
});

app.Lifetime.ApplicationStopping.Register(() =>
{
    fixClient.Stop();
});

app.Run();

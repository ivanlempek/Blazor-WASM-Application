using Blazorise.Icons.FontAwesome;
using BlazorVTPSolutions;
using BlazorVTPSolutions.DTOs;
using BlazorVTPSolutions.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Newtonsoft.Json;
using System.Text.Json;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

//builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("https://nocodebackend.azurewebsites.net") });

builder.Services.AddScoped<IEmpresaService, EmpresaService>();
builder.Services.AddScoped<ISegmentoService, SegmentoService>();
builder.Services.AddFontAwesomeIcons();

await builder.Build().RunAsync();

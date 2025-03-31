using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SecureChatProjekt.Client;



var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Services.AddSingleton<CryptographyService>();

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

await builder.Build().RunAsync();

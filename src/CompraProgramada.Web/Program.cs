using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using CompraProgramada.Web;
using CompraProgramada.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Registrar HttpClient para o frontend
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Registrar o serviço da API com URL da API
builder.Services.AddScoped(sp => 
    new CompraProgradadaApiService(
        new HttpClient(), 
        "http://localhost:5000/api"
    )
);

await builder.Build().RunAsync();

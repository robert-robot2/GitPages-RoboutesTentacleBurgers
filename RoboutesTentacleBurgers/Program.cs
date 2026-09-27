namespace RoboutesTentacleBurgers
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // Environment
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.Services.AddSingleton(sp => builder.HostEnvironment.Environment);

            // Logging
            builder.Logging.ClearProviders();
            builder.Logging.AddDebug();                                             
          
            // SpectralX Library
            builder.Services.AddScoped<SpectralXGLX.Services.GamepadService>();
            builder.Services.AddScoped<SpectralXGLX.Services.PerformanceMonitor>();

            // WarCraft Class Library
            builder.Services.AddSingleton<WarCraftLibrary.WarGameService>();
              
            // Root Components
            builder.RootComponents.Add<App>("#app");
            builder.RootComponents.Add<HeadOutlet>("head::after");


            // ── HTTP Clients ───────────────────────────────────────────────

            // Default client — Blazor app itself
            builder.Services.AddScoped(sp =>
                new HttpClient
                {
                    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
                });

            // Nova backend client
            builder.Services.AddHttpClient<NovaAdeptusLibrary.NovaAPIService>(
                client =>
                {
                    client.BaseAddress = new Uri("https://localhost:7221");
                });


            // Auth (Optional)
            builder.Services.AddOidcAuthentication(options =>
            {
                builder.Configuration.Bind("Local", options.ProviderOptions);
            });

            builder.Build().RunAsync();
        }
    }
}
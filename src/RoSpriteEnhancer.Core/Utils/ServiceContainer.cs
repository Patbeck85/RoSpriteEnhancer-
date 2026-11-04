using Microsoft.Extensions.DependencyInjection;

namespace RoSpriteEnhancer.Core.Utils
{
    /// <summary>
    /// Zentrale Dependency-Injection-Container für RoSpriteEnhancer.
    ///
    /// Registriert alle instanzbasierten Core-Dienste als Singletons und stellt
    /// einen globalen ServiceProvider zur Verfügung.
    ///
    /// Hinweis: Statische Klassen (z. B. ToolInstaller, PixelEnhancer, Waifu2xRunner)
    /// können nicht direkt mit DI registriert werden. Diese bleiben als statische
    /// Utility-Klassen bestehen. Nur instanzbasierte Klassen werden hier registriert.
    ///
    /// Verwendung:
    ///   ServiceContainer.Instance.GetRequiredService<EsrganOptions>()
    /// </summary>
    public static class ServiceContainer
    {
        private static readonly Lazy<IServiceProvider> _provider = new(BuildProvider);

        /// <summary>Der DI-Container (Singleton)</summary>
        public static IServiceProvider Instance => _provider.Value;

        private static IServiceProvider BuildProvider()
        {
            var services = new ServiceCollection();

            // --- Instanzbasierte Options-Klassen (konfigurierbar) ---
            services.AddSingleton<EsrganOptions>();
            services.AddSingleton<Waifu2xOptions>();
            services.AddSingleton<RealCuganOptions>();
            services.AddSingleton<AiFirstOptions>();

            // --- Instanzbasierte Verarbeitungs-Klassen ---
            services.AddSingleton<IncrementalManifest>();

            return services.BuildServiceProvider();
        }

        /// <summary>
        /// Holt einen Dienst aus dem DI-Container.
        /// </summary>
        public static T GetService<T>() where T : class
        {
            return Instance.GetRequiredService<T>();
        }
    }
}
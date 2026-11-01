using System;
using System.IO;
using System.Threading.Tasks;

using FFXIVTataruHelper.Factories;
using FFXIVTataruHelper.FFHandlers;
using FFXIVTataruHelper.Services.GameMemory;
using FFXIVTataruHelper.Services.HotKeys;
using FFXIVTataruHelper.Services.Logging;
using FFXIVTataruHelper.Services.Settings;
using FFXIVTataruHelper.Services.UI;
using FFXIVTataruHelper.Services.Update;
using FFXIVTataruHelper.Utils;
using FFXIVTataruHelper.ViewModel;
using FFXIVTataruHelper.WinUtils;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Translation;
using Translation.Credentials;
using Translation.Models;
using Translation.Providers.DeepL;
using Translation.Settings;

using Updater;

namespace FFXIVTataruHelper
{
    public static class AppCompositionRoot
    {
        /// <summary>
        /// Connects DeepL to the bundled local relay. Starting it takes seconds, so
        /// it happens beside the application's own start rather than in front of
        /// it; until it answers, DeepL simply goes on to the next route.
        /// </summary>
        private static ITranslationCredentialStore ConnectDeepLRelay(IServiceProvider provider)
        {
            var store = new DpapiCredentialStore();
            var custom = store.GetEndpoint(TranslationEngineName.DeepL);
            var ours = string.IsNullOrWhiteSpace(custom)
                || custom.TrimEnd('/') == ManagedRelayProcess.DefaultEndpoint;

            // The Linux launcher may pass the address of a relay of its own. Its
            // helper ends with its session, which an update restart outlives, so
            // the address is only trusted when it is not ours or someone answers.
            var launcherEndpoint = Environment.GetEnvironmentVariable(ManagedDeepLRelay.EnvironmentVariable);
            if (!string.IsNullOrWhiteSpace(launcherEndpoint)
                && launcherEndpoint.TrimEnd('/') != ManagedRelayProcess.DefaultEndpoint)
            {
                if (ManagedDeepLRelay.Configure(store, launcherEndpoint))
                    Logger.WriteLog("DeepL: connected to the local relay started by the launcher.");
                return store;
            }

            if (!ours || !store.IsEngineEnabled(TranslationEngineName.DeepL))
                return store;

            var relay = provider.GetRequiredService<ManagedRelayProcess>();
            var executable = Path.Combine(AppContext.BaseDirectory, "deepl-relay.exe");
            _ = Task.Run(async () =>
            {
                try
                {
                    var started = await relay.TryStartAsync(executable).ConfigureAwait(false);
                    if (ManagedDeepLRelay.Configure(store, started))
                        Logger.WriteLog("DeepL: connected to the bundled local relay.");
                    else if (ManagedDeepLRelay.Forget(store, ManagedRelayProcess.DefaultEndpoint))
                        Logger.WriteLog("DeepL: the local relay did not start; its address was cleared.");
                }
                catch (Exception ex)
                {
                    Logger.WriteLog("DeepL: the local relay could not be started: " + ex.GetType().Name);
                }
            });
            return store;
        }

        public static IServiceProvider BuildServiceProvider()
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            return services.BuildServiceProvider();
        }

        public static void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IAppLogger, AppLogger>();
            services.AddSingleton<LogWriter>();
            services.AddSingleton<ISettingsStore, AppSettingsStore>();
            services.AddSingleton<ISettingsSyncService, SettingsSyncService>();
            services.AddSingleton<ISettingsMigrationService, SettingsMigrationService>();
            services.AddSingleton<ISettingsResetService, SettingsResetService>();
            services.AddSingleton<IUiDispatcher, WpfUiDispatcher>();

            services.AddSingleton<IDirectDialogReader, HeuristicDirectDialogReader>();
            services.AddSingleton<IGameMemoryGateway, SharlayanGameMemoryGateway>();

            services.AddSingleton<ILoggerFactory>(_ =>
                LoggerFactory.Create(builder => builder.AddProvider(new QueueLoggerProvider())));
            services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
            services.AddSingleton<ILogger>(provider =>
                provider.GetRequiredService<ILoggerFactory>().CreateLogger("TataruHelper"));

            services.AddSingleton<ManagedRelayProcess>(_ => new ManagedRelayProcess(message => Logger.WriteLog(message)));
            services.AddSingleton<ITranslationCredentialStore>(provider => ConnectDeepLRelay(provider));
            services.AddSingleton<TranslationCredentialsViewModel>();
            services.AddSingleton<WebTranslator>(provider => new WebTranslator(
                provider.GetRequiredService<ILogger>(),
                provider.GetRequiredService<ITranslationCredentialStore>()));
            services.AddSingleton<IFFMemoryReaderService, FFMemoryReader>();
            services.AddSingleton<IReferenceIndexUpdateService, ReferenceIndexUpdateService>();

            services.AddSingleton<IHotKeyBindingService, HotKeyBindingService>();
            services.AddSingleton<IHotkeyCaptureService, HotkeyCaptureService>();
            services.AddSingleton<DialogueOverlayHost>();
            services.AddSingleton<GameIconReader>();
            services.AddSingleton<GameWorldReader>();
            services.AddSingleton<IChatWindowFactory, ChatWindowFactory>();
            services.AddTransient<IChatWindowCoordinator, ChatWindowCoordinator>();
            services.AddTransient<IChatWindowsEventCoordinator, ChatWindowsEventCoordinator>();
            services.AddTransient<ITranslationPipelineCoordinator, TranslationPipelineCoordinator>();
            services.AddTransient<IApplicationCoordinator, ApplicationCoordinator>();
            services.AddTransient<ITataruModelFactory, TataruModelFactory>();

            services.AddSingleton<IUpdateService>(provider =>
            {
                var logger = provider.GetRequiredService<IAppLogger>();
                try
                {
                    return (IUpdateService)new VelopackUpdateService(provider.GetRequiredService<ILogger>());
                }
                catch (Exception ex)
                {
                    logger.WriteLog("Failed to initialize Velopack updater. Updater will run in disabled mode.");
                    logger.WriteLog(ex);
                    return new DisabledUpdateService();
                }
            });
            services.AddSingleton<OptimizeFootprint>();
            services.AddSingleton<WinMessagesHandler>();
            services.AddSingleton(provider =>
                new LanguageWrapper(provider.GetRequiredService<ISettingsStore>().AppSettings));
            services.AddTransient<MainWindow>();
        }
    }
}
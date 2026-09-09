using System;
using System.Threading.Tasks;
using System.Windows;

using FFXIVTataruHelper;
using FFXIVTataruHelper.Services.Logging;
using FFXIVTataruHelper.Services.Settings;
using FFXIVTataruHelper.Services.UI;

using NUnit.Framework;

namespace TataruHelper.Tests.Models.Settings
{
    // A setting reaches the saved file through two hand-written copies, and
    // the copy constructor is the only one a walk covers. This one decides
    // whether the application helps itself to a gigabyte, so it is worth
    // knowing that turning it on survives a restart - and, more to the point,
    // that turning it off again does.
    [TestFixture]
    public class ReferenceIndexAutoInstallSettingTests
    {
        [Test]
        public void OffIsWhatAFreshInstallationGets()
        {
            // Downloading the export unprompted is not a default anybody
            // agreed to.
            Assert.That(new UserSettings().IsReferenceIndexAutoInstall, Is.False);
            Assert.That(CreateUiModel().IsReferenceIndexAutoInstall, Is.False);
        }

        /// <summary>
        /// The copy of the game's dialogue box draws over the game, which is
        /// not something to do to somebody who has not asked for it - and it
        /// arrived in an update, so most people who get it never chose it.
        /// </summary>
        [Test]
        public void TheCopyOfTheDialogueBoxIsOffUntilItIsChosen()
        {
            Assert.That(new UserSettings().IsDialogueOverlayShown, Is.False);
            Assert.That(CreateUiModel().IsDialogueOverlayShown, Is.False);

            // And an installation that predates the setting has nothing to say
            // about it, which has to read as off rather than as unset.
            var model = CreateUiModel();
            model.SetSettings(new UserSettings());

            Assert.That(model.IsDialogueOverlayShown, Is.False);
            Assert.That(model.GetSettings().IsDialogueOverlayShown, Is.False);
        }

        [Test]
        public void TheCopyOfTheDialogueBoxIsRememberedOnceItIsChosen()
        {
            var model = CreateUiModel();

            model.SetSettings(new UserSettings { IsDialogueOverlayShown = true });
            Assert.That(model.IsDialogueOverlayShown, Is.True);
            Assert.That(model.GetSettings().IsDialogueOverlayShown, Is.True);

            model.IsDialogueOverlayShown = false;
            Assert.That(model.GetSettings().IsDialogueOverlayShown, Is.False);
        }

        [Test]
        public void TurningItOnSurvivesTheRoundTripThroughSettings()
        {
            var model = CreateUiModel();

            model.SetSettings(new UserSettings { IsReferenceIndexAutoInstall = true });

            Assert.That(model.IsReferenceIndexAutoInstall, Is.True, "not read out of the saved settings");
            Assert.That(model.GetSettings().IsReferenceIndexAutoInstall, Is.True, "not written back into them");
        }

        [Test]
        public void TurningItOffAgainIsAlsoSaved()
        {
            // The failure that matters: a flag written only when true reads as
            // off next time and never as on, or reads as on and cannot be
            // turned off. Both look like the switch doing nothing.
            var model = CreateUiModel();
            model.SetSettings(new UserSettings { IsReferenceIndexAutoInstall = true });

            model.IsReferenceIndexAutoInstall = false;

            Assert.That(model.GetSettings().IsReferenceIndexAutoInstall, Is.False);
        }

        private static TataruUIModel CreateUiModel()
        {
            return new TataruUIModel(new FakeSettingsStore(), new ImmediateUiDispatcher(), new NullLogger());
        }

        private sealed class ImmediateUiDispatcher : IUiDispatcher
        {
            public bool IsInitialized => true;

            public Window CurrentWindow => null;

            public void SetWindow(Window window)
            {
            }

            public void Invoke(Action action)
            {
                action();
            }

            public Task InvokeAsync(Action action)
            {
                action();
                return Task.CompletedTask;
            }
        }

        private sealed class FakeSettingsStore : ISettingsStore
        {
            public AppSettings AppSettings { get; } = new AppSettings();

            public string ChatCodesFilePath => string.Empty;
            public string BlackListPath => string.Empty;
            public string IgnoreNickNameChatCodesPath => string.Empty;
            public string SystemSettingsPath => string.Empty;
            public string SettingsPath => string.Empty;
            public string OldSettingsPath => string.Empty;
            public int SettingsSaveDelayMs => 60_000;
            public int LookForProcessDelayMs => 1;
            public int MemoryReaderDelayMs => 1;
            public int AutoHideWatcherDelayMs => 1;
            public int TranslatorWaitTimeMs => 1;
            public int MaxTranslateTryCount => 1;
            public int MaxChatMessages => 500;

            public bool LoadGlobalSettings(string fileName)
            {
                return true;
            }

            public void SaveGlobalSettings(string fileName)
            {
            }
        }

        private sealed class NullLogger : IAppLogger
        {
            public void WriteLog(string input, string memberName = "", int sourceLineNumber = 0)
            {
            }

            public void WriteLog(object input, string memberName = "", int sourceLineNumber = 0)
            {
            }

            public void WriteConsoleLog(string input)
            {
            }

            public void WriteChatLog(string input)
            {
            }
        }
    }
}

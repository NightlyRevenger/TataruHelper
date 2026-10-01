using FFXIVTataruHelper.Utils;

using NUnit.Framework;

namespace TataruHelper.Tests.Utils
{
    /// <summary>
    /// The application wants administrator rights, and for a long time its
    /// manifest said so. That made the executable unstartable by anything that
    /// cannot elevate - the installer among them, which is how a reporter's
    /// log came to read
    ///
    ///   Failed to start hook --veloapp-install:
    ///   The requested operation requires elevation. (os error 740)
    ///
    /// So the asking moved here, where a copy that is not an administrator
    /// starts itself again through the shell.
    /// </summary>
    [TestFixture]
    public class StartupElevationTests
    {
        [Test]
        public void AnOrdinaryLaunchWithoutRights_AsksForThem()
        {
            Assert.That(StartupElevation.IsWanted(new string[0], false), Is.True);
        }

        [Test]
        public void ACopyThatAlreadyHasThem_AsksForNothing()
        {
            Assert.That(StartupElevation.IsWanted(new string[0], true), Is.False);
        }

        /// <summary>
        /// Building the index is a job rather than a copy of the application:
        /// the release script runs it and reads the exit code, and a copy that
        /// steps aside for an elevated child has no exit code to give. The
        /// script is elevated already, so there is nothing to ask for.
        /// </summary>
        [Test]
        public void BuildingTheIndex_IsAJobAndIsLeftAlone()
        {
            Assert.That(
                StartupElevation.IsWanted(new[] { "--build-reference-index" }, false),
                Is.False);
        }

        [Test]
        public void BuildingTheIndexFromAFolder_IsAJobToo()
        {
            Assert.That(
                StartupElevation.IsWanted(
                    new[] { "--build-reference-index", "--source", @"C:\export" }, false),
                Is.False);
        }

        /// <summary>
        /// Every other switch is an ordinary launch - a copy told to write the
        /// raw log is still the application.
        /// </summary>
        [Test]
        public void ASwitchThatIsNotAJob_StillAsks()
        {
            Assert.That(StartupElevation.IsWanted(new[] { "--log-raw-dialog" }, false), Is.True);
        }

        [Test]
        public void NoArgumentsAtAll_IsNoTrouble()
        {
            Assert.That(StartupElevation.IsWanted(null, false), Is.True);
            Assert.That(StartupElevation.IsWanted(null, true), Is.False);
        }
    }
}

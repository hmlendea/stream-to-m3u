using NUnit.Framework;

using StreamToM3U.Utils;

namespace StreamToM3U.UnitTests.Utils
{
    [TestFixture]
    public sealed class WebDriverHandlerTests
    {
        [SetUp]
        public void SetUp() => WebDriverHandler.ReleaseLock();

        [TearDown]
        public void TearDown() => WebDriverHandler.ReleaseLock();

        [Test]
        public static void GivenANewHandler_WhenReadingTheLockState_ThenTheLockIsFree()
            => Assert.That(WebDriverHandler.IsWebDriverLocked, Is.False);

        [Test]
        public void GivenAFreeHandler_WhenGainingTheLock_ThenTheLockIsTaken()
        {
            WebDriverHandler.GainLock();

            Assert.That(WebDriverHandler.IsWebDriverLocked);
        }

        [Test]
        public void GivenALockedHandler_WhenReleasingTheLock_ThenTheLockIsFree()
        {
            WebDriverHandler.GainLock();

            WebDriverHandler.ReleaseLock();

            Assert.That(WebDriverHandler.IsWebDriverLocked, Is.False);
        }
    }
}

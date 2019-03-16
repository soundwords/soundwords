using AutofacContrib.NSubstitute;
using NUnit.Framework;

namespace SoundWords.Tests
{
    [TestFixture]
    public class ServiceStackFixtures
    {
        public AutoSubstitute AutoSubstitute => Tests.Setup.AutoSubstitute;

        [SetUp]
        public void Setup()
        {
            MockingContainerAdapter.SetAutoSubstitute(Tests.Setup.AutoSubstitute);
        }
    }
}
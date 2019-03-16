using AutofacContrib.NSubstitute;
using NUnit.Framework;

namespace SoundWords.Tests
{
    [SetUpFixture]
    class Setup
    {
        public static string BaseUrl => "http://some.site";

        private static bool _initialized;

        [SetUp]
        public void GlobalSetup()
        {
            AppHost = new FakeAppHost(BaseUrl);
            AutoSubstitute = new AutoSubstitute();
            MockingContainerAdapter.SetAutoSubstitute(AutoSubstitute);

            if (!_initialized)
            {
                AppHost.Init();
                _initialized = true;
            }
        }

        public static FakeAppHost AppHost { get; private set; }

        public static AutoSubstitute AutoSubstitute { get; private set; }
    }
}

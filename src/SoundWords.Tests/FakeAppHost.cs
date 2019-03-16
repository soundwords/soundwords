using ServiceStack;
using ServiceStack.Configuration;

namespace SoundWords.Tests
{
    public class FakeAppHost : AppHostBase
    {
        private readonly string _baseUrl;
        private IContainerAdapter _containerAdapter;

        public FakeAppHost(string baseUrl)
            : base("SoundWordsFake", typeof(SoundWordsAppHost).GetAssembly())
        {
            TestMode = true;
            _baseUrl = baseUrl;
        }

        public override void Configure(Funq.Container container)
        {
            _containerAdapter = new MockingContainerAdapter();
            container.Adapter = _containerAdapter;
            
            SetConfig(new HostConfig
            {
                DebugMode = true,
                WebHostUrl = _baseUrl
            });
        }
    }
}
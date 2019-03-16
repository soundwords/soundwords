using System.Reflection;
using NSubstitute;
using NUnit.Framework;
using SoundWords.Tools;

namespace SoundWords.Tests.Tools
{
    [TestFixture]
    public class UrlExtensionsTests : ServiceStackFixtures
    {
        private ISoundWordsConfiguration _configuration;

        [SetUp]
        public void SetUp()
        {
            var propertyInfo = typeof(SoundWordsAppHost).GetTypeInfo().GetProperty("Configuration");
            _configuration = Substitute.For<ISoundWordsConfiguration>();
            propertyInfo.SetMethod.Invoke(null, new object[] {_configuration});
        }

        [TestCase("http://some.site/?a=1&b=2", "a", ExpectedResult = "http://some.site/?b=2")]
        [TestCase("http://some.site/abc?a=1&b=2", "a", ExpectedResult = "http://some.site/abc?b=2")]
        [TestCase("http://some.site/abc?a=1&b=2&c&d=3", "b", ExpectedResult = "http://some.site/abc?a=1&c&d=3")]
        [TestCase("http://some.site/abc?a=1&b=2&c&d=3", "e", ExpectedResult = "http://some.site/abc?a=1&b=2&c&d=3")]
        [TestCase("http://some.site/abc?_escaped_fragment_=Album%3DHymns%20for%20the%20Christian%20Life", "_escaped_fragment_", ExpectedResult = "http://some.site/abc")]
        public string RemoveQueryParameter_ReturnsCorrectValue(string url, string queryParameter)
        {
            return url.RemoveQueryParameter(queryParameter);
        }

        [TestCase("http://some.site/?a=1&b=2", "c", "3", ExpectedResult = "http://some.site/?a=1&b=2&c=3")]
        [TestCase("http://some.site/?a=1&b=2", "c", "3 4&5", ExpectedResult = "http://some.site/?a=1&b=2&c=3+4%265")]
        [TestCase("http://some.site/", "c", "3", ExpectedResult = "http://some.site/?c=3")]
        public string AddQueryParameter_ReturnsCorrectValue(string url, string queryParameter, string value)
        {
            return url.AddQueryParameter(queryParameter, value);
        }

        [TestCase("http://some.site?a=1&b=2", ExpectedResult = "http://some.site/?a=1&b=2")]
        [TestCase("/?a=1&b=2", ExpectedResult = "http://some.site/?a=1&b=2")]
        public string ToNormalizedUrl_AddsTrailingSlashWhenMissing(string url)
        {
            _configuration.Protocol.Returns("http");
            return url.ToNormalizedUrl();
        }
    }


}

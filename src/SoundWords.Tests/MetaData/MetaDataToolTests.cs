using System.Collections.Generic;
using System.Runtime.Serialization;
using FluentAssertions;
using NUnit.Framework;
using SoundWords.Social;

namespace SoundWords.Tests.MetaData
{
    [TestFixture]
    public class MetaDataToolTests
    {
        [Test]
        public void GetMetaData_Works()
        {
            MetaDataTool metaDataTool = new MetaDataTool();

            Dictionary<string, string> metaData = metaDataTool.GetMetaData(new MyMetaDataFake {A = "someA", B = "someB"});

            metaData.Keys.Count.Should().Be(2);
            metaData.Should().ContainKey("capitalA");
            metaData["capitalA"].Should().Be("someA");
            metaData.Should().ContainKey("capitalB");
            metaData["capitalB"].Should().Be("someB");
        }

        [DataContract]
        class MyMetaDataFake
        {
            [DataMember(Name = "capitalA")]
            public string A { get; set; }
            [DataMember(Name = "capitalB")]
            public string B { get; set; }
        }
    }
}

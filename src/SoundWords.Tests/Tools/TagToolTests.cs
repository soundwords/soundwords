using System;
using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
using System.Text;
using FluentAssertions;
using NUnit.Framework;
using SoundWords.Tools;
using TagLib;
using TagLib.Id3v2;

namespace SoundWords.Tests.Tools
{
    [TestFixture]
    public class TagToolTests
    {
        private MockFileSystem _mockFileSystem;
        private TagTool _tagTool;

        [SetUp]
        public void SetUp()
        {
            _mockFileSystem = new MockFileSystem();
            _tagTool = new TagTool(fileName => new FileAbstraction(_mockFileSystem, fileName));
        }

        [Test]
        public void WritePrivateFrames_InputIsValidated()
        {
            Action action = () => _tagTool.WritePrivateFrames(null, new Dictionary<string, string>());
            action.ShouldThrow<ArgumentNullException>().Where(x => x.ParamName == "path");
            Action action2 = () => _tagTool.WritePrivateFrames("something", null);
            action2.ShouldThrow<ArgumentNullException>().Where(x => x.ParamName == "values");
        }

        [Test]
        public void WritePrivateFrames_TagsAreWritten()
        {
            const string fileName = @"C:\test.mp3";
            _mockFileSystem.AddFile(fileName, new MockFileData(System.IO.File.ReadAllBytes(@"TestData\test.mp3")));

            File.IFileAbstraction mp3File = _mockFileSystem.File.CreateFileAbstraction(fileName);

            Dictionary<string, string> values = new Dictionary<string, string> {{"SW/Uid", Guid.NewGuid().ToString("N")}};

            _tagTool.WritePrivateFrames(fileName, values);

            File actualFile = File.Create(mp3File);

            TagLib.Id3v2.Tag id3V2Tag = (TagLib.Id3v2.Tag) actualFile.GetTag(TagTypes.Id3v2, true);
            PrivateFrame actualFrame = PrivateFrame.Get(id3V2Tag, "SW/Uid", true);

            string actualUid = Encoding.Unicode.GetString(actualFrame.PrivateData.Data);

            actualUid.Should().Be(values["SW/Uid"]);
        }

        [Test]
        public void ReadPrivateFrames_AllTagsAreRead()
        {
            const string fileName = @"C:\test.mp3";
            _mockFileSystem.AddFile(fileName, new MockFileData(System.IO.File.ReadAllBytes(@"TestData\test.mp3")));

            Dictionary<string, string> values = new Dictionary<string, string>
            {
                { "SW/Uid", Guid.NewGuid().ToString("N") },
                { "SW/AlbumUid", Guid.NewGuid().ToString("N") }
            };

            _tagTool.WritePrivateFrames(fileName, values);

            Dictionary<string, string> actualValues = _tagTool.ReadPrivateFrames(fileName, values.Keys);

            actualValues.ShouldBeEquivalentTo(values);
        }

        [Test]
        public void ReadPrivateFrames_NotExistingTag_ReturnsNull()
        {
            const string fileName = @"C:\test.mp3";
            _mockFileSystem.AddFile(fileName, new MockFileData(System.IO.File.ReadAllBytes(@"TestData\test.mp3")));

            
            Dictionary<string, string> actualValues = _tagTool.ReadPrivateFrames(fileName, new[] { "SW/Foo"});

            actualValues.ShouldBeEquivalentTo(new Dictionary<string, string> {{"SW/Foo", null}});
        }
    }
}

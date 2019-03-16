using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Text;
using FluentAssertions;
using NUnit.Framework;
using SoundWords.Tools;
using TagLib;
using TagLib.Flac;
using TagLib.Id3v2;
using TagLib.Ogg;
using File = TagLib.File;
using Tag = TagLib.Id3v2.Tag;

namespace SoundWords.Tests
{
    [TestFixture]
    public class TagLibTests
    {
        private MockFileSystem _mockFileSystem;

        [SetUp]
        public void SetUp()
        {
            _mockFileSystem = new MockFileSystem();
        }

        [Test, Explicit]
        public void TestTagLib()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            string[] strings = Directory.GetFiles(@"D:\Music\Privat\Rune Vidar Fjelde\Sakkeus & di").Where(x => Path.GetExtension(x) == ".flac").ToArray();

            List<TagLib.Tag> list = strings.Select(s => File.Create(CreateAbstraction(s))).Select(file => file.Tag).ToList();

            stopwatch.Stop();
            long elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
        }

        private FileAbstraction CreateAbstraction(string fileName)
        {
            return new FileAbstraction(_mockFileSystem, fileName);
        }

        [Test, Explicit]
        public void TestFlacUid()
        {
            var flacFile = string.Format("{0}\\{1:N}.flac", Path.GetTempPath(), Guid.NewGuid());
            System.IO.File.Copy(@"TestData\test.flac", flacFile, true);

            var file = File.Create(CreateAbstraction(flacFile));

            Metadata metadata = (Metadata) file.GetTag(TagTypes.FlacMetadata, true);

            XiphComment xiphComment = (XiphComment) metadata.Tags.First();
            string uid = xiphComment.GetFirstField("UID");
            uid.Should().Be("SomewhereOverTheRainbow");
            //metadata.SetTextFrame((ReadOnlyByteVector) "UFID", "http://www.id3.org/dummy/ufid.html", Guid.NewGuid().ToString("N"));
                
            //file.Save();

            //File actualFile = File.Create(mp3File);

            //metadata = (Tag)actualFile.GetTag(TagTypes.Id3v2, true);
            //// Get the private frame, create if necessary.
            //var frame = metadata.GetFrames().FirstOrDefault(f => f.FrameId == "UFID");
            //frame.Should().NotBeNull();
           
            System.IO.File.Delete(flacFile);
       }

        [Test, Explicit]
        public void TestWriteFlacUid()
        {
            var flacFile = string.Format("{0}\\{1:N}.flac", Path.GetTempPath(), Guid.NewGuid());
            System.IO.File.Copy(@"TestData\hei.flac", flacFile, true);

            var file = File.Create(flacFile);

            Metadata metadata = (Metadata)file.GetTag(TagTypes.FlacMetadata, true);

            XiphComment xiphComment = (XiphComment)metadata.Tags.First();
            xiphComment.SetField("SW-AlbumUid", Guid.NewGuid().ToString("N"));
            file.Save();
            //metadata.SetTextFrame((ReadOnlyByteVector) "UFID", "http://www.id3.org/dummy/ufid.html", Guid.NewGuid().ToString("N"));

            //file.Save();

            //File actualFile = File.Create(mp3File);

            //metadata = (Tag)actualFile.GetTag(TagTypes.Id3v2, true);
            //// Get the private frame, create if necessary.
            //var frame = metadata.GetFrames().FirstOrDefault(f => f.FrameId == "UFID");
            //frame.Should().NotBeNull();

            System.IO.File.Delete(flacFile);
        }

        [Test, Explicit]
        public void TestMp3Uid()
        {
            var mp3File = string.Format("{0}\\{1:N}.mp3", Path.GetTempPath(), Guid.NewGuid());
            System.IO.File.Copy(@"TestData\test.mp3", mp3File, true);

            var file = File.Create(CreateAbstraction(mp3File));

            Tag id3V2Tag = (Tag)file.GetTag(TagTypes.Id3v2, true);
            var userTextInformationFrames = id3V2Tag.GetFrames<UserTextInformationFrame>();
            UserTextInformationFrame frame = userTextInformationFrames.First(a => a.Description == "UID");
            frame.Text.First().Should().Be("SomewhereOverTheRainbow");
            frame.Text = new[] {"Hei"};
            var userTextInformationFrame = new UserTextInformationFrame("WhateverUID")
            {
                Text = new[] {Guid.NewGuid().ToString("N")}
            };
            id3V2Tag.AddFrame(userTextInformationFrame);
            file.Save();
            System.IO.File.Delete(mp3File);
        }

        [Test]
        public void TestPrivateFrame()
        {
            MockFileSystem fileSystem = new MockFileSystem();
            fileSystem.AddFile(@"C:\test.mp3", new MockFileData(System.IO.File.ReadAllBytes(@"TestData\test.mp3")));

            File.IFileAbstraction mp3File = fileSystem.File.CreateFileAbstraction(@"C:\test.mp3");
            File file = File.Create(mp3File);

            Tag id3V2Tag = (Tag) file.GetTag(TagTypes.Id3v2, true);

            // Get the private frame, create if necessary.
            PrivateFrame frame = PrivateFrame.Get(id3V2Tag, "SW/Uid", true);

            string uid = Guid.NewGuid().ToString("N");
            frame.PrivateData = Encoding.Unicode.GetBytes(uid);

            file.Save();


            File actualFile = File.Create(mp3File);

            id3V2Tag = (Tag) actualFile.GetTag(TagTypes.Id3v2, true);
            // Get the private frame, create if necessary.
            PrivateFrame actualFrame = PrivateFrame.Get(id3V2Tag, "SW/Uid", true);

            // Set the frame data to your value.  I am 90% sure that these are encoded with UTF-16.
            string actualUid = Encoding.Unicode.GetString(actualFrame.PrivateData.Data);

            actualUid.Should().Be(uid);
        }
    }

    static class MockFileAbstractionExtensions
    {
        public static File.IFileAbstraction CreateFileAbstraction(this FileBase fileBase, string path)
        {
            return new MockFileAbstraction(fileBase, path);
        }

        private class MockFileAbstraction : File.IFileAbstraction
        {
            private readonly FileBase _fileBase;

            public MockFileAbstraction(FileBase fileBase, string path)
            {
                _fileBase = fileBase;
                Name = path;
            }

            public void CloseStream(Stream stream)
            {
                if (stream == null) throw new ArgumentNullException(nameof(stream));

                stream.Dispose();
            }

            public string Name { get; }

            public Stream ReadStream => _fileBase.Open(Name, FileMode.Open, FileAccess.Read, FileShare.Read);

            public Stream WriteStream => _fileBase.Open(Name, FileMode.Open, FileAccess.ReadWrite);
        }
    }
}

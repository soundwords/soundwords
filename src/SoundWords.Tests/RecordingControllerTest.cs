//using System;
//using System.Collections.Generic;
//using System.Configuration;
//using System.IO;
//using System.Linq;
//using System.Web.Mvc;
//using Moq;
//using NUnit.Framework;
//using ServiceStack.Logging;
//using SoundWords.Controllers;
//using SoundWords.Models;

//namespace SoundWords.Tests
//{
//    [TestFixture]
//    public class RecordingControllerTest
//    {
//        private RecordingController _recordingController;
//        private Mock<IRecordingRepository> _recordingRepositoryMock;
//        private const string TestMp3FileName = @"TestData\test.mp3";

//        [SetUp]
//        public void SetUp()
//        {
//            _recordingRepositoryMock = new Mock<IRecordingRepository>();
//            Mock<ILog> loggerMock = new Mock<ILog>();
//            _recordingController = new RecordingController(_recordingRepositoryMock.Object, new MimeTypes(), loggerMock.Object);
//        }

//        [Test]
//        public void Rebuild_InvalidInput_Throws()
//        {
//            ConfigurationManager.AppSettings.Remove("RECORDINGS_FOLDER");
//            Assert.That(() => _recordingController.Rebuild(), Throws.TypeOf<ArgumentNullException>());
//        }

//        [Test]
//        public void Rebuild_NewFolder_DeletesExisting()
//        {
//            List<Recording> recordings = new List<Recording>
//                                     {
//                                         new Recording {RecordingId = 1},
//                                         new Recording {RecordingId = 2}
//                                     };
//            _recordingRepositoryMock.Setup(x => x.GetAllRecordings(It.IsAny<bool>())).Returns(recordings.AsQueryable());

//            string tempPath = Path.GetTempPath();
//            DirectoryInfo folder = Directory.CreateDirectory(Path.Combine(tempPath, Guid.NewGuid().ToString("N")));
//            ConfigurationManager.AppSettings.Set("RECORDINGS_FOLDER", folder.FullName);

//            _recordingController.Rebuild();

//            _recordingRepositoryMock.Verify(x => x.GetAllRecordings(It.IsAny<bool>()), Times.Exactly(1));
//            _recordingRepositoryMock.Verify(x => x.Delete(recordings[0].RecordingId), Times.Exactly(1));
//            _recordingRepositoryMock.Verify(x => x.Delete(recordings[1].RecordingId), Times.Exactly(1));
//            _recordingRepositoryMock.Verify(x => x.Save(), Times.AtLeast(1));
//        }

//        [Test]
//        public void Rebuild_FolderWithTwoMP3s_CorrectRecordsInBase()
//        {
//            string tempPath = Path.GetTempPath();
//            DirectoryInfo folder = Directory.CreateDirectory(Path.Combine(tempPath, Guid.NewGuid().ToString("N")));
//            ConfigurationManager.AppSettings.Set("RECORDINGS_FOLDER", folder.FullName);

//            string testFile1 = Path.Combine(folder.FullName, "test1.mp3");
//            File.Copy(TestMp3FileName, testFile1);
//            TagLib.File testFile1Tags = TagLib.File.Create(testFile1);
//            testFile1Tags.Tag.Performers = new[] { "Test1" };
//            testFile1Tags.Save();

//            string testFile2 = Path.Combine(folder.FullName, "test2.mp3");
//            File.Copy(TestMp3FileName, testFile2);
//            TagLib.File testFile2Tags = TagLib.File.Create(testFile2);
//            testFile2Tags.Tag.Performers = new[] { "Test2" };
//            testFile2Tags.Save();

//            _recordingRepositoryMock.Setup(x => x.GetAllRecordings(It.IsAny<bool>())).Returns(new List<Recording>().AsQueryable());

//            _recordingController.Rebuild();

//            _recordingRepositoryMock.Verify(x => x.Add(It.Is<Recording>(r => r.Speaker == testFile1Tags.Tag.FirstPerformer)), Times.Exactly(1));
//            _recordingRepositoryMock.Verify(x => x.Add(It.Is<Recording>(r => r.Speaker == testFile2Tags.Tag.FirstPerformer)), Times.Exactly(1));
//            _recordingRepositoryMock.Verify(x => x.Save(), Times.AtLeast(1));
//        }

//        [Test]
//        public void Stream_InvalidInput_Throws()
//        {
//            Assert.That(() => _recordingController.Stream(0), Throws.TypeOf<ArgumentOutOfRangeException>());
//            Assert.That(() => _recordingController.Stream(-1), Throws.TypeOf<ArgumentOutOfRangeException>());
//        }

//        [Test]
//        public void Stream_WithValidId_ReturnsStream()
//        {
//            string mp3Path = Path.GetFullPath(TestMp3FileName);
//            int id = 1;

//            _recordingRepositoryMock.Setup(x => x.GetById(id)).Returns(new Recording
//                                                                          {
//                                                                              Path = mp3Path
//                                                                          });

//            FilePathResult fileResult = _recordingController.Stream(id) as FilePathResult;
//            Assert.That(fileResult, Is.Not.Null, "The ActionResult should be a FileResult");
//            // ReSharper disable PossibleNullReferenceException
//            Assert.That(fileResult.ContentType, Is.EqualTo("audio/mpeg"), "The ContentType should be audio/mpeg");
//            // ReSharper restore PossibleNullReferenceException
//        }
//    }
//}

using System;
using System.IO;
using System.IO.Abstractions;
using ServiceStack;
using File = TagLib.File;

namespace SoundWords.Tools
{
    public class FileAbstraction : File.IFileAbstraction
    {
        private readonly Stream _stream;

        public FileAbstraction(IFileSystem fileSystem, string path, bool writable)
        {
            _stream = writable ? fileSystem.File.Open(path, FileMode.Open, FileAccess.ReadWrite) : fileSystem.File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            Name = path;
        }

        public void CloseStream(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException("stream");

            stream.Close();
        }

        public string Name { get; }

        public Stream ReadStream => _stream;

        public Stream WriteStream => _stream;
    }
}
﻿using System;
using System.IO.Abstractions;

namespace SoundWords.Tools
{
    public class PageTool : IPageTool
    {
        private readonly ISoundWordsConfiguration _soundWordsConfiguration;
        private readonly IFileSystem _fileSystem;

        public PageTool(ISoundWordsConfiguration soundWordsConfiguration, IFileSystem fileSystem)
        {
            _soundWordsConfiguration = soundWordsConfiguration;
            _fileSystem = fileSystem;
        }
        public string Get(string key)
        {
            if (key == null) throw new NullReferenceException(nameof(key));

            string path = _fileSystem.Path.Combine(_soundWordsConfiguration.CustomFolder, "Pages", $"{key}.md");
            return _fileSystem.File.Exists(path) ? _fileSystem.File.ReadAllText(path) : null;
        }
    }
}

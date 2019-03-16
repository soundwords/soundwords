using System.Collections.Generic;

namespace SoundWords.Models
{
    public class Album : Entity
    {
        public string Uid { get; set; }
        public string Name { get; set; }
        public string ProductNo { get; set; }
        public string MasterNo { get; set; }
        public string StorageNo { get; set; }
        public string Occasion { get; set; }
        public ushort? Day { get; set; }
        public ushort? Month { get; set; }
        public ushort? Year { get; set; }
        public string Place { get; set; }
        public string Comment { get; set; }
        public string Path { get; set; }
        public string AlbumArtPath { get; set; }
        public string MarkdownPath { get; set; }
        public List<string> AttachmentPaths { get; set; }
        public bool Restricted { get; set; }
    }
}

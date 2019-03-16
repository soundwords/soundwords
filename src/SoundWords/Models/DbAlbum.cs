using System.Collections.Generic;

namespace SoundWords.Models
{
    public class DbAlbum : DbEntity
    {
        public string Uid { get; set; }
        public string Name { get; set; }
        public string ProductNo { get; set; }
        public string MasterNo { get; set; }
        public string StorageNo { get; set; }
        public string Occasion { get; set; }
        public string Place { get; set; }
        public string Comment { get; set; }
        public string Path { get; set; }
        public string Description { get; set; }
        public string AlbumArtPath { get; set; }
        public List<string> AttachmentPaths { get; set; }
        public bool Restricted { get; set; }
    }
}

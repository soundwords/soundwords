using ServiceStack.DataAnnotations;

namespace SoundWords.Models
{
    public class DbRecording : DbEntity
    {
        public string Uid { get; set; }

        [References(typeof(DbAlbum))]
        public long AlbumId { get; set; }
        
        public string Title { get; set; }
        public ushort Track { get; set; }
        public string Comment { get; set; }
        public ushort? Day { get; set; }
        public ushort? Month { get; set; }
        public ushort? Year { get; set; }
        public string Path { get; set; }
        public bool Restricted { get; set; }
    }
}

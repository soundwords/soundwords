namespace SoundWords.Models
{
    public class Recording : Entity
    {
        public string Uid { get; set; }
        public long AlbumId { get; set; }
        public string Title { get; set; }
        public short Track { get; set; }
        public string Comment { get; set; }
        public ushort? Day { get; set; }
        public ushort? Month { get; set; }
        public ushort? Year { get; set; }
        public string Path { get; set; }
        public bool Restricted { get; set; }
    }
}

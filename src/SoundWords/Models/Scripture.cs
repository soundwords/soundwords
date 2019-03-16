namespace SoundWords.Models
{
    internal class Scripture : Entity
    {
        public long RecordingId { get; set; }
        public Book Book { get; set; }
        public ushort? FromChapter { get; set; }
        public ushort? FromVerse { get; set; }
        public ushort? ToChapter { get; set; }
        public ushort? ToVerse { get; set; }
    }
}

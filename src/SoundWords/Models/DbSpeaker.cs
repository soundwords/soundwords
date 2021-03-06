namespace SoundWords.Models
{
    internal class DbSpeaker : DbEntity
    {
        public string Uid { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public ushort? BirthDay { get; set; }
        public ushort? BirthMonth { get; set; }
        public ushort? BirthYear { get; set; }
        public string Nationality { get; set; }
        public string Description { get; set; }
        public string PhotoPath { get; set; }
    }
}

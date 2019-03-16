namespace SoundWords.Models
{
    public class User : Entity
    {
        public string Email { get; set; }
        public string UserName { get; set; }
        public string DisplayName { get; set; }
        public string GravatarImageUrl64 { get; set; }
    }
}
using ServiceStack;

namespace SoundWords.Services
{
    public abstract class ServiceBase : Service
    {
        public CustomUserSession UserSession
        {
            get { return GetSession() as CustomUserSession; }
        }
    }

    public class CustomUserSession : AuthUserSession
    {
    }
}
using ServiceStack;

namespace SoundWords.Services
{
    public abstract class ServiceBase : Service
    {
        protected CustomUserSession UserSession => GetSession() as CustomUserSession;
        protected ISoundWordsConfiguration Configuration => ResolveService<ISoundWordsConfiguration>();
    }

    public class CustomUserSession : AuthUserSession
    {
    }
}
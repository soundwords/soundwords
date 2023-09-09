using ServiceStack;
using ServiceStack.Auth;

namespace SoundWords.Services
{
    public abstract class ServiceBase : Service
    {
        protected IAuthSession UserSession => GetSession();
        protected ISoundWordsConfiguration Configuration => ResolveService<ISoundWordsConfiguration>();
    }
}
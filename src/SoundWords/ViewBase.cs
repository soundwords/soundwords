using ServiceStack.Mvc;
using SoundWords.Services;

namespace SoundWords
{
    public abstract class ViewBase : ViewBase<object>
    {
    }

    public abstract class ViewBase<T> : ViewPage<T>
    {
        protected CustomUserSession UserSession => GetSession() as CustomUserSession;
        protected ISoundWordsConfiguration Configuration => ResolveService<ISoundWordsConfiguration>();
    }
}

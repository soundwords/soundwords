using ServiceStack.Mvc;

namespace SoundWords
{
    public abstract class ViewBase : ViewBase<object>
    {
    }

    public abstract class ViewBase<T> : ViewPage<T>
    {
        protected ISoundWordsConfiguration Configuration => ResolveService<ISoundWordsConfiguration>();
    }
}

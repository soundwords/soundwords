using Autofac;
using ServiceStack.Configuration;

namespace SoundWords
{
    public class AutofacIocAdapter : IContainerAdapter
    {
        private readonly IComponentContext _componentContext;

        public AutofacIocAdapter(IComponentContext componentContext)
        {
            _componentContext = componentContext;
        }

        public T Resolve<T>()
        {
            return _componentContext.Resolve<T>();
        }

        public T TryResolve<T>()
        {
            T result;

            if (_componentContext.TryResolve(out result))
            {
                return result;
            }

            return default(T);
        }
    }
}
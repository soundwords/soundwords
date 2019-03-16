using AutofacContrib.NSubstitute;
using ServiceStack.Configuration;

namespace SoundWords.Tests
{
    public class MockingContainerAdapter : IContainerAdapter
    {
        private static AutoSubstitute _autoSubstitute;

        public T TryResolve<T>()
        {
            return _autoSubstitute != null ? _autoSubstitute.Resolve<T>() : default(T);
        }

        public T Resolve<T>()
        {
            return TryResolve<T>();
        }

        public static void SetAutoSubstitute(AutoSubstitute autoSubstitute)
        {
            _autoSubstitute = autoSubstitute;
        }
    }
}
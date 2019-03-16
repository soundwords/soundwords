using FluentMigrator.Runner.Announcers;
using ServiceStack.Logging;

namespace SoundWords.Tools
{
    public class ServiceStackLoggingAnnouncer : Announcer
    {
        private readonly ILog _logger;

        public ServiceStackLoggingAnnouncer(ILogFactory logFactory)
        {
            _logger = logFactory.GetLogger(GetType());
        }

        public override void Write(string message, bool escaped)
        {
            _logger.Debug(message);
        }
    }
}

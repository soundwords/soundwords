using ServiceStack;
using ServiceStack.Logging;
using ServiceStack.Web;

namespace SoundWords.Social
{
    public class EscapedFragmentAttribute : RequestFilterAttribute
    {
        private static ILog _logger;

        private static ILog Logger
        {
            get { return _logger ?? (_logger =LogManager.GetLogger(typeof(EscapedFragmentAttribute))); }
        }

        public override void Execute(IRequest req, IResponse res, object requestDto)
        {
            string escapedFragment = req.QueryString["_escaped_fragment_"].UrlDecode();
            if (escapedFragment == null) return;
            Logger.DebugFormat("A robot is here! User Agent={0} Url={1}", req.UserAgent, req.RawUrl);
            IHaveEscapedFragment escapedFragmentDto = requestDto as IHaveEscapedFragment;
            if (escapedFragmentDto == null) return;

            escapedFragmentDto.EscapedFragment = escapedFragment;
        }
    }
}
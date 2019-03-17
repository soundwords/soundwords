using Microsoft.AspNetCore.Mvc;
using ServiceStack;
using ServiceStack.Web;

namespace SoundWords.ViewComponents
{
    public abstract class ViewComponentBase : ViewComponent
    {
        private IServiceStackProvider _provider;

        protected virtual IServiceStackProvider ServiceStackProvider => _provider ?? (_provider = (IServiceStackProvider)new ServiceStackProvider(GetRequest()));

        public IHttpRequest GetRequest()
        {
            if (ViewContext.ViewData.TryGetValue(Keywords.IRequest, out var requestObject))
                return (IHttpRequest) requestObject;

            return AppHostBase.GetOrCreateRequest(HttpContext) as IHttpRequest;
        }

        public virtual bool IsAuthenticated => ServiceStackProvider.IsAuthenticated;
    }
}

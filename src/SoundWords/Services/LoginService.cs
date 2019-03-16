#define UseEmbeddedHtmlForm

using ServiceStack;
using Service = ServiceStack.Service;

namespace SoundWords.Services
{
    public class LoginService : Service
    {
        [Route("/Login")]
        public class LoginRequest
        {
            public string Redirect { get; set; }
        }

#if UseEmbeddedHtmlForm
        const string HtmlBody = @"<!DOCTYPE html>
<html lang=""en"">
<head><title>Login Page</title></head>
<style>body {{ font: 16px/24px Arial; }}</style>
<body>
    <h3>Login Page</h3>
    <p>
        Using the Embedded HTML Form in the <a href='https://github.com/ServiceStack/SocialBootstrapApi/blob/master/src/SocialBootstrapApi/ServiceInterface/LoginService.cs'>/login</a> service.
    </p>
    <p>
        Autentication is required to view: <b>{0}</b>
    </p>
    <form action='/auth/credentials' method='POST'>
        <input type='hidden' name='Continue' value='{0}' />
        <dl>
            <dt>User Name:</dt>
            <dd><input type='text' name='UserName' /></dd>
            <dt>Password:</dt>
            <dd><input type='password' name='Password' /></dd>
        </dl>             
        <input type='submit' value='Sign In' />
    </form>
</body>
</html>";

        [AddHeader(ContentType = "text/html")]
        public object Any(LoginRequest request)
        {
            return HtmlBody.Fmt(request.Redirect);
        }
#else
        //
        public object Any(Login request)
        {
            return HttpResult.Redirect("/");
        }
#endif
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack;
using ServiceStack.Web;

namespace SoundWords.Tools
{
    public static class UrlExtensions
    {
        public static string RemoveQueryParameter(this string url, string queryParameter)
        {
            UriBuilder uriBuilder = new UriBuilder(url);

            string query = uriBuilder.Query;
            if (string.IsNullOrWhiteSpace(query)) return url;

            query = query[0] == '?' ? query.Substring(1) : query;
            IEnumerable<string> parametersToKeep = from p in query.Split('&')
                                                   where !string.IsNullOrWhiteSpace(p)
                                                   let paramAndValue = p.Split('=')
                                                   where !paramAndValue[0].Equals(queryParameter, StringComparison.OrdinalIgnoreCase)
                                                   select string.Join("=", paramAndValue);

            uriBuilder.Query = string.Join("&", parametersToKeep);

            return uriBuilder.Uri.AbsoluteUri;
        }

        public static string AddQueryParameter(this string url, string queryParameter, string value)
        {
            UriBuilder uriBuilder = new UriBuilder(url);

            string query = uriBuilder.Query;
            
            query = (query.Length > 0 && query[0] == '?') ? query.Substring(1) : query;
            Dictionary<string, string> parameters = (from p in query.Split('&')
                                                     where !string.IsNullOrWhiteSpace(p)
                                                     let paramAndValue = p.Split('=')
                                                     select new
                                                     {
                                                         Key = paramAndValue[0],
                                                         Value = paramAndValue[1]
                                                     }).ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);
            parameters.Add(queryParameter, value.UrlEncode20());

            uriBuilder.Query = string.Join("&", parameters.Select(x => string.Format("{0}={1}", x.Key, x.Value)));

            return uriBuilder.Uri.AbsoluteUri;
        }

        public static string SetHashBangPath(this string url, string path)
        {
            UriBuilder uriBuilder = new UriBuilder(url);
            if (!uriBuilder.Path.EndsWith("/")) uriBuilder.Path += "/";
            uriBuilder.Fragment = string.Format("!{0}", path);
            return uriBuilder.Uri.AbsoluteUri;
        }

        public static bool IsNormalizedUrl(this string rawUrl)
        {
            string[] urlParts = rawUrl.Split('?');
            return urlParts[0].EndsWith("/");
        }

        public static string ToNormalizedUrl(this string url)
        {
            if (string.IsNullOrEmpty(url)) return "/".ToAbsoluteUri();
            if (url.StartsWith("/")) url = url.ToAbsoluteUri();

            string[] urlParts = url.Split('?');
            StringBuilder urlStringBuilder = new StringBuilder();
            urlStringBuilder.Append(urlParts[0].WithTrailingSlash());
            if (urlParts.Length > 1)
            {
                urlStringBuilder.Append("?");
                urlStringBuilder.Append(string.Join("?", urlParts.Skip(1)));
            }
            return urlStringBuilder.Replace("http://", GetFullProtocol()).ToString();
        }

        private static string GetFullProtocol()
        {
            return string.Format("{0}://", SoundWordsAppHost.Configuration.Protocol);
        }

        public static string SetFragment(this string url, string fragment)
        {
            if (string.IsNullOrEmpty(url)) return url;

            url = url.RemoveQueryParameter("_escaped_fragment_");

            IEnumerable<string> urlEncodedFragmentParts = fragment.Split('/').Select(p => p.UrlEncode20());
            return string.Format("{0}#!{1}", url.Split('#')[0], string.Join("/", urlEncodedFragmentParts));
        }

        public static string UrlEncode20(this string url)
        {
            return url.UrlEncode().Replace("+", "%20");
        }

        public static string ToFullUrl(this IRequest request)
        {
            return request.ToFullUrl(request.Dto);
        }

        public static string ToFullUrl(this IRequest request, object requestDto)
        {
            return $"{request.GetApplicationUrl()}{requestDto.ToUrl()}/".Replace("http://", GetFullProtocol());
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Principal;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Guardo;

namespace Softengi.UbClient.Sessions
{
    internal class UbTransport
	{
		internal UbTransport(Uri uri)
		{
			_uri = uri;
		}

	    internal T Get<T>(string appMethod, Dictionary<string, string> queryStringParams,
	        Dictionary<string, string> requestHeaders, bool base64Response = false, bool sendCredentials = false)
	    {
	        return GetAsync<T>(appMethod, queryStringParams, requestHeaders, base64Response, sendCredentials).Result;
	    }

        internal async Task<T> GetAsync<T>(string appMethod, Dictionary<string, string> queryStringParams,
			Dictionary<string, string> requestHeaders, bool base64Response = false, bool sendCredentials = false)
		{
			var result = await GetAsync(appMethod, queryStringParams, requestHeaders, base64Response, sendCredentials);
			return JsonConvert.DeserializeObject<T>(result);
		}

		internal async Task<string> GetAsync(string appMethod, Dictionary<string, string> queryStringParams,
			Dictionary<string, string> requestHeaders, bool base64Response = false, bool sendCredentials = false)
		{
			return await RequestAsync("GET", appMethod, queryStringParams, requestHeaders, null, base64Response, sendCredentials);
		}

	    internal string Request(string httpMethod, string appMethod,
	        Dictionary<string, string> queryStringParams,
	        Dictionary<string, string> requestHeaders, Stream data, bool base64Response = false,
	        bool sendCredentials = false)
	    {
	        return RequestAsync(httpMethod, appMethod, queryStringParams, requestHeaders, data, base64Response,
	            sendCredentials).Result;

	    }

        internal async Task<string> RequestAsync(string httpMethod, string appMethod, Dictionary<string, string> queryStringParams,
			Dictionary<string, string> requestHeaders, Stream data, bool base64Response = false, bool sendCredentials = false)
		{
			if (httpMethod != "GET" && httpMethod != "POST")
				throw new UbClientException($"HTTP method '{httpMethod}' is not supported.");
			Requires.NotNullOrEmpty(nameof(httpMethod), httpMethod);

			var uri = BuildUri(_uri, appMethod, queryStringParams);
			var request = WebRequest.Create(uri);
			request.Method = httpMethod;

			if (sendCredentials)
			{
				request.Credentials = CredentialCache.DefaultNetworkCredentials;
				// request.ImpersonationLevel = TokenImpersonationLevel.Delegation;
			}

			if (requestHeaders != null)
				foreach (var p in requestHeaders)
					request.Headers[p.Key] = p.Value;

			if (data != null && data.Length > 0)
				using (var requestStream = await request.GetRequestStreamAsync())
					data.CopyTo(requestStream);

			using (var response = await request.GetResponseAsync())
			{
				using (var responseStream = response.GetResponseStream())
				{
					if (responseStream == null)
						return null;

					if (base64Response)
						using (var ms = new MemoryStream())
						{
							responseStream.CopyTo(ms);
							return Convert.ToBase64String(ms.ToArray());
						}

					using (var reader = new StreamReader(responseStream))
						return reader.ReadToEnd();
				}
			}
		}

		static internal Uri BuildUri(Uri baseUri, string relativeUri, Dictionary<string, string> queryStringParams)
		{
			return new Uri(baseUri, AppendQueryParamsToUri(relativeUri, queryStringParams));
		}

		static private string AppendQueryParamsToUri(string uri, Dictionary<string, string> queryStringParams)
		{
			return queryStringParams != null
				? uri + "?" + QueryParamsToString(queryStringParams)
				: uri;
		}

		static private string QueryParamsToString(Dictionary<string, string> queryStringParams)
		{
		    var urlEncoder = UrlEncoder.Create();

            return queryStringParams
				.Select(p => urlEncoder.Encode(p.Key) + "=" + urlEncoder.Encode(p.Value))
				.Aggregate((current, next) => current + "&" + next);
		}

		private readonly Uri _uri;
	}
}
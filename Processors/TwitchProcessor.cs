using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace StreamToM3U.Processors
{
    public sealed class TwitchProcessor : ITwitchProcessor
    {
        static string TwitchApiUrl => "https://api.twitch.tv/api";
        static string TwitchApiChannelsUrl => $"{TwitchApiUrl}/channels";
        static string PlaylistUrlFormat =>
            "http://usher.twitch.tv/api/channel/hls/{0}.m3u8?player=twitchweb&token={1}&sig={2}";

        const string TokenPattern = "\"token\": *\"({.*})\",";
        const string SignaturePattern = "\"sig\": *\"([a-z0-9]*)\"";

        public string GetPlaylistUrl(string channelId)
        {
            string endpoint = $"{TwitchApiChannelsUrl}/{channelId}/access_token/";

            UriBuilder uriBuilder = new UriBuilder(endpoint);
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(uriBuilder.Uri);

            request.Headers["Client-ID"] = "jzkbprff40iqj646a697cyrvl0zt2m6";
            request.Headers["Host"] = "api.twitch.tv";
            request.Headers["User-Agent"] = "[{\"key\":\"User-Agent\",\"value\":\"Mozilla/5.0 (Windows NT 6.3; rv:43.0) Gecko/20100101 Firefox/43.0 Seamonkey/2.40\",\"description\":\"\",\"type\":\"text\",\"enabled\":true}]";
            request.Headers["Accept"] = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            if (response.StatusCode != HttpStatusCode.OK)
            {
                return null;
            }

            using (StreamReader reader = new StreamReader(
                response.GetResponseStream(),
                ASCIIEncoding.ASCII))
            {
                string body = reader.ReadToEnd();

                string token = Regex.Match(body, TokenPattern).Groups[1].Value;
                string signature = Regex.Match(body, SignaturePattern).Groups[1].Value;

                return string.Format(
                    PlaylistUrlFormat,
                    channelId,
                    UrlEcodeToken(token),
                    signature);
            }
        }

        private string UrlEcodeToken(string token)
        {
            string processedToken = token.Replace("\\\"", "\"");

            return HttpUtility
                .UrlEncode(processedToken)
                .Replace("+", "%20");
        }
    }
}

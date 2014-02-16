using System;
using System.Text;
using System.Collections.Concurrent;

using System.Security.Cryptography;
using System.Net;
using System.IO;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Twitter_Parser
{

    class Parser
    {
        static Parser parser;
        static public BlockingCollection<Tweet> tweets = new BlockingCollection<Tweet>(new ConcurrentQueue<Tweet>());

        // OAuth keys generated at http://dev.twitter.com/apps. For security purposes would be better to put these in web.config.
        // Other OAuth connection/authentication variables
        public string OAuthToken;
        public string OAuthTokenSecret;
        public string OAuthConsumerKey;
        public string OAuthConsumerSecret;

        public Parser(string OAuthToken, string OAuthTokenSecret, string OAuthConsumerKey, string OAuthConsumerSecret) {
            this.OAuthToken = OAuthToken;
            this.OAuthTokenSecret = OAuthTokenSecret;
            this.OAuthConsumerKey = OAuthConsumerKey;
            this.OAuthConsumerSecret = OAuthConsumerSecret;

        }

        //static void Main()
       // {
        //   parser = new Parser(key1, key2, key3, key4);
        //    parser.findKeywords(keyword);

        
        //}

        public void Queue(Tweet t)
        {
            tweets.Add(t);
        }

        public Tweet Dequeue()
        {
           return tweets.Take();
        }
        public class Tweet
        {
            public string name;
            public string text;
            public string location;
            JObject geo;
            JObject place;
            JObject coordinates;

            public Tweet(string name, string text, string location)
            {
                this.name = name;
                this.text = text;
                this.location = location;
            }
        }

        protected void findKeywords(string keyword)
        {
            string OAuthVersion = "1.0";
            string OAuthSignatureMethod = "HMAC-SHA1";
            string OAuthNonce = Convert.ToBase64String(new ASCIIEncoding().GetBytes(DateTime.Now.Ticks.ToString()));
            TimeSpan timeSpan = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            string OAuthTimestamp = Convert.ToInt64(timeSpan.TotalSeconds).ToString();
            string ResourceUrl = "https://stream.twitter.com/1.1/statuses/filter.json";

            // Generate OAuth signature. Note that Twitter is very particular about the format of this string. Even reordering the variables
            // will cause authentication errors.


            //THESE VARIABLES MUST BE IN ALPHABETICAL ORDER, OR THIS WILL FAIL

            var baseFormat = "oauth_consumer_key={0}&oauth_nonce={1}&oauth_signature_method={2}" +
            "&oauth_timestamp={3}&oauth_token={4}&oauth_version={5}&track={6}";

            var baseString = string.Format(baseFormat,
            OAuthConsumerKey,
            OAuthNonce,
            OAuthSignatureMethod,
            OAuthTimestamp,
            OAuthToken,
            OAuthVersion,
            Uri.EscapeDataString(keyword)
            );

            baseString = string.Concat("GET&", Uri.EscapeDataString(ResourceUrl), "&", Uri.EscapeDataString(baseString));

            // Generate an OAuth signature using the baseString

            var compositeKey = string.Concat(Uri.EscapeDataString(OAuthConsumerSecret), "&", Uri.EscapeDataString(OAuthTokenSecret));
            string OAuthSignature;
            using (HMACSHA1 hasher = new HMACSHA1(ASCIIEncoding.ASCII.GetBytes(compositeKey)))
            {
                OAuthSignature = Convert.ToBase64String(hasher.ComputeHash(ASCIIEncoding.ASCII.GetBytes(baseString)));
            }

            // Now build the Authentication header. Again, Twitter is very particular about the format. Do not reorder variables.

            var HeaderFormat = "OAuth " +
            "oauth_consumer_key=\"{0}\", " +
            "oauth_nonce=\"{1}\", " +
            "oauth_signature=\"{2}\", " +
            "oauth_signature_method=\"{3}\", " +
            "oauth_timestamp=\"{4}\", " +
            "oauth_token=\"{5}\", " +
            "oauth_version=\"{6}\"";

            var authHeader = string.Format(HeaderFormat,
            Uri.EscapeDataString(OAuthConsumerKey),
            Uri.EscapeDataString(OAuthNonce),
            Uri.EscapeDataString(OAuthSignature),
            Uri.EscapeDataString(OAuthSignatureMethod),
            Uri.EscapeDataString(OAuthTimestamp),
            Uri.EscapeDataString(OAuthToken),
            Uri.EscapeDataString(OAuthVersion)
            );

            // Now build the actual request

            ServicePointManager.Expect100Continue = false;
            var postBody = string.Format("track={0}", Uri.EscapeDataString(keyword));
            ResourceUrl += "?" + postBody;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(ResourceUrl);
            request.Headers.Add("Authorization", authHeader);
            request.Method = "GET";
            request.ContentType = "application/x-www-form-urlencoded";

            // Retrieve the response data and deserialize the JSON data to a list of Tweet objects
            //WebResponse response = request.GetResponse();
            request.BeginGetResponse(ar =>
            {
                var req = (WebRequest)ar.AsyncState;
                var response = req.EndGetResponse(ar);
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    // This loop goes as long as twitter is streaming
                    while (!reader.EndOfStream)
                    {
                        var json_obj = (JObject)JsonConvert.DeserializeObject(reader.ReadLine());
                        string name = (string) json_obj["user"]["name"];
                        string text = (string)json_obj["text"];
                        string location = (string) json_obj["user"]["location"];
                        //Console.WriteLine(json_obj["geo"]);
                        //Console.WriteLine(json_obj["place"]);
                        //Console.WriteLine(json_obj["coordinates"]);
                        if (location != null && location != "")
                        {
                            parser.Queue(new Tweet(name, text, location));
                        }
                    }
                }  
            }, request);
        }
    }
}

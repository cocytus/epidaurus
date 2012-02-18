using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Web;
using System.Runtime.Serialization;
using System.Threading;

namespace Epidaurus.ScannerLib.Utils
{
    public static class GoogleApi
    {
        private static DateTime PreviousSearchTime;

        private const string SearchUrl = "http://ajax.googleapis.com/ajax/services/search/web?v=1.0&q={0}&key={1}";
        public static SearchResult[] Search(string searchTerm)
        {
            ThrottleSearches();
            string apiKey = ConfigurationManager.AppSettings["googleApiKey"];
            var query = HttpUtility.UrlEncode(searchTerm);

            try
            {
                var result = Json.JsonDeserialize<OuterResultContainer>(string.Format(SearchUrl, query, apiKey));
                if (result == null || result.ResponseData == null || result.ResponseData.Results == null)
                    return null;

                return result.ResponseData.Results;
            }
            finally 
            {
                PreviousSearchTime = DateTime.Now;
            }
        }

        private static void ThrottleSearches()
        {
            var toSleep = (PreviousSearchTime + TimeSpan.FromMilliseconds(1100)) - DateTime.Now;
            if (toSleep.TotalMilliseconds > 0.0)
                Thread.Sleep(toSleep);
        }

        [DataContract]
        private class OuterResultContainer
        {
            [DataMember(Name="responseData")]
            public ResponseData ResponseData { get; set; }
        }

        [DataContract]
        private class ResponseData
        {
            [DataMember(Name = "results")]
            public SearchResult[] Results { get; set; }
        }

    }

    [DataContract]
    public class SearchResult
    {
        [DataMember(Name="url")]
        public string Url { get; set; }
        
        [DataMember(Name="titleNoFormatting")]
        public string Title { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Runtime.Serialization.Json;

namespace Epidaurus.ScannerLib.Utils
{
    public static class Json
    {
        public static T JsonDeserialize<T>(string url)
        {
            var req = (HttpWebRequest)WebRequest.Create(url);
            using (var response = req.GetResponse())
            using (var reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
            {
                string jsonData = reader.ReadToEnd();

                var serializer = new DataContractJsonSerializer(typeof(T));
                using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(jsonData)))
                    return (T)serializer.ReadObject(ms);
            }
        }
    }
}

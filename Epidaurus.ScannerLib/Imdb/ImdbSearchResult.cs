using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace Epidaurus.ScannerLib
{
    [DataContract]
    public class ImdbSearchResult
    {
        [DataMember] public string Title;
        [DataMember] public string Year;
        [DataMember] public string Rated;
        [DataMember] public string Released;
        [DataMember] public string Genre;
        [DataMember] public string Director;
        [DataMember] public string Writer;
        [DataMember] public string Actors;
        [DataMember] public string Plot;
        [DataMember] public string Poster;
        [DataMember] public string Runtime;
        [DataMember] public string Rating;
        [DataMember] public string Votes;
        [DataMember] public string Response;
        [DataMember(Name = "ID")] 
        public string ImdbId;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using Epidaurus.Domain.Entities;

namespace Epidaurus.ScannerLib
{
    public class MovieDataSourceQueryResult
    {
        public string Title { get; set; }
        public short Year { get; set; }
        public string[] Genres { get; set; }
        public MovieDataSourcePersonData[] Casts { get; set; }
        public string Plot { get; set; }
        public string Poster { get; set; }
        public int Runtime { get; set; }
        public int Score { get; set; }
        public int Votes { get; set; }
        public string ImdbId { get; set; }
        public int? TmdbId { get; set; }
        public string Homepage { get; set; }
    }

    public struct MovieDataSourcePersonData
    {
        public MovieDataSourcePersonData(string name, string imdbId, int tmdbId, int sortOrder, Cast.Roles role)
        {
            _name = name;
            _imdbId = imdbId;
            _tmdbId = tmdbId;
            _sortOrder = sortOrder;
            _role = role;
        }

        private readonly string _name;
        public string Name { get { return _name; } }
        private readonly string _imdbId;
        public string ImdbId { get { return _imdbId; } }
        private readonly int _tmdbId;
        public int TmdbId { get { return _tmdbId; } }

        private readonly int _sortOrder;
        public int SortOrder { get { return _sortOrder; } }

        private readonly Cast.Roles _role;
        public Cast.Roles Role { get { return _role; } }
    }
}

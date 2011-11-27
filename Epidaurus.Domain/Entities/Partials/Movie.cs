using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Epidaurus.Domain.Entities
{
    public partial class Movie : IEntityWithMovieSystemServiceKnowledge
    {
        public MovieSystemService MovieSystemService { get; set; }

        public static Movie Create(string title, string imdbId, short? year = null, short? seriesSeason= null, short? seriesEpisode=null)
        {
            return new Movie()
            {
                Title = title,
                ImdbId = imdbId,
                ImdbQueried = false,
                Plot = "",
                Score = -1,
                SeriesEpisode = seriesEpisode,
                SeriesSeason = seriesSeason,
                Year = year.HasValue ? year.Value : (short)-1,
                AddedAt = DateTime.Now,
                ImdbQueryFailCount = 0
            };
        }

        public void SetGenres(IEnumerable<string> genres)
        {
            Genres.Clear();
            foreach (var genre in genres)
                this.Genres.Add(MovieSystemService.GetOrCreateGenre(genre));
        }

        public MovieAtStorage AddStorageLocation(StorageLocation sl, string relativePath, string samplePath, string cleanedName)
        {
            if (!MovieAtStorages.Any())
                AddedAt = DateTime.Now;

            var mas = new MovieAtStorage()
            {
                RelativePath = relativePath,
                SampleRelativePath = samplePath,
                StorageLocation = sl,
                CleanedName = cleanedName
            };

            MovieAtStorages.Add(mas);
            return mas;
        }

        public void RemoveStorageLocation(int storageLocationId)
        {
            MovieSystemService.RemoveStorageLocationFromMovie(Id, storageLocationId);
        }

        public void SetMySeenStatus(bool seen)
        {
            var db = MovieSystemService.DbEntities;
            var user = MovieSystemService.LoggedInUser;

            if (seen)
            {
                var ss = new SeenStatus()
                {
                    Movie = this,
                    User = user,
                    SeenAt = DateTime.Now,
                    Review = "",
                    Score = 0
                };
            }
            else
            {
                var todel = user.SeenStatuses.Where(ss => ss.Movie.Id == this.Id).ToList(); //FIXME: SLOW
                todel.ForEach(db.DeleteObject);
            }
            MovieSystemService.Save();
        }

        public void ClearType(Cast.Jobs type)
        {
            var toDel = Casts.Where(el => el.Job == type.ToString()).ToList();
            foreach (var del in toDel)
                Casts.Remove(del);
        }

        public void AddCastMember(Cast.Jobs job, string name, string imdbId, int tmdbId, int sortOrder, string roleName)
        {
            if (this.EntityState == System.Data.EntityState.Added)
                throw new InvalidOperationException("Can not add cast to new movie");
            var person = MovieSystemService.GetOrCreatePerson(name, imdbId, tmdbId);
            var cast = Cast.CreateCast(0, job.ToString(), person.Id, Id);
            cast.SortOrder = sortOrder;
            cast.RoleName = !string.IsNullOrWhiteSpace(roleName) ? roleName : null;
            MovieSystemService.DbEntities.AddToCasts(cast);
        }
    }
}

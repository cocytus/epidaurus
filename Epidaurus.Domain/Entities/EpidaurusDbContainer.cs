using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace Epidaurus.Domain.Entities
{
    public partial class EpidaurusDbContainer
    {
        private readonly MovieSystemService _movieSystemService;
        private static readonly string _connBase = "metadata=res://*/Entities.EpidaurusDb.csdl|res://*/Entities.EpidaurusDb.ssdl|res://*/Entities.EpidaurusDb.msl;provider=System.Data.SqlClient;provider connection string='{0}'";

        public EpidaurusDbContainer(MovieSystemService mss) 
            : this(string.Format(_connBase, DefaultConnectionString))
        {
            _movieSystemService = mss;
            this.ObjectMaterialized += EpidaurusDbContainer_ObjectMaterialized;
        }

        void EpidaurusDbContainer_ObjectMaterialized(object sender, System.Data.Objects.ObjectMaterializedEventArgs e)
        {
            var ent = e.Entity as IEntityWithMovieSystemServiceKnowledge;
            if (ent != null)
                ent.MovieSystemService = _movieSystemService;
        }

        public static EpidaurusDbContainer Create(string connectionString = null)
        {
            if (connectionString == null)
                connectionString = DefaultConnectionString;

            return new EpidaurusDbContainer(string.Format(_connBase, connectionString));
        }

        private static string _defaultConnectionString;
        private static string DefaultConnectionString 
        {
            get 
            {
                if (_defaultConnectionString == null)
                    _defaultConnectionString = ConfigurationManager.ConnectionStrings["db"].ConnectionString;
                return _defaultConnectionString;
            }
        }
    }
}

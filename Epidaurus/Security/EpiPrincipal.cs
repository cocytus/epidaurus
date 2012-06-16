using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Security.Principal;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace Epidaurus.Security
{
    //[Serializable]
    public class EpiPrincipal : MarshalByRefObject, IPrincipal
    {
        private readonly EpiIdentity _identity;
        private readonly string[] _roles;

        public EpiPrincipal(string name, string[] roles)
        {
            _identity = new EpiIdentity(name);
            _roles = roles;
        }

        public IIdentity Identity
        {
            get { return _identity; }
        }

        public bool IsInRole(string role)
        {
            if (_roles.Contains(EpiRoles.Admin))
                return true;
            return _roles.Contains(role);
        }

        public string RolesList { get { return string.Join(",", _roles); } }
    }

    //[Serializable]
    public class EpiIdentity : MarshalByRefObject, IIdentity
    {
        public EpiIdentity(string name)
        {
            Name = name;
        }

        public string AuthenticationType
        {
            get { return "Forms authentication"; }
        }

        public bool IsAuthenticated
        {
            get 
            { 
                return true;
            }
        }

        public string Name { get; private set; }
    }
}
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Security;
using System.Threading;
using Autofac;
using Autofac.Integration.Mvc;
using Epidaurus.Domain;
using Epidaurus.ScannerLib;
using Epidaurus.Domain.Mime;

namespace Epidaurus
{
    public class MvcApplication : System.Web.HttpApplication
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }

        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                "Default", // Route name
                "{controller}/{action}/{id}", // URL with parameters
                new { controller = "Movie", action = "Index", id = UrlParameter.Optional } // Parameter defaults
            );

        }

        protected void Application_Start()
        {
            SetupDependencyResolver();
            AreaRegistration.RegisterAllAreas();

            RegisterGlobalFilters(GlobalFilters.Filters);
            RegisterRoutes(RouteTable.Routes);
        }

        protected void Application_PreRequestHandlerExecute()
        {
            Security.SecurityService.TryLoadCurrentPrincipalFromSession();
        }

        private static void SetupDependencyResolver()
        {
            var builder = new ContainerBuilder();
            builder.RegisterControllers(typeof(MvcApplication).Assembly);
            builder.RegisterType<MovieSystemService>().InstancePerHttpRequest();
            builder.RegisterType<MovieInformationUpdater>().InstancePerHttpRequest();
            builder.RegisterType<StaticMimeTypeResolver>().As<IMimeTypeResolver>().SingleInstance();
            var container = builder.Build();
            DependencyResolver.SetResolver(new AutofacDependencyResolver(container));
        }
    }
}
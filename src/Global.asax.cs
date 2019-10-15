using Piggy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace PigSwitch
{
    public partial class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            OnAppStart();
        }

        protected void Application_Error(object sender, EventArgs e)
        {
            if (Server != null)
            {
                var url = HttpContext.Current?.Request?.Url?.ToString();
                Exception ex = Server.GetLastError();
                ex.SaveToDB(url);                
            }
        }
    }
}

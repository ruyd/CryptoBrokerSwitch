using System;
using System.Collections.Generic;
using System.Linq;
using Owin;


namespace PigSwitch
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            Microsoft.ApplicationInsights.Extensibility.TelemetryConfiguration.Active.DisableTelemetry = true;

            app.MapSignalR();
        }
    }
}

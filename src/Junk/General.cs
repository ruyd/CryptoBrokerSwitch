using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace PigSwitch
{
    public class GeneralFunctions
    {
        public static bool IsAzureWebsite()
        {
            return (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME")) || !String.IsNullOrEmpty(Environment.GetEnvironmentVariable("RoleRoot")));
        }
        public static string GetBuildIdentifier(System.Reflection.Assembly assembly = null)
        {
            var buildIdentifier = "NOT FOUND";

            if (assembly == null)
                assembly = System.Reflection.Assembly.GetExecutingAssembly();
            
            var fFile = new System.IO.FileInfo(assembly.Location);
            var fileDate = fFile.LastWriteTime;

            if (IsAzureWebsite())
                fileDate = fileDate.AddHours(-5);

            var buildDate = $"{fileDate.ToShortDateString()} - {fileDate.ToShortTimeString()}";
            var version = assembly.GetName().Version.ToString(2);
            buildIdentifier = version + " @ " + buildDate;

            return buildIdentifier;
        }

        public static StackTraceParse ParseStackMessage(string m)
        {
            var response = new StackTraceParse();

            var r = new Regex(@"at (?<namespace>.*)\.(?<class>.*)\.(?<method>.*(.*)) in (?<file>.*):line (?<line>\d*)");
            var result = r.Match(m);
            if (result.Success)
            {
                response.Namespace = result.Groups["namespace"].Value.ToString();
                response.ClassName = result.Groups["class"].Value.ToString();
                response.MethodName = result.Groups["method"].Value.ToString();
                response.FileName = result.Groups["file"].Value.ToString();
                response.LineNumber = result.Groups["line"].Value.ToString();
            }

            return response;
        }
        public class StackTraceParse
        {
            public string Namespace { get; set; }
            public string ClassName { get; set; }
            public string MethodName { get; set; }
            public string FileName { get; set; }
            public string LineNumber { get; set; }
        }

    }
}
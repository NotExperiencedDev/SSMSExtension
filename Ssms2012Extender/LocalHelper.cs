using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Ssms2012Extender
{
    public static class LocalHelper
    {
        public const string ConfigFileName = "DbGroupConfig.xml";
        public static string GetPathToConfigFile
        {
            get
            {
                string path = string.Empty;
                Assembly executingAssembly = Assembly.GetEntryAssembly();
                if (executingAssembly == null)
                    executingAssembly = Assembly.GetExecutingAssembly();
                if (executingAssembly != null)
                {
                    path = System.IO.Path.GetDirectoryName(executingAssembly.Location) + "\\" + ConfigFileName;
                }

                if (string.IsNullOrEmpty(path))
                    path = System.IO.Directory.GetCurrentDirectory() + "\\" + ConfigFileName;

                return path;
            }
        }

        public const string DefaultLogPath = "C:\\";
        public const string LogFileName = "Ssms2012ExtenderLog.log";
        public static bool LoggingEnabled
        {
            get
            {
                if (string.IsNullOrEmpty(LoggingPath))
                    return false;

                return object.Equals(
                    Microsoft.Win32.Registry.GetValue("HKEY_CURRENT_USER\\Software\\Ssms2012Extender",
                                                      "LoggingEnabled", 0), 1);
            }
        }

        public static string LoggingPath
        {
            get
            {
                return 
                (string)(Microsoft.Win32.Registry.GetValue("HKEY_CURRENT_USER\\Software\\Ssms2012Extender",
                                                              "LoggingPath", string.Empty));
            }
        }
    }
}

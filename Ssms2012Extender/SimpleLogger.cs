using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Ssms2012Extender
{
    
    public class SimpleLogger
    {
        private static SimpleLogger logger = null;
        private string logPath;
        private bool loggingEnabled = false;
        private List<string> loggs = null;
        object sync = new object();

        private SimpleLogger()
        {
          
        }


        public static SimpleLogger CreateLogger(bool loggingEnabled, string path)
        {
           if(logger == null)
                    logger = new SimpleLogger();

            logger.logPath = path;
            logger.loggingEnabled = loggingEnabled;
            logger.loggingEnabled = !string.IsNullOrEmpty(path);
            logger.loggs = new List<string>();
            return logger;
        }

        private const string logstart = "START";
        private const string logend = " END ";



        /// <summary>
        /// Call LogAsync with log start string
        /// </summary>
        /// <param name="args">objects to log</param>
        public void LogStart(params object[] args)
        {
            Log(logstart, args);
        }

        /// <summary>
        /// calls log async with logend stirng
        /// </summary>
        /// <param name="args"></param>
        public void LogEnd(params object[] args)
        {
            Log(logend, args);
        }

        /// <summary>
        /// Calls asynclog with stamp string + params
        /// </summary>
        /// <param name="stamp">additional string</param>
        /// <param name="args">params</param>
        public void Log(string stamp, params object[] args)
        {
            object[] logs = new object[] { stamp };
            if (args != null)
            {
                Array.Resize(ref logs, logs.Length + args.Length);
                Array.Copy(args, 0, logs, 1, args.Length);
            }
            Log(logs);
        }

        /// <summary>
        /// Calls log asynchronously
        /// </summary>
        /// <param name="args">objects to be logged</param>
        public void Log(params object[] args)
        {
            string toLog = "{0}";
            int len = args == null ? 0 : args.Length;
            object[] objectsToLog = new object[len + 1];
            objectsToLog[0] = DateTime.UtcNow;
            for (int i = 0; i < len; i++)
            {
                objectsToLog[i + 1] = args[i];
                toLog += "|{" + (i + 1).ToString() + "}";
            }
            AddLog(string.Format(toLog, objectsToLog));
        }

        private void AddLog(string logtext)
        {
            if (loggingEnabled)
            {
                loggs.Add(logtext);
                Action act = new Action(LogToFile);
                act.BeginInvoke(null, null);
            }
        }

      
        void LogToFile()
        {
            lock (sync)
            {
                try
                {
                    using (StreamWriter sw = new StreamWriter(logPath+LocalHelper.LogFileName, true))
                    {
                        if (loggs.Count > 0)
                        {
                            string logText = logText = loggs[0];
                            loggs.RemoveAt(0);
                            sw.WriteLine(logText);
                        }

                    }
                }
                catch (Exception ex)
                {
                    System.Windows.Forms.MessageBox.Show(string.Format("You cannot logg to file {0}. Check your permission to the logg path folder", logPath));
                }
            }
        }

    }
}

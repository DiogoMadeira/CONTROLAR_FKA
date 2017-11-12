using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace Preh {
    internal static class Log {
        public static bool IsDebugMessages { get; set; }
        public static Logger Instance { get; private set; }
        static Log() {

            //LogManager.Configuration = new XmlLoggingConfiguration(AppDomain.CurrentDomain.BaseDirectory + @"Main\NLog\NLog.xml", true);
            LogManager.Configuration = new XmlLoggingConfiguration(AppDomain.CurrentDomain.BaseDirectory + @"Main\NLog\NLog.config", true);
            //LoggingConfiguration config = new LoggingConfiguration();


            var sentinalTarget = new NLogViewerTarget() {
                Name = "sentinal",
                Address = "udp://127.0.0.1:9999",
                IncludeNLogData = true
            };
            var sentinalRule = new LoggingRule("*", LogLevel.Trace, sentinalTarget);
            LogManager.Configuration.AddTarget("sentinal", sentinalTarget);
            LogManager.Configuration.LoggingRules.Add(sentinalRule);

            var harvesterTarget = new OutputDebugStringTarget() {
                Name = "harvester",
                Layout = "${log4jxmlevent:includeNLogData=false}"
            };
            var harvesterRule = new LoggingRule("*", LogLevel.Trace, harvesterTarget);
            LogManager.Configuration.AddTarget("harvester", harvesterTarget);
            LogManager.Configuration.LoggingRules.Add(harvesterRule);

            LogManager.ReconfigExistingLoggers();

            Instance = LogManager.GetCurrentClassLogger();
        }
    }
}

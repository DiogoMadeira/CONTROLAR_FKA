using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NLog;

namespace Preh.Main
{
    public class NLog
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public void Alarm_Error(string erro)
        {
            logger.Error(erro);
        }

        public void Alarm_Warning(string warning)
        {
            logger.Warn(warning);
        }

        public void Alarm_Trace(string trace)
        {
            logger.Trace(trace);
        }

        public void Alarm_Info(string info)
        {
            logger.Info(info);
        }

        public void Alarm_Debug(string debug)
        {
            logger.Debug(debug);
        }

        public void Alarm_Fatal(string faltal)
        {
            logger.Fatal(faltal);
        }
    }


}

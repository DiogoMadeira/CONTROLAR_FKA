using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;



namespace Preh
{
    public static class ErrorControl
    {

        public static void CreateErrorLog(string error)
        {
            StreamWriter SW;

            try
            {
                var RuningPath = AppDomain.CurrentDomain.BaseDirectory;
                var strFileLocation = RuningPath + "\\InfoLog.txt";

                if (!File.Exists(strFileLocation)) SW = File.CreateText(strFileLocation);

                SW = File.AppendText(strFileLocation);
                SW.WriteLine(DateTime.Now.ToString() + "_" + error);
                SW.Close();
            }
            catch (Exception ex)
            {
                throw ex.InnerException;
            }
        }
    }
}

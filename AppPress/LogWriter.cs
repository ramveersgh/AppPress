using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Globalization;

namespace AppPressFramework
{
    public class Log
    {
        public static void Writeln(string logMessage)
        {
            //Ram: Do not write in log, This is noticied that writing in file and modifying directory is also caused of session expire.
            //So only log when this is required.
            return;
            //Write(logMessage+"\r\n");
        }
        private static void Write(string logMessage)
        {
            var m_exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            try
            {
                using (StreamWriter w = File.AppendText(m_exePath + "\\" + "AppPressLog_"+DateTime.UtcNow.ToString("dd_MM_yyyy", DateTimeFormatInfo.InvariantInfo)+".txt"))
                {
                    LogW(logMessage, w);
                }
            }
            catch (Exception)
            {
            }
        }


        private static void LogW(string logMessage, TextWriter txtWriter)
        {
            try
            {
                txtWriter.Write("{0} {1}", DateTime.UtcNow.ToLongTimeString(),
                    DateTime.UtcNow.ToShortDateString());
                txtWriter.Write("  :{0}", logMessage);
            }
            catch (Exception)
            {
            }
        }
    }
}

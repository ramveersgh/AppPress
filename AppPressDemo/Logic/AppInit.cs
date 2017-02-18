using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Configuration;
using System.Security;
using System.Threading;
using System.Web;
using AppPressFramework;
using ApplicationClasses;

namespace Application
{

    public class ApplicationSettings : AppPressSettings
    {
        public string LogoPathSmall;
        public bool IsLocalHost = false;
        public string SupportEmail = null;
        public string ProductName;
        public string ProductURL;
    }
    public class AppPressApplication
    {
        public static ApplicationSettings Settings = null;

        private static Thread PurgeUnusedFilesThread = null;
        private static void PurgeUnusedFiles()
        {
            try
            {

                while (true)
                {
                    // sleep to 1 min past midnight on next day
                    var timeToNextMidnight = (DateTime.Now.Date.AddDays(1) - DateTime.Now).Add(new TimeSpan(0, 1, 0));
                    Thread.Sleep(timeToNextMidnight); // 1 sec

                    var site = new DAOBasic();
                    try
                    {
                        AppPress.MarkFilesUnused(site, true);
                    }
                    finally
                    {
                        site.Close();
                    }
                }
            }
            catch
            {
                // Do something with exception here
            }
        }

        internal static void InitApplication()
        {
            if (Settings != null)
                return;
            try
            {
                bool isDebug = false;
#if DEBUG
                isDebug = true;
#endif
                Settings = new ApplicationSettings();
                string ip = System.Web.HttpContext.Current.Request.UserHostAddress;
                Settings.IsLocalHost = (ip == "127.0.0.1" || ip == "::1");
                string dbName = "AppPressDemo";
                Settings.developer = isDebug;
                Settings.DEBUG = isDebug;

                Settings.databaseType = DatabaseType.SqlServer;

                Settings.NetDateFormat = "dd-MMM-yyyy";
                Settings.NetDateTimeFormat = "dd-MMM-yyyy HH:mm";
                Settings.NetDateMonthFormat = "MMM-yyyy";
                Settings.JQueryDateMonthFormat = "M-yy";
                if (Settings.databaseType == DatabaseType.SqlServer)
                    Settings.SQLDateFormat = "dd-MMM-yyyy";
                else // MySQL
                    Settings.SQLDateFormat = "%d-%b-%Y";
                if (Settings.databaseType == DatabaseType.SqlServer)
                    Settings.SQLDateTimeFormat = Settings.SQLDateFormat + " hh:mm";
                else // MySQL
                    Settings.SQLDateTimeFormat = Settings.SQLDateFormat + " %h:%i %p";

                Settings.JQueryDateFormat = "dd-M-yy";
                Settings.AdditionalInputDateFormats = "dd-MM-yyyy | dd/MM/yyyy";

                Settings.DefaultForm = "Login";

                Settings.applicationAssembly = System.Reflection.Assembly.GetExecutingAssembly();
                Settings.applicationNameSpace = "Application";
                Settings.applicationClassName = "AppLogic";

                Settings.pluginAssemblyNames = new List<string>();

                Settings.ProductName = "AppPressDemo";
                Settings.ProductURL = "http://www.hrmates.com";
                Settings.Instances.Add(new AppPressInstance { InstanceId = 4, InstanceBaseUrl = HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Authority) + "/" + AppPress.GetDefaultAspx(), ApplicationData = "AppPressDemo", LocalInstance = dbName == "AppPressDemo" });

                Settings.ConnectionString = ConfigurationManager.ConnectionStrings["AppPressDemo"].ConnectionString;

                Settings.LogoPathSmall = AppPress.GetBaseUrl() + "Resources/img/Logo_Small.png";

                var smtpSection = (SmtpSection)ConfigurationManager.GetSection("system.net/mailSettings/smtp");
                Settings.Smtp = new Smtp();
                Settings.Smtp.Host = smtpSection.Network.Host;
                Settings.Smtp.Port = smtpSection.Network.Port;
                Settings.Smtp.EnableSsl = smtpSection.Network.EnableSsl;
                Settings.Smtp.UserName = smtpSection.Network.UserName;
                Settings.Smtp.Password = smtpSection.Network.Password;

                //Settings.encryptionKey = EmpireEncryption.EmpireKey.GetEncryptionKey(site.dbName);
                Settings.encryptionKey = @"https://";

                Settings.DebugEmail = "hrmatestestmail@sysmates.com";
                Settings.useDebugEmail = AppPressApplication.Settings.IsLocalHost;
                Settings.ApplicationAppPress = typeof(AppLogic.AppPressDemo);
                AppPress.InitAppPress(Settings);
                var site = new DAOBasic();
                try
                {
                    var a = new AppPress(site);
                    // Do any Database specific work here
                }
                finally
                {
                    site.Close();
                }
                if (PurgeUnusedFilesThread == null)
                {
                    PurgeUnusedFilesThread = new Thread(PurgeUnusedFiles);
                    PurgeUnusedFilesThread.Priority = ThreadPriority.Lowest;
                    PurgeUnusedFilesThread.Start();
                }
                // just in case you host Debug version on server
                if (Settings.DEBUG && !Settings.IsLocalHost)
                    throw new Exception("Debug version is hosted on server. Need release version to run application.");
            }
            catch
            {
                Settings = null;
                throw;
            }
        }

        private static string GetDebugUrl(string url, string newport)
        {
#if DEBUG
            url = url.Replace(HttpContext.Current.Request.Url.Port.ToString(), newport);
#endif
            return url;
        }
    }
}
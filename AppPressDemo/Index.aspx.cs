/* Copyright SysMates Technologies Pte. Ltd. */
using System;
using System.Web;
using System.Web.UI;
using AppPressFramework;

namespace Application
{
    public partial class Index : Page
    {
        protected void Page_Init(object sender, EventArgs e)
        {
            if (Request.Browser.MajorVersion < 45) // just to speed up things a bit
                switch (Request.Browser.Browser)
                {
                    case "Chrome":
                        //if (Request.Browser.MajorVersion < 45)
                        //    throw new AppPressException("The Chrome Browser you are using is old version. Please upgrade the Browser to latest version");
                        break;
                    case "Firefox":
                        if (Request.Browser.MajorVersion < 45)
                            throw new AppPressException("The Firefox Browser you are using is old version. Please upgrade the Browser to latest version");
                        break;
                    case "InternetExplorer":
                        if (Request.Browser.MajorVersion < 11)
                            throw new AppPressException("The Internet Explorer Browser you are using is old version. Please upgrade the Browser to at least version 11");
                        break;
                }
            if (HttpContext.Current != null)
            {
                var sessionData = HttpContext.Current.Session["SessionData"] as SessionData;
                if (sessionData != null)
                {
                    ViewState["AppPressSessionData"] = sessionData;
                }
                else if (ViewState["AppPressSessionData"] != null)
                {
                    HttpContext.Current.Session["SessionData"] = ViewState["AppPressSessionData"];
                }
            }
        }
        internal static void ErrorResponse(HttpResponse Response, string message, string stackTrace)
        {

            Response.Clear();
            Response.ContentType = "text/html";
            try
            {
                string skin = System.IO.File.ReadAllText(HttpContext.Current.Server.MapPath("~/Skins/Error.html"));
                //skin = CompileSkin(a, skin, false, SkinType.HTML);
                skin = skin.Replace("_DefaultAspxPage_", AppPress.GetDefaultAspx());
                skin = skin.Replace("_ThisIsReplacedByErrorMessage_", System.Web.HttpUtility.HtmlEncode(message).Replace("\n", "<br/>"));
                skin = skin.Replace("_ThisIsReplacedByLoginUrl_", System.Web.HttpUtility.HtmlEncode(AppPress.GetBaseUrl() + AppPress.GetDefaultAspx()).Replace("\n", "<br/>"));

                skin = skin.Replace("_ThisIsReplacedByStackTrace_", System.Web.HttpUtility.HtmlEncode(stackTrace));
                Response.AddHeader("Cache-Control", "no-store; no-cache");
                Response.Write(skin);
            }
            catch
            {
                Response.Write(message);
            }
        }
        protected void Page_Load(object sender, EventArgs e)
        {
            AppPressApplication.InitApplication();
            try
            {
                new AppPressHandler(new AppLogic());
            }
            catch (SessionExpiredException Ex)
            {
                string errorMsg = "Error: " + Ex.Message;
                if (Ex.InnerException != null)
                    errorMsg += ", InnerException: " + Ex.InnerException.Message;
                if (Ex.InnerException != null && Ex.InnerException.InnerException != null)
                    errorMsg += ", InnerInnerException: " + Ex.InnerException.InnerException.Message;
                Log.Writeln("Error: " + "Session Expired. " + errorMsg);
                ErrorResponse(HttpContext.Current.Response, "Session Expired. " + errorMsg, Ex.StackTrace);
            }
            catch (Exception ex)
            {
                if (ex.GetType() != typeof(System.Threading.ThreadAbortException))
                {
                    string errorMsg1 = ex.Message;
                    if (ex.InnerException != null)
                        errorMsg1 += ", InnerException: " + ex.InnerException.Message;
                    if (ex.InnerException != null && ex.InnerException.InnerException != null)
                        errorMsg1 += ", InnerInnerException: " + ex.InnerException.InnerException.Message;
                    Log.Writeln("Error: " + errorMsg1 + "     StackTrace: " + ex.StackTrace);
                    ErrorResponse(HttpContext.Current.Response, ex.Message, ex.StackTrace);
                }
            }
        }

      
        
    }
}
/* Copyright SysMates Technologies Pte. Ltd. */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data;
using System.IO;
using ApplicationClasses;
using AppPressFramework;
using System.Runtime.Serialization;

namespace Application
{
    public partial class AppLogic
    {

        
        public const string QuerySeperator = "sdfsdfsdfsdfsdf";

        

        
       
        public class ApplicationData
        {
            public string Error;
        }
        [DataContract]
        public class AppPressDemo : AppPress
        {
            public AppPressDemoSessionData appPressDemoSessionData;
            public string loginUserId = null;
            public bool mobile = false;
            public AppPressDemo(DAOBasic site, bool checkSession) : base(site)
            {
                Init(checkSession);
            }
            public override void Redirect(long formDefId, string formId, RedirectParams redirectParams)
            {
                if (redirectParams == null)
                    redirectParams = new RedirectParams();
                base.Redirect(formDefId, formId, redirectParams);
            }
            public void Init(bool checkSession)
            {
                appPressDemoSessionData = (AppPressDemoSessionData)TryGetSessionData();
                if (appPressDemoSessionData == null && checkSession)
                {
                    LoginClass.Redirect(this, null, new RedirectParams { formError = "Session Expired. Please Login again." });
                    throw new AppPressException();
                }
                if (appPressDemoSessionData != null)
                {
                    mobile = appPressDemoSessionData.FromMobile;
                    loginUserId = appPressDemoSessionData.loginUserId;
                }
            }
        }

        public static void Init(AppPressDemo a, ErrorFormClass ErrorForm)
        {
            if (a.sessionData != null)
                // AppPressOnClick in Message was causing problem so encoded it 
                ErrorForm.Message.val =HttpUtility.HtmlEncode(((CustomizeException)a.sessionData.UserData[AppPressKeys.ErrorFormException]).Message).Replace("A", "&#65;");
        }

        public static bool IsAdmin(AppPressDemo p)
        {
            if (p.remoteLoginUserId != null || p.mobile)
                return false;
            if (p.PageURL["fromMenu"] != null)
                return false;
            if (p.PageURL["FromEmployeeAdmin"] != null)
            {
                //Do not check in table permission_employeeadministration becasue if comming from EmployeeAdmin page then he should have access to modify of all details.
                return true;
            }
            return p.appPressDemoSessionData.isAdmin;
        }
        //public static bool IsSystemAdmin(AppPressDemo p, string employeeId)
        //{
        //    var count = p.ExecuteInt32("SELECT Count(1) FROM permission_systemadministration Where Employee = " + employeeId);
        //    return count > 0;
        //}
        
    }
   
}




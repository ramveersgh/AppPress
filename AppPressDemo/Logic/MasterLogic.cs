/* Copyright SysMates Technologies Pte. Ltd. */
using System.Collections.Generic;
using ApplicationClasses;
using AppPressFramework;

namespace Application
{
    public partial class AppLogic
    {
        public static void Calc(AppPressDemo a, MasterClass.AppAnalyticsFieldClass AppAnalytics)
        {
            AppAnalytics.val = @"";
        }
        public static string GetErrorFormMessage(AppPress p)
        {
            var message = "";
            if (p.sessionData.UserData.ContainsKey(AppPressKeys.ErrorFormException))
            {
                var cEx = (CustomizeException)p.sessionData.UserData[AppPressKeys.ErrorFormException];
                string errorMsg1 = cEx.Message;
                string errorMsg2 = "StackTrace:<br/>" + cEx.StackTrace;
                if (!cEx.InnerExMessage1.IsNullOrEmpty())
                {
                    errorMsg1 += "<br/><br/> InnerException1: " + cEx.InnerExMessage1;
                    errorMsg2 += "<br/><br/> InnerStackTrace1: " + cEx.InnerExStackTrace1;
                }
                if (!cEx.InnerExMessage2.IsNullOrEmpty())
                {
                    errorMsg1 += "<br/><br/> InnerException2: " + cEx.InnerExMessage2;
                    errorMsg2 += "<br/><br/> InnerStackTrace2: " + cEx.InnerExStackTrace2;
                }
                message = p.PageURL["Message"];
                if (message != null)
                    message += "<br/>";
                else
                    message = "";
                message += errorMsg1;
                message += "<br/><span style='color:#366092; display:none'>" + errorMsg2.Replace("\r\n", "<br/>") + "</span>";
            }
            else
            {
                message = p.PageURL["Message"];
            }
            return message;
        }
        public static List<FormData> Domain(AppPressDemo a, MasterClass.MasterContentAreaFieldClass Content)
        {
            var formDatas = Content.GetMasterContainer(a);
            var formName = formDatas[0].GetFormName();
            if (Content.FormData.Title.val == null)
                // Title can be set in Init for Form
                Content.FormData.Title.val = AppPressDemo.InsertSpacesBetweenCaps(formName);
            a.ExecuteJSScript("AppPressOpenMenu('" + formName + "');");
            return formDatas;
        }
        public static List<FormData> Domain(AppPressDemo a, NonSecureMasterClass.MasterContentAreaFieldClass Content)
        {
            return Content.GetMasterContainer(a);
        }
        public static void Init(AppPressDemo p, NonSecureMasterClass NonSecure)
        {
            NonSecure.ProductLogo.val = "Resources/img/ProductLogo.png";
        }
        public static void Init(AppPressDemo p, MasterClass Master)
        {
            var adminLogin = p.appPressDemoSessionData.isAdmin;
            var sessionData = p.appPressDemoSessionData;
            var Employeeid = p.loginUserId;
            Master.ProductIcon.val = "Resources/img/ProductIcon.png";
            Master.ProductLogo.val = "Resources/img/ProductLogo.png";
            Master.ProductURL.val = AppPressApplication.Settings.ProductURL;
            Master.ProductName.val = AppPressApplication.Settings.ProductName;
            Master.EmployeeName.val = sessionData.loginName;
            Master.EmployeeImgUrl.val = sessionData.loginImgUrl;
            Master.EmployeeEmail.val = sessionData.email;
        }
        public static void OnClick(AppPressDemo p, ContactUsClass.SendMessageFieldClass SendMessage)
        {
            var contactUs = SendMessage.FormData;
            contactUs.Validate();
            var message = "";
            message += "Name: " + contactUs.YourName.val + "\n";
            message += "Company: " + contactUs.YourCompany.val + "\n";
            message += "Email: " + contactUs.YourEmail.val + "\n";
            message += "Current HRIS System: " + contactUs.CurrentHRISSystem.val + "\n";
            message += "Current Pyroll System: " + contactUs.CurrentPayrollSystem.val + "\n";
            message += "Message: " + contactUs.Message.val + "\n";
            p.SendEmail("info@sysmates.com", contactUs.YourEmail.val, "HRMates Contact Us Request From Demo", message, null, null, false);
            p.AlertMessage("Thank you for Contacting SysMates Technologies. We will be contacting you shortly.");
        }
    }
}
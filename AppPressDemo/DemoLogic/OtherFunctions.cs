/* Copyright SysMates Technologies Pte. Ltd. */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data;
using ApplicationClasses;
using AppPressFramework;

namespace Application
{
    public partial class AppLogic
    {
        public static void Init(AppPressDemo a, OtherFunctionsClass OtherFunctions)
        {
            OtherFunctions.SendEmail.Help = "Shortcut Key Ctrl+S";
            // Change Title of Form
            ((MasterClass)OtherFunctions.FormDataContainer).Title.val = "Other Functions with Title Change";
        }
        public static void OnClick(AppPressDemo a, OtherFunctionsClass.AlertMessageFieldClass AlertMessage)
        {
            a.AlertMessage("This is a Alert Message");
        }
        public static void OnClick(AppPressDemo a, OtherFunctionsClass.PromptClientFieldClass PromptClient)
        {
            a.PromptClient("Press OK to Continue Execution");
            a.AlertMessage("You pressed Ok");
        }
        public static void OnClick(AppPressDemo a, OtherFunctionsClass.SendEmailFieldClass SendEmail)
        {
            SendEmail.FormData.Validate();
            a.PromptClient("Do you really want to send the Email to " + SendEmail.FormData.SendEmail.val);
            a.SendEmail(SendEmail.FormData.SendEmail.val, SendEmail.FormData.SendEmail.val, "Test Email From AppPress Demo", "Test Email Content From AppPress Demo", null, null, false);
            a.AlertMessage("Email has been Send");
        }
    }
}

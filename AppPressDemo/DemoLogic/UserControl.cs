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
        public static void Init(AppPressDemo a, UserControlsClass UserControls)
        {
            UserControls.Discussion.Help = "Discussion between Login user and Reporting Manager";
        }
        public static void OnClick(AppPressDemo a, MasterClass.UserControlsFieldClass UserControls)
        {
            // Redirect with Data with Row of Id 1
            UserControlsClass.Redirect(a, "6", null);
        }
    }
}

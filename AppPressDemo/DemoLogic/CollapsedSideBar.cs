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
        public static void Init(AppPressDemo a, CollapsedSideBarClass CollapsedSideBar)
        {
            a.ExecuteJSScript("setTimeout('$(\\\'.sidebar-toggle\\\').click()',1);");
        }
    }
}

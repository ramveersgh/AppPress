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
        public static void Init(AppPressDemo a, NavigationFunctionsClass NavigationFunctions)
        {
            NavigationFunctions.RedirectFunction.FieldLabel = "Redirect to UIElements";
            NavigationFunctions.PopupFunction.FieldLabel = "Popup UIElements";
        }
        public static void OnClick(AppPressDemo a, NavigationFunctionsClass.RedirectFunctionFieldClass RedirectFunction)
        {
            UIElementsClass.Redirect(a, null, null);
        }
        public static void OnClick(AppPressDemo a, NavigationFunctionsClass.PopupFunctionFieldClass PopupFunction)
        {
            UIElementsClass.Popup(a, null, null);
        }

    }
}

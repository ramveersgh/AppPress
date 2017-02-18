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
        public static void OnClick(AppPressDemo a, MiscellaneousClass.AddOptionFieldClass AddOption)
        {
            AddOptionClass.Popup(a, null, null);
        }
        public static void OnClick(AppPressDemo a, AddOptionClass.AddFieldClass Add)
        {
            Add.FormData.Save(a);
            a.ClosePopup();
            var fieldValue = ((MiscellaneousClass)Add.FormData.FormDataPopupCaller).Pickone;
            fieldValue.Refresh(a);
        }
    }
}

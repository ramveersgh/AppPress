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
        public static void Init(AppPressDemo a, AutoBindingClass AutoBinding)
        {
            AutoBinding.Search.Help = "Search the Grid on Animal and Remark Columns. To Test Type 'a' and press enter.";
            AutoBinding.Grid.Help = "Content of this grid get bound automatically to search field as in Domain of Grid Search field is used.";
            AutoBinding.AddGrid.Hidden = FieldHiddenType.Hidden;
        }
        public static void Calc(AppPressDemo a, AutoBindingClass.XFieldClass X)
        {
            X.Help = "Field Help Bound to Field: X*2=" + (X.val == null ? "NA" : (X.val * 2).ToString());
        }
        public static void Calc(AppPressDemo a, AutoBindingClass.XYFieldClass XY)
        {
            // Using value of other fields in the form automatically binds the field to other fields
            var AutoBinding = XY.FormData;
            if (AutoBinding.X.val == null || AutoBinding.Y.val == null)
                XY.val = null;
            else
                XY.val = AutoBinding.X.val * AutoBinding.Y.val;
        }
        public static void Calc(AppPressDemo a, AutoBindingClass.XPlusYFieldClass XPlusY)
        {
            var AutoBinding = XPlusY.FormData;
            if (AutoBinding.X.val == null || AutoBinding.Y.val == null)
            {
                if (a.CallReason != CallReasonType.PageLoad)
                    XPlusY.Error = "Both X and Y should not be Blank";
                XPlusY.val = null;
            }
            else
                XPlusY.val = AutoBinding.X.val + AutoBinding.Y.val;
            XPlusY.Help = "When X or Y is changed bound fields are automtically refreshed. Works with all field Types";
        }
        public static string Options(AppPressDemo a, AutoBindingClass.SubValuesFieldClass SubValues)
        {
            // Using value of "Values" field automatically binds the SubValues to other Values
            SubValues.Help = "When Values is changed bound fields are automatically refreshed.";
            SubValues.val = null; // discard existing value
            if (SubValues.FormData.Values.val == null)
                return null;
            return @"Select Id,Value From ""demo.lookup.PickValues.SubPickValues"" Where PickValuesId=" + SubValues.FormData.Values.val;
        }
        public static string Domain(AppPressDemo a, AutoBindingClass.GridFieldClass Grid)
        {
            var search = Grid.FormData.Search.val;
            if (search == null)
                return null;
            var qry = @"
                Select [demo.formcontainerdata].Id,Animal,Count,Remark 
                From [demo.formcontainerdata] 
                Join [demo.Lookup.PickValues] On [demo.Lookup.PickValues].id=[demo.formcontainerdata].Animal
                Where remark like '%" + a.EscapeSQLString(search) + "%' or  [demo.Lookup.PickValues].Value like '%" + a.EscapeSQLString(search) + "%'";
            return qry;
        }


    }

}

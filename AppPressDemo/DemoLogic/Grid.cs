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
using System.IO;

namespace Application
{
    public partial class AppLogic
    {
        public static void Init(AppPressDemo p, GridClass Grid)
        {
            Grid.Grid.Help = "Help Text for the Grid";
        }
        public static string Domain(AppPressDemo a, GridClass.NestedGridPopupClass.FeedConsumptionCurrentWeekFieldClass FeedConsumptionCurrentWeek)
        {
            var topOfWeek = DateTime.Now.AddDays(-(int)DateTime.Now.DayOfWeek);
            var query = "Select * From \"demo.feedconsumption\" Where Date between '" + topOfWeek.ToString(DAOBasic.DBDateFormat) + "' and '" + topOfWeek.AddDays(7).ToString(DAOBasic.DBDateFormat) + "' and Animal=" + FeedConsumptionCurrentWeek.FormData.Animal.val;
            return query;
        }
        public static string Domain(AppPressDemo a, GridClass.NestedGridRowClass.FeedConsumptionCurrentWeekFieldClass FeedConsumptionCurrentWeek)
        {
            var topOfWeek = DateTime.Now.AddDays(-(int)DateTime.Now.DayOfWeek);
            var query = "Select * From \"demo.feedconsumption\" Where Date between '" + topOfWeek.ToString(DAOBasic.DBDateFormat) + "' and '" + topOfWeek.AddDays(7).ToString(DAOBasic.DBDateFormat) + "' and Animal=" + FeedConsumptionCurrentWeek.FormData.Animal.val;
            return query;
        }
        public static void OnChange(AppPressDemo p, GridClass.AllowMultiSelectFieldClass AllowMultiSelect)
        {
            AllowMultiSelect.FormData.GridUserControlledMultiSelect.AllowMultiSelect(AllowMultiSelect.val);
            AllowMultiSelect.FormData.ModifyGridUserControlledMultiSelect.FieldLabel = AllowMultiSelect.val ? "Modify Multiple" : "Modify";
        }
        public static void OnClick(AppPressDemo p, GridClass.HideRemarkColumnFieldClass HideRemarkColumn)
        {
            var Grid = HideRemarkColumn.FormData;
            Grid.GridWithHiddenColumnAndTotalRow.HideColumn("Remark");
            Grid.GridWithHiddenColumnAndTotalRow.Refresh(p);
        }
        public static void Calc(AppPressDemo a, GridClass.GridWithHiddenColumnAndTotalRowFieldClass GridWithHiddenColumnAndTotalRow)
        {
            // Create a total formData and add it to the grid. 
            // This is done in Calc so that the totals of any calculated fields in grid are also done
            var totalFormData = new GridClass.GridWithHiddenColumnAndTotalRowRowClass(a);
            totalFormData.Animal.val = "<strong>Total</strong>";
            var formDatas = GridWithHiddenColumnAndTotalRow.val;
            totalFormData.Count.val = formDatas.Sum(t => ((GridClass.GridWithHiddenColumnAndTotalRowRowClass)t).Count.val);
            formDatas.Insert(0, totalFormData);
            GridWithHiddenColumnAndTotalRow.val = formDatas;
        }
        public static List<FormData> Domain(AppPressDemo a, GridClass.GridWithHiddenColumnAndTotalRowFieldClass GridWithHiddenColumnAndTotalRow)
        {
            var formDatas = GridWithHiddenColumnAndTotalRow.ReadFormDatas(a, @"
                Select ""demo.Lookup.PickValues"".Value as Animal,  ""demo.formcontainerdata"".Count,Remark
                From ""demo.formcontainerdata""
                Left Outer Join ""demo.Lookup.PickValues"" On ""demo.Lookup.PickValues"".id = ""demo.formcontainerdata"".Animal
                ");
            return formDatas;
        }
        public static void OnClick(AppPressDemo a, GridClass.DownloadGridAsCSVFieldClass DownloadGridAsCSV)
        {
            var csv = DownloadGridAsCSV.FormData.GridWithHiddenColumnAndTotalRow.GetCSV(a);
            var fileName = Path.GetTempFileName();
            File.WriteAllText(fileName, csv);
            a.DownloadFile(fileName, "Grid Details as on " + DateTime.Now.Date.ToString(AppPressApplication.Settings.NetDateTimeFormat) + ".csv", "text/csv");
        }
        public static void OnClick(AppPressDemo a, GridClass.MultipleModifyFieldClass MultipleModify)
        {
            MultipleModifyClass.Popup(a, null, null);
        }
        public static void Init(AppPressDemo a, MultipleModifyClass MultipleModify)
        {
            var gridMultiSelect = ((GridClass)MultipleModify.FormDataPopupCaller).GridMultiSelect;
            var selectedFormDatas = gridMultiSelect.GetSelection();
            MultipleModify.Animals.val = string.Join(", ", selectedFormDatas.Select(t => ((GridClass.GridMultiSelectRowClass)t).Animal.GetOption(a).value));
        }
        public static void OnClick(AppPressDemo a, MultipleModifyClass.SaveFieldClass Save)
        {
            var MultipleModify = Save.FormData;
            var selectedFormDatas = ((GridClass)MultipleModify.FormDataPopupCaller).GridMultiSelect.GetSelection();
            try
            {
                a.BeginTrans();
                foreach (var formData in selectedFormDatas)
                {
                    if (MultipleModify.Count.val != AppPress.DecimalMultiple)
                        a.ExecuteNonQuery(@"Update ""demo.formcontainerdata"" Set Count=" + MultipleModify.Count.val + " Where Id=" + formData.id);
                }
                a.CommitTrans();
            }
            catch
            {
                a.RollbackTrans();
                throw;
            }
            a.ClosePopup();
            a.PageRefresh();
        }
    }
}
    



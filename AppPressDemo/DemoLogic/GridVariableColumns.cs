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
        public static string Domain(AppPressDemo a, GridVariableColumnsClass.AnimalFarmRowClass.FeedConsumptionCurrentWeekFieldClass FeedConsumptionCurrentWeek)
        {
            var lastDate = a.ExecuteDateTime("Select Max(Date) From \"demo.feedconsumption\" Where Animal=" + FeedConsumptionCurrentWeek.FormData.Animal.val);
            if (lastDate == null)
                lastDate = new DateTime(2016,11,1); // Start Recording Feed From This Day
            else
                lastDate = lastDate.Value.AddDays(1);
            a.BeginTrans();
            try
            {
                while (lastDate <= DateTime.Today)
                {
                    a.ExecuteNonQuery("Insert into \"demo.feedconsumption\" (Date,Animal) Select '" + lastDate.Value.ToString(DAOBasic.DBDateFormat) + "' Date, " + FeedConsumptionCurrentWeek.FormData.Animal.val + " Animal");
                    lastDate = lastDate.Value.AddDays(1);
                }
                a.CommitTrans();
            }
            catch
            {
                a.RollbackTrans();
                throw;
            }
            var query = "Select Id,Date, Weight From \"demo.feedconsumption\" Where Animal=" + FeedConsumptionCurrentWeek.FormData.Animal.val + " and Date between '" + DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek).ToString(DAOBasic.DBDateFormat) + "' and '" + DateTime.Today.ToString(DAOBasic.DBDateFormat) + "'";
            return query;
        }
        public static string Domain(AppPressDemo a, GridVariableColumnsClass.AnimalFarmPopupClass.FeedConsumptionCurrentWeekFieldClass FeedConsumptionCurrentWeek)
        {
            if (FeedConsumptionCurrentWeek.FormData.Animal.val == null)
            {
                FeedConsumptionCurrentWeek.Hidden = FieldHiddenType.Hidden;
                return null;
            }
            var query = "Select Id,Date, Weight From \"demo.feedconsumption\" Where Animal=" + FeedConsumptionCurrentWeek.FormData.Animal.val + " and Date between '" + DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek).ToString(DAOBasic.DBDateFormat) + "' and '" + DateTime.Today.ToString(DAOBasic.DBDateFormat) + "'";
            return query;
        }
    }
}

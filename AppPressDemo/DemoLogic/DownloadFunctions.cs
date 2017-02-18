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
        public static void OnClick(AppPressDemo a, DownloadFunctionsClass.DownloadEmployeeListFieldClass DownloadEmployeeList)
        {
            a.DownloadCSV(@"
                Select FirstName,MiddleName,LastName, ""lookup.gender"".Gender, Format(DateOfBirth,'" + AppPressApplication.Settings.SQLDateFormat + @"') DateOfBirth
                From ""application.users""
                Left Outer Join ""lookup.gender"" on ""lookup.gender"".id=""application.users"".Gender
                ", "Employee List as on " + DateTime.Today.ToString(AppPressApplication.Settings.NetDateFormat));
        }
        public static void OnClick(AppPressDemo a, DownloadFunctionsClass.ViewEmployeeListFieldClass DownloadEmployeeList)
        {
            a.ViewHTML(@"
                Select FirstName,MiddleName,LastName, ""lookup.gender"".Gender, Format(DateOfBirth,'" + AppPressApplication.Settings.SQLDateFormat + @"') DateOfBirth 
                From ""application.users"" 
                Left Outer Join ""lookup.gender"" on ""lookup.gender"".id=""application.users"".Gender
                ", "Employee List as on " + DateTime.Today.ToString(AppPressApplication.Settings.NetDateFormat),true,null);
        }
        public static void OnClick(AppPressDemo a, DownloadFunctionsClass.DownloadFileFieldClass DownloadFile)
        {
            a.DownloadFile(1);
        }
        public static void OnClick(AppPressDemo a, DownloadFunctionsClass.DownloadMultipleFilesFieldClass DownloadMultipleFile)
        {
           // a.DownloadMultipleFile(1);
        }
    }
}

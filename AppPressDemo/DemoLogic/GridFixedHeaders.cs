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
        public static string Domain(AppPressDemo a, GridFixedHeadersClass.EmployeesFieldClass Employees)
        {
            var query = @"Select ""application.users"".Id, Photoupload, [dbo].[FullName](FirstName,MiddleName,LastName) Name, ""lookup.gender"".Gender,DateOfBirth, CASE WHEN dateadd(year, datediff (year, DateOfBirth, getdate()), DateOfBirth) > getdate()
            THEN datediff(year, DateOfBirth, getdate()) - 1
            ELSE datediff(year, DateOfBirth, getdate())
       END Age From ""application.users""
                Left join  ""lookup.gender"" on ""lookup.gender"".id = ""application.users"".Gender";
            return query;
        }
    }
}

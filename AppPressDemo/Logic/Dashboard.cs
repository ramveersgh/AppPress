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
        public static void Init(AppPressDemo p, DashboardClass Dashboard)
        {
            Dashboard.Description.val = @"<span style='font-size:32px'>Advanced Skin Transformation <a href='https://docs.google.com/document/d/1goW6BxoZp1WK12ZCcmg8RqVgADifn8yYZSZoGjpUeRo/edit#heading=h.p0odykvmemlj' target='_blank'><i class=""fa fa-question-circle""></i></a></span>";
            Dashboard.Logins.val = p.ExecuteDecimal(@"Select count(*) 
                From application_audit 
                Where AuditType=" + (int)AuditType.Login);
            Dashboard.Help.val = "http://AppPress.in";
        }
        public static string Domain(AppPressDemo p, DashboardClass.HeadCountByGenderFieldClass HeadCountByGender)
        {

            var query = @"
                Select A.*,""lookup.Gender"".Gender From (
                    Select ""application.users"".Gender Id, Count(*) HeadCount
                    From ""application.users""
                    Group by ""application.users"".Gender
			        )A
                Left Outer Join ""lookup.Gender"" ON ""lookup.Gender"".id = A.Id
";
            return query;
        }
        public static void OnClick(AppPressDemo a, DashboardClass.GenderDetailsFieldClass GenderDetails)
        {
            var query = @"
                Select Id, [dbo].[FullName](FirstName,MiddleName,LastName) Name 
                From ""application.users"" 
                Where Gender=" + GenderDetails.FormData.Gender.val;
            var gender = a.ExecuteString(@"Select Gender From ""lookup.gender"" Where Id=" + GenderDetails.FormData.Gender.val);
            a.AlertMessage(a.GetHtmlTableFromQuery(query, false, null), "Users: " + gender, 0, true);
        }

    }
}

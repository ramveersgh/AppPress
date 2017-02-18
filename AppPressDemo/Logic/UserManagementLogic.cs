/* Copyright SysMates Technologies Pte. Ltd. */
using System;
using System.Web;
using ApplicationClasses;
using AppPressFramework;
using System.Net;
using System.Collections.Generic;
using System.IO;
using System.Web.Script.Serialization;

namespace Application
{
    public partial class AppLogic
    {
        public static string Domain(AppPressDemo a, UserManagementClass.UsersFieldClass Users)
        {
            var query = @"Select ""application.users"".Id, Photoupload, [dbo].[FullName](FirstName,MiddleName,LastName) Name, ""lookup.gender"".Gender,DateOfBirth,PersonalEmail,MobileNumber,
                CASE WHEN dateadd(year, datediff (year, DateOfBirth, getdate()), DateOfBirth) > getdate()
                 THEN datediff(year, DateOfBirth, getdate()) - 1
                 ELSE datediff(year, DateOfBirth, getdate())
            END Age From ""application.users""
                     Left join  ""lookup.gender"" on ""lookup.gender"".id = ""application.users"".Gender";
            return query;
        }
        public static void OnClick(AppPressDemo a, UserManagementClass.ChangePasswordFieldClass ChangePassword)
        {
            var UserManagement = ChangePassword.FormData;
            var employeeId = UserManagement.Users.GetSingleSelection().id;
            ChangePasswordClass.Popup(a, employeeId, null);
        }
        public static void Init(AppPressDemo a, UserManagementClass.UsersRowClass UsersRow)
        {
            if (UsersRow.Photoupload.val != null)
                UsersRow.Photoupload.val = "<img src='" + a.GetFileUploadImageUrl(int.Parse(UsersRow.Photoupload.val), 50, null) + "' style='width:50px'></img>";
        }
    }
}

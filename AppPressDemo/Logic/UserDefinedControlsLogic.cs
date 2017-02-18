using System;
/* Copyright SysMates Technologies Pte. Ltd. */
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data;
using System.Globalization;
using ApplicationClasses;
using AppPressFramework;

namespace Application
{
    public partial class AppLogic
    {
        public static int GetDiscussionUnreadCount(AppPressDemo p, string discussionId, string employeeId)
        {
            if (discussionId == null)
                return 0;
            return p.ExecuteInt32(@"
                Select Count(*) 
                From Discussions 
                Where DiscussionMasterId=" + discussionId + @" and Id not in (
                    Select DiscussionId From ""Discussions.Read"" Where EmployeeId=" + employeeId + @" and DiscussionId=Discussions.id
                    ) and Discussions.[By]<>" + employeeId).Value;
        }
        public static string Domain(AppPressDemo p, DiscussionClass.DiscussionFieldClass DiscussionRow)
        {
            var discussion = DiscussionRow.FormData;
            var query = @"Select * From Discussions Where DiscussionMasterId=" + DiscussionRow.FormData.id;
            p.BeginTrans();
            try
            {
                p.ExecuteNonQuery(@"
                    Insert Into ""Discussions.Read"" (EmployeeId,DiscussionId,Time) 
                    Select " + p.loginUserId + " EmployeeId, Id, '" + DateTime.Now.ToString(DAOBasic.DBDateTimeFormat) + @"' Time 
                    From Discussions Where DiscussionMasterId=" + DiscussionRow.FormData.id + @" and id not in (
                        Select discussionId From ""Discussions.Read"" Where DiscussionId in (" + query.Replace("*", "id") + ") and EmployeeId=" + p.loginUserId + @"
                        ) and Discussions.[By]<>" + p.loginUserId);
                p.CommitTrans();
            }
            catch
            {
                p.RollbackTrans();
                throw;

            }
            return query;
        }

        public static List<Option> Options(AppPressDemo p, GetCurrentEmployeeOptionsClass.EmployeesFieldClass Employees)
        {
            var id = Employees.val;
            var includeExEmployees = Employees.GetUserControlParameters().ContainsKey("IncludeExEmployees");
            var whereClause = "";
            var tableName = "\"application.users\"";
            if (Employees.AutoCompleteTerm != null)
                whereClause += " or 1 = 1 ";
            if (id != null && id.Length > 0)
                whereClause = " ( Id = " + id + " " + whereClause + ") ";
            var query = @"
                Select Id,[dbo].[FullName](FirstName,MiddleName,LastName) EmployeeName,Email 
                From " + tableName + @" 
                Where 1=1 and " + whereClause;
            query += @" And [dbo].[FullName](FirstName,MiddleName,LastName) Like '" + Employees.AutoCompleteTerm + @"%' Order By[dbo].[FullName](FirstName,MiddleName,LastName) "; //PickoneFieldValue
            var dr = p.ExecuteQuery(query);
            var options = new List<Option>();
            try
            {
                while (dr.Read())
                {
                    var val = "";
                    if (!dr.IsDBNull(1))
                    {
                        if (Employees.IsStatic)
                            val = "<span>" + dr.GetString(1) + " - <span style='font-size:9px'>" + (dr.IsDBNull(2) ? "" : dr.GetString(2)) + "</span></span>";
                        else
                            val = dr.GetString(1) + "@--@" + (dr.IsDBNull(2) ? "" : dr.GetString(2));
                    }
                    options.Add(new Option { id = dr.GetInt64(0).ToString(), value = val });
                }
            }
            finally
            {
                dr.Close();
            }

            return options;
        }
        public static void Calc(AppPressDemo p, ChangeHistoryAnchorClass.ChangeHistoryFieldClass ChangeHistory)
        {
            var formData = ChangeHistory.FormData;
            if (formData.IsNew)
                ChangeHistory.Hidden = FieldHiddenType.Hidden;
        }
        public static void Init(AppPressDemo p, ChangeHistoryClass ChangeHistory)
        {
            var formData = ChangeHistory.FormDataPopupCaller;
            if (formData != null && !formData.IsNew)
                ChangeHistory.ChangeHistory.val = p.GetHtmlTableFromQuery(@"
                    Select [dbo].[FullName](FirstName,Middlename,LastName) as ""By"",time as ""When"",""Change""
                    From ""application_audit"" 
                    Left Outer Join ""application.users"" On ""application.users"".id=LoginUserId
                    Where TableName='" + formData.GetTableName() + "' and auditType in (100,102) and rowId=" + formData.id + @"
                ", false, null);
        }
        public static void OnClick(AppPressDemo p, ChangeHistoryAnchorClass.ChangeHistoryFieldClass ChangeHistory)
        {
            ChangeHistoryClass.Popup(p, null, null);
        }
        public static void OnClick(AppPressDemo p, DiscussionAnchorClass.DiscussionFieldClass Discussion)
        {
            if (Discussion.val == null)
            {
                p.BeginTrans();
                try
                {
                    var tableName = Discussion.FormData.GetTableName();
                    Discussion.val = p.ExecuteIdentityInsert("Insert into discussionmaster(TableName) Values('" + tableName + "')", "discussionmaster").ToString();
                    p.ExecuteNonQuery("Update `" + tableName + "` Set Discussion=" + Discussion.val + " Where Id=" + Discussion.FormData.id);
                    p.CommitTrans();
                }
                catch
                {
                    p.RollbackTrans();
                    throw;
                }
            }
            DiscussionClass.Popup(p, Discussion.val, null);
        }
        public static void Calc(AppPressDemo p, DiscussionAnchorClass.DiscussionFieldClass Discussion)
        {
            var unreadCount = GetDiscussionUnreadCount(p, Discussion.val, p.loginUserId);
            Discussion.FieldLabel = "<i class='fa fa-comment-o'></i>";
            if (unreadCount != 0)
                Discussion.FieldLabel += unreadCount + " Unread";
        }
        public static void OnClick(AppPressDemo p, DiscussionClass.AddDiscussionFieldClass AddDiscussion)
        {
            if (AddDiscussion.FormData.AddText.val == null || AddDiscussion.FormData.AddText.val.Trim().Length == 0)
                return;
            p.BeginTrans();
            try
            {
                p.ExecuteIdentityInsert("Insert into discussions(DiscussionMasterId,\"By\",\"On\",Comment) Values(" + AddDiscussion.FormData.id + "," + p.loginUserId + ",'" + DateTime.Now.ToString(DAOBasic.DBDateTimeFormat) + "','" + p.EscapeSQLString(AddDiscussion.FormData.AddText.val) + "')", "discussions");
                p.CommitTrans();
            }
            catch
            {
                p.RollbackTrans();
                throw;
            }
            AddDiscussion.FormData.Refresh();
        }
        public static void Init(AppPressDemo p, DiscussionClass.ParticipantsRowClass ParticipantsRow)
        {
            p.appPressDemoSessionData.AddSecureUrl(ParticipantsRow.Photo.val);
        }
        public static string Domain(AppPressDemo p, DiscussionClass.ParticipantsFieldClass Participants)
        {
            string query = null;
            var baseFormData = Participants.FormData.IsPopup ? Participants.FormData.FormDataPopupCaller : Participants.FormData;
            if (baseFormData.GetType() == typeof(UserControlsClass))
            {
                query = @"Select Case When PhotoUpload Is Null 
                             Then
                                 Case When gender = 1 Then
                                     '" + AppPress.GetBaseUrl() + @"Resources/img/img_Female.jpg'
                                 Else
                                     '" + AppPress.GetBaseUrl() + @"Resources/img/img_Male.jpg' End
                             Else
                                 Concat('" + AppPress.GetBaseUrl() + AppPress.GetDefaultAspx() + @"?getFile=&id=', PhotoUpload, '&width=100')
                    End as Photo, [dbo].[FullName](FirstName,MiddleName,LastName) Name,  case when id=" + p.loginUserId + @" then 'Self' else 'ReportingTo' end Level, Isnull(Email,PersonalEmail) Email
                    From ""Application.Users""
                    Where Id in (" + p.loginUserId + @"," + ((UserControlsClass)baseFormData).ReportingTo.val + ")";
            }
            return query;
        }
        public static void Init(AppPressDemo p, DiscussionClass.DiscussionRowClass discussionRow)
        {
            if (discussionRow.By.val == p.loginUserId)
            {
                discussionRow.ByPhoto.Hidden = FieldHiddenType.Hidden;
                discussionRow.By.Hidden = FieldHiddenType.Hidden;
            }
            else
                discussionRow.ByPhoto.val = GetEmployeePhotoUrl(p, p.ExecuteInt32("Select PhotoUpload From \"application.users\" Where Id=" + discussionRow.By.val), p.ExecuteString("Select Gender From \"application.users\" Where Id=" + discussionRow.By.val));
            discussionRow.Direction.val = p.loginUserId == discussionRow.By.val ? "right" : "left";
            discussionRow.OtherDirection.val = p.loginUserId == discussionRow.By.val ? "left" : "right";
        }

        public static string GetEmployeePhotoUrl(AppPressDemo p, int? photoFileId, string gender)
        {
            var imgUrl = "";
            if (photoFileId == null)
            {
                if (gender == "2")
                    imgUrl += AppPress.GetBaseUrl() + @"Resources/img/img_Male.jpg";
                else if (gender == "1")
                    imgUrl += AppPress.GetBaseUrl() + @"Resources/img/img_Female.jpg";
                else
                    imgUrl += AppPress.GetBaseUrl() + @"Resources/img/img_Default.jpg";
            }
            else
                imgUrl += p.GetFileUrl(photoFileId.Value);
            return imgUrl;
        }
    }
}
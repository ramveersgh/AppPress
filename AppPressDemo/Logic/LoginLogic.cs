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

        [Serializable]
        public class AppPressDemoSessionData : SessionData
        {
            public bool isAdmin = false;
            public string loginName;
            public string loginImgUrl;
            private bool mobile = false;
            public bool FromMobile { get { return mobile; } set { mobile = value; if (mobile) isAdmin = false; } }
            public AppPressDemoSessionData(AppPressDemo p, string loginUserId,  string email)
                : base(email, loginUserId, null)
            {
                this.email = email;
                if (loginUserId != null)
                {
                    this.loginName = p.ExecuteString("Select [dbo].[FullName](FirstName,MiddleName,LastName) From \"application.users\" Where Id = " + loginUserId);
                    string imgUrlQry = @"
                        Select 
                            Case When ""Application_Files"".Id Is Null 
    	                    Then 
		                    Case When CV.gender = 1 Then
			                    '" + AppPress.GetBaseUrl() + @"Resources/img/img_Female.jpg'  
		                    Else
			                    '" + AppPress.GetBaseUrl() + @"Resources/img/img_Male.jpg' End
	                        Else 
		                    Concat('" + AppPress.GetBaseUrl() + AppPress.GetDefaultAspx() + @"?GetFile=&width=100&id=',Application_Files.Id) 
                            End as Image 
                        From ""application.users"" CV 
                        Left Join ""Application_Files"" On ""Application_Files"".Id = CV.PhotoUpload
                        Where CV.Id = " + loginUserId;
                    this.loginImgUrl = p.ExecuteString(imgUrlQry);
                    // Add the URL to session for inbuilt security
                    this.AddSecureUrl(loginImgUrl);
                }
            }
        }
        private static string CookiePrefix(AppPressDemo p)
        {
            var prefix = AppPress.GetBaseUrl();
            return prefix;
        }


        public static void Calc(AppPressDemo a, LoginClass.AppAnalyticsFieldClass AppAnalytics)
        {

        }
        public static void Init(AppPressDemo p, LoginClass login)
        {
            p.ExecuteJSScript("setCookie('isAdminDropDown', false, 1);");
            login.Email.SetFocus();
            login.ProductName.val = AppPressApplication.Settings.ProductName;
            login.DemoLogin.val = @"            <style>
                table, th, td {
                    border: 1px solid lightgrey;
                    border-collapse: collapse;
                    padding: 2px;
                }
            </style>
            <hr/>
            <table style=""width:100%"">
                <tbody>
                    <tr>
                        <td><button class=""btn btn-primary btn-block btn-flat"" onclick = ""$('input[placeholder=Email]').val('karuna@wellruncompany.com');$('input[placeholder=Password]').val('123');$(JQueryEscape('#" + login.Login.GetHtmlId() + @"')).click();"">Click here for Admin Login</button></td>
                    </tr>
                </tbody>
            </table>
";
                
            var cookiePrefix = CookiePrefix(p);
            var emailCookie = p.Request.Cookies[cookiePrefix + "Email"];
            if (emailCookie != null)
            {
                login.Email.val = emailCookie.Value;
                var passwordCookie = p.Request.Cookies[cookiePrefix + "Password"];
                if (passwordCookie != null)
                    login.Password.val = passwordCookie.Value;
                if (p.Request["FromSignout"] == null)
                {
                    //SetupSession(p, login, true, companyId);
                    login.RememberMe.val = true;
                    OnClick(p, login.Login);
                }
                else
                    login.RememberMe.val = true;
            }
        }
        public static void OnClick(AppPressDemo p, LoginClass.RememberMeFieldClass rememberMe)
        {
            if (!rememberMe.val)
            {
                try
                {
                    p.Response.Cookies["Email"].Expires = p.Response.Cookies["Password"].Expires = DateTime.Now.AddDays(-1);
                }
                catch { }
            }
        }
        public static void OnClick(AppPressDemo p, LoginClass.ForgotPasswordFieldClass emailField)
        {
            var login = (LoginClass)emailField.FormData;
            emailField.Validate();

            var f = new ForgotPasswordClass(p);
            f.Email.val = login.Email.val;
            //ForgotPasswordClass.Popup(p, null, null);
            f.Popup(p, new PopupParams { PopupWidth = 500, title = "Forgot Password" });
        }


        public static void OnClick(AppPressDemo p, ForgotPasswordClass.SendPasswordFieldClass SendPassword)
        {
            SendPassword.Validate();
            var ForgotPassword = SendPassword.FormData;
            var email = ForgotPassword.Email.val;
            var userId = ValidateUserAndGetPassword(p, email);
            var secureUrl = p.GetSecureUrl("ChangePassword", userId, userId, new TimeSpan(0, 12, 0, 0, 0));
            var employeeName = p.ExecuteString(@"Select IfNull(""application.users"".ShortName,[dbo].[FullName](FirstName,MiddleName,LastName)) From ""Application.Users"" Where Id=" + userId);
            var message = "<html><body style=\"font-family:Calibri; font-size:14px;\"> Dear " + employeeName + ",<br/><br/>Click on the following link to change your password.<br/><br/><a href=\"" + secureUrl + "\">" + secureUrl + "<a/>" +
                            "<br/><br/><b>This link will expire in 2 days.</b>.<br/>If you have not raised this request then kindly contact HRMates Support Team.<br><br/> Regards,<br/>HRMates Administrator<br/><a href=\"mailto:" + AppPressApplication.Settings.SupportEmail + "\">" + AppPressApplication.Settings.SupportEmail + "<a/></html></body>";
            p.SendEmail(email, null, "Forgot Password", message, null, null, true);
            p.AlertMessage("Mail sent successfully. Please check your mail for link to change Password.");
            p.ClosePopup();
        }

        public static string ValidateUserAndGetPassword(AppPressDemo p, string email)
        {
            email = email.Trim();
            email = p.EscapeSQLString(email);
            string query = "Select Id From \"application.users\" Where (Email='" + email + "' or PersonalEmail ='" + email + "' or ClientEmail ='" + email + "')";
            var userId = p.ExecuteScalar(query);

            if (userId == null)
                throw new AppPressException("Invalid Email address");
            return userId.ToString();
        }

        public static void OnChange(AppPressDemo p, LoginClass.EmailFieldClass Email)
        {
            var Login = Email.FormData;
            var email = Login.Email.val;
            var oldHidden = Login.Password.Hidden;
            Login.Password.Hidden = Login.ForgotPassword.Hidden = FieldHiddenType.None;
            if (email != null)
            {
                email = email.Trim().ToLower();
            }
            if (oldHidden != Login.Password.Hidden)
            {
                // Login.Refresh TBD ???
                Login.Password.Refresh(p);
                Login.ForgotPassword.Refresh(p);
                if (Login.Password.Hidden != FieldHiddenType.Hidden)
                    Login.Password.SetFocus();
            }
        }
        public static void OnClick(AppPressDemo p, LoginClass.LoginFieldClass loginField)
        {

            var login = loginField.FormData;
            var email = login.Email.val;
            var cookiePrefix = CookiePrefix(p);
            
            var password = login.Password.val;
            SetupSession(p, login, false);
            var emailCookie = p.Response.Cookies[cookiePrefix + "Email"];
            var passwordCookie = p.Response.Cookies[cookiePrefix + "Password"];
            if (login.RememberMe.val)
            {
                emailCookie.Value = email;
                passwordCookie.Value = password;
                emailCookie.Expires = passwordCookie.Expires = DateTime.Now.AddDays(5);
            }
            else
            {
                emailCookie.Expires = passwordCookie.Expires = DateTime.Now.AddDays(-1);
            }
            DashboardClass.Redirect(p, null, null);
        }
        public static void OnClick(AppPressDemo p, MasterClass.SignoutFieldClass signout)
        {
            var Master = signout.FormData;
            HttpContext.Current.Session.Abandon();
            var redirectParams = new RedirectParams();
            redirectParams.postParams.Add("FromSignout", "");

            // delete cookies
            var cookiePrefix = CookiePrefix(p);
            var emailCookie = p.Response.Cookies[cookiePrefix + "Email"];
            emailCookie.Expires = DateTime.Now.AddDays(-1);
            p.Response.Cookies.Add(emailCookie);
            var passwordCookie = p.Response.Cookies[cookiePrefix + "Password"];
            passwordCookie.Expires = DateTime.Now.AddDays(-1);
            p.Response.Cookies.Add(passwordCookie);

            LoginClass.Redirect(p, null, redirectParams);

        }
        public static void Init(AppPressDemo p, ChangePasswordClass ChangePassword)
        {
            ChangePassword.Employee.val = p.ExecuteString("Select [dbo].[FullName](FirstName,MiddleName,LastName) From \"application.users\" Where Id=" + ChangePassword.id);
        }
        public static void ChangePassword1(AppPressDemo p, string Value, string EmployeeId)
        {
            var tempValue = Value;
            tempValue = EmployeeId.ToString() + "_" + System.Text.Encoding.Default.GetString((byte[])p.ExecuteScalar("Select HASHBYTES('SHA2_256','" + p.EscapeSQLString(tempValue) + "')"));
            tempValue = p.ExecuteString("Select  HASHBYTES('SHA2_256','" + p.EscapeSQLString(tempValue) + "')");


            p.ExecuteNonQuery("Update \"application.users\" Set Password = '" + p.EscapeSQLString(tempValue) + "' Where Id = " + EmployeeId);

        }
        public static void OnClick(AppPressDemo p, ChangePasswordClass.ChangeFieldClass Change)
        {
            var ChangePassword = Change.FormData;
            ChangePassword.Validate();
            if (ChangePassword.NewPassword.val != ChangePassword.ConfirmPassword.val)
                throw new AppPressException("New Password and Confirm Password do not Match");
            try
            {
                p.BeginTrans();
                ChangePassword1(p, ChangePassword.NewPassword.val, ChangePassword.id);
                p.CommitTrans();
            }
            catch
            {
                p.RollbackTrans();
                throw;
            }

            p.AlertMessage("Your Password has been changed.");
            if (ChangePassword.PopupContainer != null)
                p.ClosePopup(); // if it is a popup
            else
            {
                var redirectParams = new RedirectParams();
                LoginClass.Redirect(p, null, redirectParams);
            }
        }

        public static void SetupSession(AppPressDemo p, LoginClass login, bool cookieLogin)
        {
            var email = login.Email.val;
            var password = login.Password.val;
            login.Validate();
            var employeeId = ValidateLogin(p, ref email, password, cookieLogin).ToString();
            p.sessionData = new AppPressDemoSessionData(p, employeeId, email);

        }

        internal static int ValidateLogin(AppPressDemo p, ref string email, string password, bool cookieLogin)
        {

            int employeeId = 0;
            var loginLink = "<br/><br/><a href='" + AppPress.GetDefaultAspx() + "?&FromSignout='>Login</a>";
            if (AppPressApplication.Settings.developer)
            {
                if (int.TryParse(email, out employeeId))
                {
                    var firstName = p.ExecuteString("Select FirstName from \"application.users\" Where Id = " + employeeId);
                    if (firstName == null)
                        throw new AppPressException("Could not find employee id. ");
                    email = "support+" + firstName.ToLower().Replace(" ", ".") + "." + employeeId + "@sysmates.com";
                }
            }
            if (employeeId <= 0)
            {
                var tempemail = email.Trim().ToLower();
                var ttttEmail = tempemail;

                var query = "Select id From \"application.users\" Where (Email='" + p.EscapeSQLString(tempemail) + "' or PersonalEmail ='" + p.EscapeSQLString(tempemail) + "')  ";
                var obj = p.ExecuteScalar(query);
                if (obj == null)
                {
                    if (cookieLogin)
                        return 0;
                    throw new AppPressException("Wrong Email. There is no account available with this email." + loginLink);
                }
                if (!AppPressApplication.Settings.developer)
                {
                    if (password != null)
                    {
                        var newPassword = obj.ToString() + "_" + System.Text.Encoding.Default.GetString((byte[])p.ExecuteScalar("Select  HASHBYTES('SHA2_256','" + p.EscapeSQLString(password) + "')"));
                        query += "and Password =  HASHBYTES('SHA2_256','" + p.EscapeSQLString(newPassword) + "')";
                    }
                    obj = p.ExecuteScalar(query);
                }
                if (obj == null)
                {
                    if (cookieLogin)
                        return 0;
                    throw new AppPressException("Wrong Email or Password." + loginLink);
                }
                employeeId = Convert.ToInt32(obj);

            }

            return employeeId;
        }

    }
}


/// update "application.users" set password= HASHBYTES('SHA2_256',concat(id,'_', HASHBYTES('SHA2_256','123')))
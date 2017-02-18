/********************************************************************************
 * Copyright Sysmates Pte Limited
 *******************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Web;
using System.Globalization;
using System.Configuration;
using System.Reflection;
using System.Security.Cryptography;
using MySql.Data.MySqlClient;
using System.Text;
using System.IO;
using System.CodeDom.Compiler;
using Microsoft.CSharp;
using System.Text.RegularExpressions;
using org.apache.fop.apps;
using java.io;
using javax.xml.transform;
using javax.xml.transform.stream;
using javax.xml.transform.sax;
using org.apache.commons.io;
using System.Net;
using System.Runtime.Serialization;
using System.Data.SqlClient;
using System.Runtime.Serialization.Json;
using System.Collections.Specialized;
using System.Net.Mail;

namespace AppPressFramework
{
    internal class Markers
    {
        internal const string ScriptBeginMarker = "<!--|ScriptBegin|-->";
        internal const string ScriptEndMarker = "<!--|ScriptEnd|-->";
    }
    public enum FormDefFieldType
    {
        None = 0,
        Text = 1,
        TextArea = 2,
        Password = 3,
        Number = 4,
        Checkbox = 5,
        Pickone = 6,
        PickMultiple = 7,
        DateTime = 8,
        DateRange = 9,
        HTML = 10,
        FileUpload = 11,
        Button = 12,
        FormContainerDynamic = 14,
        FormContainerGrid = 16,
        ForeignKey = 17,
        MultiFileUpload = 18,
        MergedForm = 20,
        UserControlScalar = 21,
        Redirect = 22,
        EmbeddedForm = 23,
    }
    public enum SkinType
    {
        HTML = 1,
        FO = 2,
        DOCX = 3,

    }
    public enum FileUploadStorageType
    {
        Database = 1,
        Directory = 2,
        AmazonS3 = 3,
    }
    public enum FormDefFieldStyle
    {
        None = 0,
        Button = 3,
        Link = 4,
        Radio = 5, // for pickone
        DropDown = 6, // for pickone
        AutoComplete = 9, // for pickone
        Checkboxes = 11,// for PickMultiple
        Date = 13, // for DateTime
        Time = 14, // for DateTime
        ImageRotation = 15, // for Pickone
        RichTextCKEditorFull = 16, // for TextArea
        RichTextCKEditorStandard = 17, // for TextArea
        RichTextCKEditorBasic = 18, // for TextArea
        UpperCase = 19, // for Text
        LowerCase = 20, // for Text
        TitleCase = 21, // for Text
        Month = 22, // for DateTime
    }
    enum FormContainerSelectionStyle
    {
        None = 0,
        Checkbox = 1,
    }

    class FormContainerDetail
    {
        public FormContainerSelectionStyle selectionStyle;
    }
    public enum CallReasonType
    {
        PageLoad = 1,
        Refresh = 2,
        Ajax = 3
    }
    public class Smtp
    {
        public string Host;
        public int Port;
        public bool EnableSsl;
        public string UserName;
        public string Password;
    }
    public enum PageSizes
    {
        A4 = 1,
        Letter = 2
    }
    [DataContract]
    public class PDFPageSettings
    {
        [DataMember]
        public decimal pageWidth;
        [DataMember]
        public decimal pageHeight;
        [DataMember]
        public decimal topMargin;
        [DataMember]
        public decimal bottomMargin;
        [DataMember]
        public decimal leftMargin;
        [DataMember]
        public decimal rightMargin;
        [DataMember]
        public Dictionary<string, string> contentImages = new Dictionary<string, string>();
        public PDFPageSettings(PageSizes pageSize, bool landscape)
        {
            switch (pageSize)
            {
                case PageSizes.A4:
                    if (landscape)
                    {
                        pageHeight = 21m;
                        pageWidth = 29.7m;

                    }
                    else
                    {
                        pageWidth = 21m;
                        pageHeight = 29.7m;
                    }
                    topMargin = bottomMargin = leftMargin = rightMargin = 2.54m;
                    break;
                case PageSizes.Letter:
                    if (landscape)
                    {
                        pageHeight = 21.59m;
                        pageWidth = 27.94m;
                    }
                    else
                    {
                        pageWidth = 21.59m;
                        pageHeight = 27.94m;
                    }
                    topMargin = bottomMargin = leftMargin = rightMargin = 2.54m;
                    break;
            }
        }
        public PDFPageSettings Clone()
        {
            return (PDFPageSettings)MemberwiseClone();
        }
    }

    public class AppPressSettings
    {
        /// <summary>
        /// Type of Database
        /// </summary>
        public DatabaseType databaseType;
        /// <summary>
        /// Name of connection string in web.config file
        /// </summary>
        public string ConnectionString;

        /// <summary>
        /// Assembly reference of the main assembly of the Application
        /// </summary>
        public Assembly applicationAssembly;
        /// <summary>
        /// namespace containing Logic Functions in the Assembly
        /// </summary>
        public string applicationNameSpace;
        /// <summary>
        /// class containing Logic Functions in the Application assembly
        /// </summary>
        public string applicationClassName;

        /// <summary>
        /// List of names of plugin assemblies. The Logic functions should be in namespace Application and class AppLogic
        /// </summary>
        public List<string> pluginAssemblyNames;

        /// <summary>
        /// Date Format to use for conversion from .net DateTime to string
        /// </summary>
        public string NetDateFormat;
        /// <summary>
        /// Date Time format to use when converting from .net DateTime to string
        /// </summary>
        public string NetDateTimeFormat;
        /// <summary>
        /// Date format to use when setting Month field in JQuery
        /// </summary>
        public string NetDateMonthFormat;
        /// <summary>
        /// Date format to use when setting Month field in JQuery
        /// </summary>
        public string JQueryDateMonthFormat;
        /// <summary>
        /// Date Time format to use when setting Date Time filed in JQuery
        /// </summary>
        public string JQueryDateFormat;
        /// <summary>
        /// Date format to use when converting from Database DateTime column to string in Query 
        /// </summary>
        public string SQLDateFormat;
        /// <summary>
        /// Date Time format to use when converting from Database DateTime column to string in Query 
        /// </summary>
        public string SQLDateTimeFormat;
        /// <summary>
        /// Additional Date formats in which user can type in a Date Time field
        /// </summary>
        public string AdditionalInputDateFormats;

        /// <summary>
        /// Fom to redirect to after session expiry. Normally should be Login Form
        /// </summary>
        public string DefaultForm;

        /// <summary>
        /// Set to true when running AppPress in developer more. In this more AppPress shows Dev Links on Page for Logic Function Templates and Skins
        /// </summary>
        public bool developer;
        /// <summary>
        /// Set to true when running AppPress in Debug Mode
        /// </summary>
        public bool DEBUG = false;

        /// <summary>
        /// Set to true to make AppPress use the Debug Email set below
        /// </summary>
        public bool useDebugEmail;
        /// <summary>
        /// Emails used when application is running in developer or Debug Mode
        /// </summary>
        public string DebugEmail;

        /// <summary>
        /// Key to use to encrypt and decrypt in AES and DES encryptions
        /// </summary>
        public string encryptionKey;

        /// <summary>
        /// Localization data
        /// </summary>
        public Dictionary<string, Dictionary<string, string>> LocalizationData = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// ???
        /// </summary>
        public List<string> LocalizationLanguages = new List<string>();

        /// <summary>
        /// ???
        /// </summary>
        public List<AppPressInstance> Instances = new List<AppPressInstance>();

        public Smtp Smtp = null;

        public Type ApplicationAppPress = null;
    }

    public enum AppPressResponseType
    {
        AlertMessage = 1,
        FieldError = 2,
        ClearErrors = 3,
        Popup = 4,
        RefreshField = 5,
        Redirect = 7,
        AddInlineRow = 8,
        PageRefresh = 9,
        ExecuteJSScript = 10,
        SetFieldValue = 11,

        ClosePopupWindow = 13,
        SetFocus = 14,

        DownloadFile = 16,
        FormError = 17,
        FieldHelp = 18,
        SetPageNonDirty = 19,
        PromptClient = 20,

        SetPageDirty = 22,
        OpenUrl = 23,
        RemoteRefresh = 24,
        RemoteRefreshMasterContentArea = 25,
        RemoteRedirect = 26,
        ChangeFormDataId = 27,
    }

    internal class MethodCache
    {
        internal MethodInfo method;
        internal Type SecondParam;
    }
    internal enum FormCallType
    {
        None = 0,
        BeforeSaving = 1,
        GetContainerRowForm = 2,
        GetContainerColumnName = 4,
        GetTableName = 5,
        AfterSaving = 7,
    }
    /// <summary>
    /// ???
    /// </summary>
    [DataContract]
    public class FileDetails
    {
        [DataMember]
        public Int64 FileId { get; set; }
        [DataMember]
        public string FileName { get; set; }
        [DataMember]
        public string ContentType { get; set; }
        [DataMember]
        public DateTime UploadTime { get; set; }
        [DataMember]
        public byte[] FileBytes { get; set; }
    }
    internal class EmailDetails
    {
        public Int64 RowId { get; set; }
        public string FromEmail { get; set; }
        public string FromEmailName { get; set; }
        public string ToEmail { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public bool IsHtml { get; set; }
        public string CC { get; set; }
        public string BCC { get; set; }
        public List<string> FileAttachments { get; set; }
        public List<FileDetails> FileBytesAttachments { get; set; }
    }

    /// <summary>
    /// ???
    /// </summary>
    public enum FieldReadonlyType
    {
        None = 0, Readonly = 1
    }

    /// <summary>
    /// ???
    /// </summary>
    public enum FieldHiddenType
    {
        None, Hidden
    }


    internal enum PickFormContainerStyle
    {
        Inline = 1, Popup = 2
    }

    internal enum FormContainerStyle
    {
        InLine = 0,
        Grid = 1
    }
    /// <summary>
    /// ???
    /// </summary>
    public enum FormType
    {
        Application = 1,

        ContainerRowFormGenerated = 4,
        ContainerRowForm = 6,
        MasterForm = 9,
        MergedForm = 11,
        UserControlScalarForm = 12,
        PluginForm = 13,

    }
    internal enum FunctionType
    {
        None, OnClick, OnSelect, OnChange, Domain,
        OnColumnHeaderClick,
        Options, Calc
    }

    public enum AuditType
    {
        Login = 1,
        Logout = 2,
        InsertRow = 100,
        DeleteRow = 101,
        UpdateRow = 102,
        View = 103,
        DownloadFile = 104,
    }
    public enum EncryptionType
    {
        None = 0,
        AES = 1,
        DES = 2
    }
    /// <summary>
    /// ???
    /// </summary>
    [DataContract]
    public class Option
    {
        /// <summary>
        /// Id of the Option
        /// </summary>
        [DataMember]
        public string id;
        /// <summary>
        /// Display string of option
        /// </summary>
        [DataMember]
        public string value;
        public bool disabled = false;
    }
    /// <summary>
    /// AppPressInstance is a instance running as part of group of Instances which can share forms
    /// </summary>
    public class AppPressInstance
    {
        /// <summary>
        /// If of the instance. Should be number between 1-99 and each instance should have a unique id.
        /// This is not saved anywhere and can be changed.
        /// </summary>
        public int InstanceId;
        /// <summary>
        /// Use URL. 
        /// Example "https://hcm.example.com/HR-SG/AppPress.aspx"
        /// </summary>
        public string InstanceBaseUrl;
        /// <summary>
        /// Set it to true for the instance running.
        /// There should be only one LocalInstance
        /// </summary>
        public bool LocalInstance;
        /// <summary>
        /// IP Address where instance is running.
        /// </summary>
        public IPAddress InstanceIPAddress;
        /// <summary>
        /// Application can keep data for its use here
        /// </summary>
        public object ApplicationData = null;
    }
    internal class UploadFileDetail
    {
        public UploadFileDetail()
        {
            FileFilters = "*.jpeg;*.jpg;*.bmp;*.png;*.gif;*.pdf;*.doc;*.docx";
        }
        public decimal MaxFileSizeInMb;
        public string FileFilters;
        public string ButtonText;
    }

    public class DialogButton
    {
        public string fieldName;
        public string onClick;
    }
    /// <summary>
    /// 
    /// </summary>
    [DataContract]
    public class PopupParams
    {
        [DataMember]
        internal List<DialogButton> dialogButtons = null;
        [DataMember]
        public int PopupWidth = 800;
        [DataMember]
        public int? PopupHeight = null;
        [DataMember]
        public string title;
        [DataMember]
        public string SkinFileName = null;
        [DataMember]
        public bool ignoreSkin = false;
        [DataMember]
        internal bool forRedirect = false;
        [DataMember]
        internal RemoteData remoteData = null;
        [DataMember]
        public string PopupPosition = null;
        [DataMember]
        public bool NoFocus = false;
    }
    [DataContract]
    public class RedirectParams
    {
        /// <summary>
        /// Parameters added to Url. Initial & is required
        /// </summary>
        public string urlParams = "";
        /// <summary>
        /// These paramaters will be posted to the form being redirected to. Logic functions can get the parameters from a.Request
        /// </summary>
        [DataMember]
        public Dictionary<string, string> postParams = new Dictionary<string, string>();
        /// <summary>
        /// Target for Redirection. It is same as HTML target. http://www.w3schools.com/tags/att_a_target.asp
        /// </summary>
        [DataMember]
        public string target = null; // Html target (_new, -blank etc)
        /// <summary>
        /// Alert Message to be shown after Redirect
        /// </summary>
        [DataMember]
        public string alertMessage = null;
        /// <summary>
        /// Form Error to be shown after Redirect
        /// </summary>
        [DataMember]
        public string formError = null;
    }
    public static class Extensions
    {
        public static string Replace(this string source, string oldString, string newString, StringComparison comp)
        {
            if (newString == null)
                newString = string.Empty;
            while (true)
            {
                int index = source.IndexOf(oldString, comp);

                // Determine if we found a match
                bool MatchFound = index >= 0;

                if (MatchFound)
                {
                    // Remove the old text
                    source = source.Remove(index, oldString.Length);

                    // Add the replacemenet text
                    source = source.Insert(index, newString);
                }
                else
                    break;
            }
            return source;
        }
    }

    public class AppPressAssembly
    {
        public string namespaceName, className;
        public Assembly assembly;
        public string assemblyName;
        public bool runtime;
        public Type appLogicType;
        internal AppPressAssembly(string n, string s, Assembly a, bool runtime)
        {
            namespaceName = n;
            className = s;
            assembly = a;
            assemblyName = a.FullName;
            this.runtime = runtime;
            appLogicType = assembly.GetType(namespaceName + "." + className);
            if (appLogicType == null)
                throw new Exception("Could not find class: " + namespaceName + "." + className + " in Assembly: " + assembly.ToString());

        }
    }
    [DataContract]
    internal class AppPressResponse
    {
        [DataMember]
        internal AppPressResponseType appPressResponseType;
        [DataMember]
        internal string message;
        [DataMember]
        internal long fieldDefId;
        [DataMember]
        internal string url;
        [DataMember]
        internal string fieldHtml;
        [DataMember]
        internal long formDefId;
        [DataMember]
        internal string id;
        [DataMember]
        internal string Value;
        [DataMember]
        internal string JsStr;
        [DataMember]
        internal string popupTitle;
        [DataMember]
        internal int popupWidth;
        [DataMember]
        internal string popupHeight;
        [DataMember]
        internal string popupPosition;
        [DataMember]
        internal long pageTimeStamp;
        [DataMember]
        internal RedirectParams redirectParams;
        [DataMember]
        internal PopupParams popupParams;
        [DataMember]
        internal bool NoFocus = true;
        [DataMember]
        bool outer;
        [DataMember]
        internal int instanceId;

        internal static AppPressResponse Popup(AppPress a, FormData formData, PopupParams popupParams, int? InstanceId = null)
        {
            var oldCallReason = a.CallReason;
            a.CallReason = CallReasonType.Refresh;
            try
            {
                formData.callerFieldValue = a.fieldValue;
                string containerColumnName = null;
                var containerIdFormField = formData.formDef.formFields.Find(t => t.Type == FormDefFieldType.ForeignKey);
                if (containerIdFormField != null)
                    containerColumnName = containerIdFormField.fieldName;



                //var popContainerFieldValue = popupParams == null ? null : popupParams.containerFieldValue;
                //var containerFieldValue = popContainerFieldValue ?? formData.callerFieldValue ?? formData.containerFieldValue;
                //if (formData.containerFieldValue == null && popupParams != null)
                //    formData.containerFieldValue = popupParams.containerFieldValue;


                var containerFieldValue = formData.callerFieldValue;
                if (containerColumnName == null && containerFieldValue != null)
                    containerColumnName = containerFieldValue.formField.GetContainerColumnName(a);

                if (containerColumnName != null)
                {
                    if (containerFieldValue != null/* from remote popup*/ && containerFieldValue.FormData.IsNew)
                    {
                        // if parent form is not savable (tableName not present) then make parentId null
                        if (containerFieldValue.FormData.formDef.TableName == null)
                            formData.SetFieldValue(containerColumnName, null);
                        else
                        {
                            formData.IsDeleted = true;
                            throw new AppPressException("Please Save Form First Before Adding Row");
                        }
                    }
                    else
                        if (formData.GetFieldValue(containerColumnName).Value == null)
                        throw new Exception("Form: " + formData.formDef.formName + " Field: " + containerColumnName + " is of type ForeignKey and Should not be null");
                    //    formData.SetFieldValue(containerColumnName, containerFieldValue.FormData.id);
                }

                var clientAction = new AppPressResponse();
                clientAction.appPressResponseType = AppPressResponseType.Popup;
                clientAction.formDefId = formData.formDef.id;
                clientAction.pageTimeStamp = DateTime.UtcNow.Ticks;
                clientAction.NoFocus = !formData.IsNew;
                if (popupParams != null && popupParams.NoFocus)
                    clientAction.NoFocus = true;
                var formDef = formData.formDef;
                if (popupParams == null || !popupParams.forRedirect)
                {
                    if (InstanceId == null)
                        a.PopupDatas[a.pageStackCount].InstanceId = AppPress.LocalInstanceId;
                    else
                        a.PopupDatas[a.pageStackCount].InstanceId = InstanceId.Value;

                    Util.ApplyOnChildFormDatas(a.formDatas, formData, t => t.pageStackIndex = a.pageStackCount);
                }
                bool WarnOnDirtyClose = false;
                Util.ApplyOnChildFormDatas(a.formDatas, formData, t => WarnOnDirtyClose = WarnOnDirtyClose || t.formDef.TableName != null);

                //a.sessionData.AddFormNameandFormId(new FormDefIdAndFormId() { FormId = formData.id, FormDefId = formData.formDefId });

                if (popupParams == null || !popupParams.forRedirect)
                {
                    // Move End Buttons to Dialog Buttons of JQuery
                    int lastButtonIndex;
                    for (lastButtonIndex = formData.fieldValues.Count() - 1; lastButtonIndex >= 0; --lastButtonIndex)
                    {
                        var fieldValue = formData.fieldValues[lastButtonIndex];
                        if ((fieldValue.formField.Type == FormDefFieldType.Button && fieldValue.formField.containerFormField == null /*&& fieldValue.Hidden == FieldHiddenType.None*/) || fieldValue.formField.Type == FormDefFieldType.ForeignKey)
                        {
                        }
                        else
                            break;
                    }
                    if (lastButtonIndex != -1)
                        for (var i = lastButtonIndex + 1; i < formData.fieldValues.Count(); ++i)
                        {
                            var fieldValue = formData.fieldValues[i];
                            if (fieldValue.formField.Type == FormDefFieldType.Button && fieldValue.Hidden == FieldHiddenType.None && fieldValue.formField.OriginalType == (int)FormDefFieldType.FormContainerGrid)
                            {
                                if (popupParams == null)
                                    popupParams = new PopupParams();

                                fieldValue.Hidden = FieldHiddenType.Hidden;
                                if (popupParams.dialogButtons == null)
                                    popupParams.dialogButtons = new List<DialogButton>();
                                string displayName;
                                if (fieldValue.FieldLabel != null)
                                    displayName = fieldValue.FieldLabel;
                                else
                                    displayName = fieldValue.formField.GetDisplayName();
                                popupParams.dialogButtons.Add(new DialogButton { fieldName = displayName, onClick = fieldValue.GetHtmlOnClick(a, true) });
                            }
                        }
                }
                var pFieldValue = a.fieldValue;
                try
                {
                    a.fieldValue = new FieldValue();
                    a.fieldValue.FormData = formData;
                    var skin = popupParams != null && popupParams.SkinFileName != null ? AppPress.skins[popupParams.SkinFileName].skin : formData.formDef.GetSkin(a, popupParams == null ? false : popupParams.ignoreSkin, true, null, SkinType.HTML, 0);
#if DEBUG
                    a.fieldsNotGenerated = new List<FieldValue>();
#endif
                    clientAction.fieldHtml = Util.CompileSkin(a, skin, false, SkinType.HTML, false).Replace(AppPress.HeaderSignature, a.GetBottomScript() + AppPress.HeaderSignature);
                    clientAction.fieldHtml = Util.RemoveScripts(a, clientAction.fieldHtml);
                    if (AppPress.Settings.DEBUG)
                    {
                        if (a.LinksGenerated.Find(t => t == formDef.formName) == null)
                        {
                            clientAction.fieldHtml += formDef.GenerateDeveloperLinks(a);
                            a.LinksGenerated.Add(formDef.formName);
                        }
                    }
                }
                finally
                {
                    a.fieldValue = pFieldValue;
                }
                if (WarnOnDirtyClose)
                    a.JsStr.Append("SetWarnOnDirtyClose();\n");

                clientAction.JsStr = a.JsStr.ToString();
                if (popupParams != null)
                    clientAction.popupTitle = popupParams.title;
                if (clientAction.popupTitle == null)
                {
                    var title = formDef.formName;
                    if (formDef.ContainerFormField != null && formDef.ContainerFormField.OriginalType == (int)FormDefFieldType.FormContainerGrid)
                        if (title.EndsWith("Popup"))
                            title = title.Substring(0, title.Length - "Popup".Length);
                    clientAction.popupTitle = AppPress.InsertSpacesBetweenCaps(title);
                }
                clientAction.popupParams = popupParams;
                int popupWidth = 600;
                string popupHeight = "auto";
                string popupPosition = null;
                if (a.serverFunction != null)
                {
                    popupWidth = int.Parse(a.TryGetFunctionParameterValue("PopupWidth") ?? "650");
                    popupHeight = a.TryGetFunctionParameterValue("PopupHeight") ?? "auto";
                    popupPosition = a.TryGetFunctionParameterValue("PopupPosition");
                }
                clientAction.popupWidth = popupWidth;
                clientAction.popupHeight = popupHeight;
                clientAction.popupPosition = popupPosition ?? "center";
                clientAction.id = formData.id; // use the id to set focus to dialog window instead of first field. This way Escape will work without clicking on window
                return clientAction;
            }
            finally
            {
                a.CallReason = oldCallReason;
            }
        }

        internal static AppPressResponse Popup(AppPress a, FormDef formDef, string formDataId, PopupParams popupParams)
        {
            //if (formDef.TableName == null && !AppPress.IsNewId(formDataId))
            //    throw new Exception("Loading Form: " + formDef.formName + " using Id: " + formDataId + " but Form does not have TableName Property.");

            a.CallReason = CallReasonType.PageLoad;
            if (a.remoteLoginUserId != null && formDef.ContainerFormField != null && a.fieldValue == null  /*otherwise popup from remote popup*/)
                throw new Exception(formDef.GetDescription() + "<br/>Error: Popup generated from FormContainerGrid can not be used in RemotePopup");
            // commented as was giving error in EmployeeProfile->Division->Add
            //if (formDef.ContainerFormField != null && formDef.ContainerFormField.formDef.id != a.fieldValue.FormData.formDef.id)
            //    throw new Exception("Form: " + formDef.ContainerFormField.formDef.formName + " Field: " + formDef.ContainerFormField.fieldName + " cannot be used to Create a Popup.");
            if (formDataId == null)
                formDataId = AppPress.GetUniqueId().ToString();
            if (popupParams == null || !popupParams.forRedirect)
                a.pageStackCount++;
            try
            {
                var formData = a.LoadFormData(formDef.id, formDataId, a.fieldValue, null);
                if (formData.IsNew)
                    // for existing form ForeignKey gets loaded from DB
                    foreach (var fieldValue in formData.fieldValues)
                        if (fieldValue.formField.Type == FormDefFieldType.ForeignKey)
                            fieldValue.Value = a.fieldValue.FormData.id;
                a.CalcFormDatas(formData, null, true);
                // formData can change in Init because of MergeForm
                formData = a.formDatas.Find(t => t.formDefId == formData.formDefId && t.id == formData.id);
                return AppPressResponse.Popup(a, formData, popupParams);
            }
            catch
            {
                if (popupParams == null || !popupParams.forRedirect)
                    a.pageStackCount--;
                throw;
            }
        }
        internal static AppPressResponse Popup(AppPress a, int formDefId, string formDataId)
        {
            return Popup(a, AppPress.FindFormDef(formDefId), formDataId, null);
        }
        internal static AppPressResponse Redirect(AppPress a, long formDefId, string id, RedirectParams redirectParams)
        {
            //if (a.pageStackCount != 0)
            //    throw new AppPressException("Cannot Redirect while popup window is open. First ClosePopup then redirect");
            var formDef = AppPress.FindFormDef(formDefId);
            if (a.remoteLoginUserId != null)
            {
                var apr = new AppPressResponse();
                apr.appPressResponseType = AppPressResponseType.RemoteRedirect;
                apr.message = formDef.formName;
                apr.id = id;
                return apr;
            }
            var clientAction = new AppPressResponse();
            if (redirectParams == null && id != null)
                redirectParams = new RedirectParams();

            var redirect = a.functionCall == null && (redirectParams == null || redirectParams.postParams.Count() == 0);
            clientAction.url = a.GetUrl(formDef.formName, id, redirectParams, null);
            if (redirect && redirectParams != null)
            {
                if (redirectParams.alertMessage != null)
                    clientAction.url += "&_AppPressAlertMessage=" + HttpUtility.UrlEncode(redirectParams.alertMessage);
                if (redirectParams.formError != null)
                    clientAction.url += "&_AppPressFormError=" + HttpUtility.UrlEncode(redirectParams.formError);
            }
            if (AppPress.IsSecureForm(formDef, id))
                a.sessionData.AddSecureUrl(clientAction.url);
            if (redirect)
            {
                a.Response.Redirect(clientAction.url, false);
                return null;
            }
            clientAction.redirectParams = redirectParams;
            clientAction.appPressResponseType = AppPressResponseType.Redirect;
            // remove all pending refresh
            a.appPressResponse.RemoveAll(t => t.appPressResponseType == AppPressResponseType.RefreshField);
            return clientAction;
        }

        internal AppPressResponse()
        {
        }

        internal static AppPressResponse RefreshField(AppPress a, FieldValue fieldValue, bool getData)
        {
            if (fieldValue == null)
                throw new Exception("fieldValue cannot be null for RefreshField Domain function.");
            var appPressResponse = new AppPressResponse();
            appPressResponse.appPressResponseType = AppPressResponseType.RefreshField;
            appPressResponse.formDefId = fieldValue.FormData.formDefId;
            appPressResponse.id = fieldValue.FormData.id;
            appPressResponse.fieldDefId = fieldValue.formField.id;
            var pFieldValue = a.fieldValue;
            var pCallReason = a.CallReason;
            try
            {
                a.CallReason = CallReasonType.Refresh;
                a.fieldValue = fieldValue;
                var str = a.fieldValue.GetSkin(a, out appPressResponse.outer);

                if (str == null)
                    throw new Exception(a.fieldValue.formField.GetDescription() + " Could not find Begin or End Marker. Either the Markers are missing or misplaced or the field should be hidden.");
                if (getData)
                {
                    if (!a.fieldValue.FormData.IsNew && fieldValue.formField.Type != FormDefFieldType.FormContainerDynamic)
                    {
                        var formData = FormData.InitializeFormData(a, a.fieldValue.FormData.formDef, a.fieldValue.FormData.id);
                        a.fieldValue.FormData.MergeFields(formData, fieldValue);
                        a.fieldValue = a.fieldValue.FormData.fieldValues.Find(t => t.fieldDefId == a.fieldValue.fieldDefId);
                    }
                    a.CalcFormDatas(a.fieldValue.FormData, a.fieldValue, true);
                }
#if DEBUG
                a.fieldsNotGenerated = new List<FieldValue>();
#endif
                appPressResponse.fieldHtml = Util.CompileSkin(a, str, true, SkinType.HTML, false);
                appPressResponse.fieldHtml = Util.RemoveScripts(a, appPressResponse.fieldHtml);
                appPressResponse.message = fieldValue.GetFieldDescription();
                appPressResponse.JsStr = a.JsStr.ToString();

            }
            finally
            {
                a.fieldValue = pFieldValue;
                a.CallReason = pCallReason;
            }
            return appPressResponse;
        }

        internal static AppPressResponse FieldError(FieldValue fieldValue, string message)
        {
            var clientAction = new AppPressResponse();
            clientAction.appPressResponseType = AppPressResponseType.FieldError;
            clientAction.formDefId = fieldValue.FormData.formDefId;
            clientAction.id = fieldValue.FormData.originalId ?? fieldValue.FormData.id;
            clientAction.fieldDefId = fieldValue.FormData.formDef.GetFormField(fieldValue.fieldDefId).id;
            clientAction.fieldHtml = fieldValue.GetFieldDescription();
            clientAction.message = message;
            return clientAction;
        }

        internal static AppPressResponse FieldHelp(FieldValue fieldValue, string message)
        {
            var clientAction = new AppPressResponse();
            clientAction.appPressResponseType = AppPressResponseType.FieldHelp;
            clientAction.formDefId = fieldValue.FormData.formDefId;
            clientAction.id = fieldValue.FormData.originalId ?? fieldValue.FormData.id;
            clientAction.fieldDefId = fieldValue.FormData.formDef.GetFormField(fieldValue.fieldDefId).id;
            clientAction.fieldHtml = fieldValue.GetFieldDescription();
            clientAction.message = message;
            return clientAction;
        }

        /// <summary>
        /// Executed the JSScript of on the Browser
        /// Script is added to AppPressReponse as Type ExecuteJSScript
        /// The response items are executed by AppPress JS library in same sequence as they are Added
        /// </summary>
        /// <param name="JSScript">Script to execute</param>
        /// <returns></returns>
        public static AppPressResponse ExecuteJSScript(string JSScript)
        {
            var clientAction = new AppPressResponse();
            clientAction.appPressResponseType = AppPressResponseType.ExecuteJSScript;
            clientAction.JsStr = JSScript;
            return clientAction;
        }

        internal static AppPressResponse AlertMessage(string message, string title = null, int popupWidth = 0, bool isHtml = true)
        {
            if (message == null)
                message = "";
            var clientAction = new AppPressResponse();
            clientAction.appPressResponseType = AppPressResponseType.AlertMessage;
            if (isHtml)
                clientAction.message = message;
            else
                clientAction.message = HttpUtility.HtmlEncode(message);

            clientAction.message = clientAction.message.Replace("&lt;br&gt;", "<br/>");
            clientAction.message = clientAction.message.Replace("&lt;/br&gt;", "<br/>");
            clientAction.message = clientAction.message.Replace("&lt;br/&gt;", "<br/>");
            clientAction.message = clientAction.message.Replace("&lt;br /&gt;", "<br/>");
            clientAction.message = clientAction.message.Replace("\\n", "<br/>");
            clientAction.message = clientAction.message.Replace("\r\n", "<br/>");
            clientAction.message = clientAction.message.Replace("\n", "<br/>");
            clientAction.popupTitle = title;
            clientAction.popupWidth = popupWidth;
            return clientAction;
        }

        internal static AppPressResponse FormError(long formDefId, string formId, string message)
        {
            var clientAction = new AppPressResponse();
            clientAction.appPressResponseType = AppPressResponseType.FormError;
            clientAction.formDefId = formDefId;
            clientAction.id = formId;
            clientAction.message = HttpUtility.HtmlEncode(message);
            return clientAction;
        }

        internal static AppPressResponse FormError(FormData formData, string message)
        {
            var clientAction = new AppPressResponse();
            clientAction.appPressResponseType = AppPressResponseType.FormError;
            clientAction.formDefId = formData.formDefId;
            clientAction.id = formData.originalId ?? formData.id;
            clientAction.message = HttpUtility.HtmlEncode(message);
            return clientAction;
        }

        internal static AppPressResponse SetFocus(long formDefId, string id, FormField formField)
        {
            var clientAction = new AppPressResponse();
            clientAction.appPressResponseType = AppPressResponseType.SetFocus;
            clientAction.formDefId = formDefId;
            clientAction.id = id;
            clientAction.fieldDefId = formField.id;
            return clientAction;
        }

        internal static AppPressResponse CloseWindow(AppPress a)
        {
            var clientAction = new AppPressResponse();
            clientAction.appPressResponseType = AppPressResponseType.ClosePopupWindow;
            if (a.pageStackCount == 0)
                throw new Exception("ClosePopup: No Popup window found to be closed");
            a.pageStackCount--;
            var removedFormDatas = a.formDatas.FindAll(t => t.pageStackIndex > a.pageStackCount);
            foreach (var removedFormData in removedFormDatas)
                removedFormData.IsDeleted = true;
            a.formDatas.RemoveAll(t => t.pageStackIndex > a.pageStackCount);
            // remove refresh fields on Popup
            a.appPressResponse.RemoveAll(t => t.appPressResponseType == AppPressResponseType.RefreshField &&
                a.formDatas.Find(t1 => t1.id == t.id && t1.formDefId == t.formDefId) == null);
            return clientAction;
        }

        internal static AppPressResponse SetFieldValue(long formDefId, string id, FormField formField, string value, string fieldHtml = null)
        {
            var clientAction = new AppPressResponse();
            clientAction.appPressResponseType = AppPressResponseType.SetFieldValue;
            clientAction.formDefId = formDefId;
            clientAction.id = id;
            clientAction.fieldDefId = formField.id;
            clientAction.Value = value;
            clientAction.fieldHtml = fieldHtml;
            return clientAction;
        }

        internal static AppPressResponse ClearErrors(FieldValue fieldValue)
        {
            var clientAction = new AppPressResponse();
            clientAction.appPressResponseType = AppPressResponseType.ClearErrors;
            clientAction.formDefId = fieldValue.FormData.formDefId;
            clientAction.id = fieldValue.FormData.id;
            clientAction.fieldDefId = fieldValue.formField.id;
            return clientAction;
        }
        internal static AppPressResponse ClearErrors()
        {
            var clientAction = new AppPressResponse();
            clientAction.appPressResponseType = AppPressResponseType.ClearErrors;
            return clientAction;
        }
        internal static AppPressResponse SetPageNonDirty()
        {
            var clientAction = new AppPressResponse();
            clientAction.appPressResponseType = AppPressResponseType.SetPageNonDirty;
            return clientAction;
        }

        internal static AppPressResponse PageRefresh()
        {
            var clientAction = new AppPressResponse();
            clientAction.appPressResponseType = AppPressResponseType.PageRefresh;
            return clientAction;
        }

        internal static AppPressResponse PromptClient(AppPress a, string message, string title = null, int popupWidth = 0, bool htmlEncode = true)
        {
            var clientAction = new AppPressResponse();
            clientAction.appPressResponseType = AppPressResponseType.PromptClient;
            clientAction.formDefId = a.fieldValue.FormData.formDefId;
            clientAction.fieldDefId = a.fieldValue.fieldDefId;
            // if Prompt client was called within transaction use originalId as RollBack will change id to originalId
            clientAction.id = a.site.trans != null ? a.fieldValue.FormData.originalId : a.fieldValue.FormData.id;
            if (htmlEncode)
                clientAction.message = HttpUtility.HtmlEncode(message);
            else
                clientAction.message = message;
            clientAction.popupTitle = title;
            clientAction.popupWidth = popupWidth;
            return clientAction;
        }


        internal static AppPressResponse SetPageDirty(bool dirty)
        {
            var clientAction = new AppPressResponse();
            clientAction.appPressResponseType = AppPressResponseType.SetPageDirty;
            clientAction.Value = (dirty ? 1 : 0).ToString();
            return clientAction;
        }

        public static AppPressResponse DownloadFile(AppPress a, string url, string datafieldName, string data)
        {
            var clientAction = new AppPressResponse();
            clientAction.url = url;
            clientAction.appPressResponseType = AppPressResponseType.DownloadFile;
            clientAction.message = datafieldName;
            clientAction.Value = data;
            a.sessionData.AddSecureUrl(AppPress.GetBaseUrl() + url);
            return clientAction;
        }
        public static AppPressResponse OpenUrl(AppPress a, string url, string target, string message)
        {
            var clientAction = new AppPressResponse();
            clientAction.url = url;
            clientAction.appPressResponseType = AppPressResponseType.OpenUrl;
            clientAction.message = message;
            clientAction.id = target; // reuse id field
            a.sessionData.AddSecureUrl(AppPress.GetBaseUrl() + url);
            return clientAction;
        }

    }

    [Serializable]
    public class SessionData
    {
        public string email;
        public string loginUserId;
        public Dictionary<string, object> UserData = null;
        internal Int64 uniqueId = AppPress.GetUniqueId();
        public string CurrentLanguage = null;
        public int screenWidth, screenHeight, documentWidth, documentHeight, windowWidth, windowHeight;
        public SessionData(string email, string loginUserId, string CurrentLanguage)
        {
            this.email = email;
            this.loginUserId = loginUserId;
            this.CurrentLanguage = CurrentLanguage;
            this.UserData = new Dictionary<string, object>();
        }


        internal List<string> formDefIdAndFormIds = new List<string>();
        public void AddSecureUrl(string url)
        {
            if (formDefIdAndFormIds.Where(t => t == url).Count() == 0)
            {
                formDefIdAndFormIds.Add(url);
            }
        }
    }

    [Serializable]
    public class CustomizeException
    {
        public string Message;
        public string StackTrace;

        public string InnerExMessage1;
        public string InnerExStackTrace1;

        public string InnerExMessage2;
        public string InnerExStackTrace2;
    }

    [DataContract]
    internal class ServerFunctionParameter
    {
        [DataMember]
        public string Name;
        [DataMember]
        public string Value;

        public ServerFunctionParameter(string Name, string Value)
        {
            // TODO: Complete member initialization
            this.Name = Name;
            this.Value = Value;
        }
        public ServerFunctionParameter()
        {
            // TODO: Complete member initialization

        }
    }
    internal class EmailTemplate
    {
        public long Id { get; set; }
        public string Subject { get; set; }
        public string Content { get; set; }
    }

    internal class Dimension
    {

        public decimal WindowWidth { get; set; }
        public decimal WindowHeight { get; set; }
        public decimal PDFWidth { get; set; }
        public decimal PDFHeight { get; set; }
        public int? MaximumRows { get; set; }
    }

    internal class ContainerField
    {
        public string formName;
        public Int64 id;
        public string fieldName;
        public ContainerField()
        {
        }
        public ContainerField(string formName, Int64 id, string fieldName)
        {
            this.formName = formName;
            this.id = id;
            this.fieldName = fieldName;
        }

        internal bool Same(ContainerField containerField)
        {
            if (containerField == null)
                return false;
            return containerField.id == id && containerField.formName == formName && containerField.fieldName == fieldName;
        }
    }

    internal class Util
    {
        private const string CultureCookieNam = "__CultureCookie";
        static decimal PDFHeight = 10.37M;
        static decimal PDFWidth = 8.5M;
        internal static string FoHeaderStr
        {
            get
            {
                return "<?xml version='1.0' encoding='UTF-8'?>" +
                                       "<fo:root xmlns:fo='http://www.w3.org/1999/XSL/Format'>" +
                                       " <fo:layout-master-set>" +
                                       " <fo:simple-page-master master-name='first'" +
                                       " margin-right='1.5cm'" +
                                        " margin-left='1.5cm'" +
                                        " margin-bottom='2cm'" +
                                        " margin-top='1cm'" +
                                        " page-width='" + PDFWidth + "in'" +
                                        " page-height='" + PDFHeight + "in'>" +
                                        " <fo:region-body margin-top='1cm'/>" +
                                        " <fo:region-before extent='1cm'/>" +
                                        " <fo:region-after extent='1.5cm'/>" +
                                    " </fo:simple-page-master>" +
                                    " </fo:layout-master-set>" +
                                    " <fo:page-sequence master-reference='first'>" +
                                    " <fo:flow flow-name='xsl-region-body'>";

            }
        }

        internal const string FoEndTag = " </fo:flow></fo:page-sequence></fo:root>";

        static string baseUrl = null;
        internal static string serverUrl = null;
        internal static string GetBaseUrl()
        {
            if (baseUrl == null)
            {
                serverUrl = HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Authority);
                if (serverUrl.StartsWith("http://app"))
                    throw new Exception("BaseURL cannot start with http"); // temporary for testing
                baseUrl = serverUrl + HttpContext.Current.Request.ApplicationPath;
                if (!baseUrl.EndsWith("/"))
                    baseUrl += "/";
            }
            return baseUrl;
        }

        internal static void ApplyOnChildFormDatas(List<FormData> formDatas, FormData formData, Action<FormData> func1)
        {
            func1.Invoke(formData);
            foreach (var formData1 in formDatas)
            {
                var containerFieldValue = formData1.containerFieldValue;
                while (containerFieldValue != null)
                    if (containerFieldValue.FormData == formData)
                    {
                        func1.Invoke(formData1);
                        break;
                    }
                    else
                        containerFieldValue = containerFieldValue.FormData.containerFieldValue;
            }

        }
        internal static MethodInfo GetMethod(AppPress a, string functionName, Type[] types, bool reverse = false)
        {
            for (int i = 0; i < AppPress.Assemblies.Count; ++i)
            {
                var assembly = AppPress.Assemblies[reverse ? (AppPress.Assemblies.Count - i - 1) : i];
                var method = assembly.appLogicType.GetMethod(functionName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static, null, types, null);
                if (method != null)
                    return method;
            }
            return null;
        }

        internal static object InvokeFunction(ServerFunction serverFunction, AppPress a)
        {
            if (serverFunction.method == null)
                throw new Exception("Could not Find Function:" + serverFunction.FunctionName);
            ServerFunction pServerFunction = a.serverFunction;
            try
            {
                a.serverFunction = serverFunction;
                return InvokeMethod(a, serverFunction.method, new object[] { a });
            }
            finally
            {
                a.serverFunction = pServerFunction;
            }

        }

        internal static object InvokeMethod(AppPress a, MethodInfo method, object[] parameters)
        {
            try
            {
                return method.Invoke(null, parameters);
            }
            catch (Exception ex)
            {
                if (!a.StopExecution)
                {
                    if (ex.GetBaseException() is AppPressException || ex.GetBaseException() is SessionExpiredException)
                        throw ex.GetBaseException();
                    if (ex.InnerException != null)
                    {
                        var message = ex.InnerException.Message;
#if Debug
                        message += "<br/><br/>Stack Trace:<br/>" + ex.InnerException.StackTrace;
#endif
                        throw new Exception(message);
                    }
                    throw;
                }
            }
            return null;
        }
        internal static void InvokeFunctions(List<ServerFunction> serverFunctions, AppPress a)
        {
            if (serverFunctions != null)
                foreach (var serverFunction in serverFunctions)
                {
                    try
                    {
                        serverFunction.ExecuteClientFunctions = false;
                        InvokeFunction(serverFunction, a);
                        if (serverFunction.ExecuteClientFunctions)
                            break;
                    }
                    catch (TargetInvocationException ex)
                    {
                        throw ex.InnerException;
                    }
                    catch (Exception ex)
                    {
                        if (ex.GetBaseException() is AppPressException)
                        {
                            //a.clientActions =
                            //    ((AppPressException)ex.GetBaseException()).clientActions;
                            throw ex.GetBaseException();
                        }
                        throw;
                    }
                }

        }
        public static DateTime GetUploadTime(long fileId)
        {
            var site = new DAOBasic();
            try
            {
                var dr = site.ExecuteQuery("SELECT UploadDate FROM Application_files WHERE ID=" + fileId);
                try
                {
                    dr.Read();
                    return dr.GetDateTime(0);
                }
                finally
                {
                    dr.Close();
                }
            }
            finally
            {
                site.Close();
            }
        }

        internal static byte[] GetFile(DAOBasic site, string dbName, int fileId, out string fileName, out string fileType, out DateTime fileUploadTime)
        {
            FileUploadStorageType storageType;
            string filePath = null;
            byte[] filebytes = null;
            fileName = null;
            fileType = null;
            fileUploadTime = DateTime.Now;
            int? encryptionKey = null;
            if (dbName == null)
                dbName = site.dbName;
            var dr = site.ExecuteQuery("SELECT StorageType, FilePath, FileName,FileType,FileContent,UploadDate,EncryptionType FROM " + site.QuoteDBName(dbName) + ".Application_files WHERE ID=" + fileId);
            try
            {
                if (!dr.Read())
                    return null;
                {
                    storageType = (FileUploadStorageType)dr.GetInt32(0);
                    fileName = dr.GetString(2);
                    if (!dr.IsDBNull(1))
                    {
                        filePath = dr.GetString(1);
                        if (filePath.StartsWith("~"))
                            filePath = HttpContext.Current.Server.MapPath("~") + filePath.Substring(1);
                    }
                    fileType = dr.GetString(3);
                    if (!dr.IsDBNull(4))
                        filebytes = (byte[])dr[4];
                    fileUploadTime = dr.GetDateTime(5);
                    if (!dr.IsDBNull(6))
                        encryptionKey = dr.GetInt32(6);
                }
            }
            finally
            {
                dr.Close();
            }
            if (storageType == FileUploadStorageType.Directory)
                filebytes = System.IO.File.ReadAllBytes(filePath);
            else if (storageType == FileUploadStorageType.AmazonS3)
            {
                // FilePath has bucket name.
                // FileName has name of file.
                // ??? Yash
            }
            if (encryptionKey != null)
            {
                filebytes = Convert.FromBase64String(AppPress.DecryptTextAES(System.Text.Encoding.UTF8.GetString(filebytes)));
            }

            return filebytes;
        }

        internal static long SaveFile(FileUploadStorageType storageType, string directory, string filename, byte[] fileContent, string fileType, long fileSizeInKb, EncryptionType? encryptionType, bool NonSecure)
        {
            SHA256Managed sha = new SHA256Managed();
            byte[] checksum = sha.ComputeHash(fileContent);
            string checksumStr = BitConverter.ToString(checksum).Replace("-", String.Empty);
            var site = new DAOBasic(); // Add for encryption key
            site.BeginTrans();
            try
            {
                if (encryptionType != null)
                {
                    fileContent = System.Text.Encoding.UTF8.GetBytes(AppPress.EncryptTextAES(Convert.ToBase64String(fileContent)));
                }
                var encStr = encryptionType == null ? "null" : ((int)encryptionType).ToString();
                var query = "Insert into Application_files(StorageType,FilePath, Filename, FileType,UploadDate,FileSize";
                if (storageType == FileUploadStorageType.Database)
                    query += ", FileContent";
                query += ",Checksum,EncryptionType,Used,NonSecure) Values (" + (int)storageType + ",null,  @Filename, @FileType,@UploadDate,@FileSize";
                int plen = 4;
                if (storageType == FileUploadStorageType.Database)
                {
                    plen = 5;
                    query += ", @FileContent";
                }
                query += ",'" + checksumStr + "'," + encStr + ",1," + (NonSecure ? "1" : "0") + ");";
                Int64 id;
                if (site.databaseType == DatabaseType.MySql)
                {
                    query += "Select LAST_INSERT_ID();";
                    var commandParameters = new MySqlParameter[plen];
                    commandParameters[0] = new MySqlParameter("@Filename", MySqlDbType.String) { Value = filename };
                    commandParameters[1] = new MySqlParameter("@FileType", MySqlDbType.String) { Value = fileType };
                    commandParameters[2] = new MySqlParameter("@UploadDate", MySqlDbType.DateTime) { Value = DateTime.UtcNow.ToString(DAOBasic.DBDateTimeFormat) };
                    commandParameters[3] = new MySqlParameter("@FileSize", MySqlDbType.Int64) { Value = fileSizeInKb };
                    if (storageType == FileUploadStorageType.Database)
                        commandParameters[4] = new MySqlParameter("@FileContent", MySqlDbType.LongBlob) { Value = fileContent };
                    id = Convert.ToInt64(site.ExecuteScalar(CommandType.Text, query, commandParameters));
                }
                else
                {
                    query += "SELECT @@IDENTITY";
                    var commandParameters = new SqlParameter[plen];
                    commandParameters[0] = new SqlParameter("@Filename", SqlDbType.Text) { Value = filename };
                    commandParameters[1] = new SqlParameter("@FileType", SqlDbType.Text) { Value = fileType };
                    commandParameters[2] = new SqlParameter("@UploadDate", SqlDbType.DateTime) { Value = DateTime.UtcNow.ToString() };
                    commandParameters[3] = new SqlParameter("@FileSize", SqlDbType.BigInt) { Value = fileSizeInKb };
                    if (storageType == FileUploadStorageType.Database)
                        commandParameters[4] = new SqlParameter("@FileContent", SqlDbType.VarBinary) { Value = fileContent };
                    id = Convert.ToInt64(site.ExecuteScalar(CommandType.Text, query, commandParameters));
                }
                if (storageType == FileUploadStorageType.Directory)
                {
                    var path = directory;
                    if (path.StartsWith("~"))
                        path = HttpContext.Current.Server.MapPath("~") + directory.Substring(1);
                    if (!Directory.Exists(path))
                        throw new AppPressException("Directory: " + path + " does not exist which is used as storage directory for file upload.");
                    var filePath = path + "\\" + id + "_" + filename;
                    System.IO.File.WriteAllBytes(filePath, fileContent);
                    site.ExecuteNonQuery("Update Application_files Set FilePath='" + site.EscapeSQLString(directory + "/" + id + "_" + filename) + "' Where Id=" + id);
                }
                else if (storageType == FileUploadStorageType.AmazonS3)
                {
                    // Bucket Name is in directory.
                    // FileName, FileBytes are in parameter.
                    // ??? Yash
                }
                site.Commit();
                return id;
            }
            catch
            {
                site.RollBack();
                throw;
            }
            finally
            {
                site.Close();
            }
        }

        private static Dictionary<string, string> ExtensionMap = null;

        static void MimeMapping()
        {
            ExtensionMap.Add(".323", "text/h323");
            ExtensionMap.Add(".asx", "video/x-ms-asf");
            ExtensionMap.Add(".acx", "application/internet-property-stream");
            ExtensionMap.Add(".ai", "application/postscript");
            ExtensionMap.Add(".aif", "audio/x-aiff");
            ExtensionMap.Add(".aiff", "audio/aiff");
            ExtensionMap.Add(".axs", "application/olescript");
            ExtensionMap.Add(".aifc", "audio/aiff");
            ExtensionMap.Add(".asr", "video/x-ms-asf");
            ExtensionMap.Add(".avi", "video/x-msvideo");
            ExtensionMap.Add(".asf", "video/x-ms-asf");
            ExtensionMap.Add(".au", "audio/basic");
            ExtensionMap.Add(".application", "application/x-ms-application");
            ExtensionMap.Add(".bin", "application/octet-stream");
            ExtensionMap.Add(".bas", "text/plain");
            ExtensionMap.Add(".bcpio", "application/x-bcpio");
            ExtensionMap.Add(".bmp", "image/bmp");
            ExtensionMap.Add(".cdf", "application/x-cdf");
            ExtensionMap.Add(".cat", "application/vndms-pkiseccat");
            ExtensionMap.Add(".crt", "application/x-x509-ca-cert");
            ExtensionMap.Add(".c", "text/plain");
            ExtensionMap.Add(".log", "text/plain");
            ExtensionMap.Add(".css", "text/css");
            ExtensionMap.Add(".cer", "application/x-x509-ca-cert");
            ExtensionMap.Add(".crl", "application/pkix-crl");
            ExtensionMap.Add(".cmx", "image/x-cmx");
            ExtensionMap.Add(".csh", "application/x-csh");
            ExtensionMap.Add(".cod", "image/cis-cod");
            ExtensionMap.Add(".cpio", "application/x-cpio");
            ExtensionMap.Add(".clp", "application/x-msclip");
            ExtensionMap.Add(".crd", "application/x-mscardfile");
            ExtensionMap.Add(".deploy", "application/octet-stream");
            ExtensionMap.Add(".dll", "application/x-msdownload");
            ExtensionMap.Add(".dot", "application/msword");
            ExtensionMap.Add(".doc", "application/msword");
            ExtensionMap.Add(".dvi", "application/x-dvi");
            ExtensionMap.Add(".dir", "application/x-director");
            ExtensionMap.Add(".dxr", "application/x-director");
            ExtensionMap.Add(".der", "application/x-x509-ca-cert");
            ExtensionMap.Add(".dib", "image/bmp");
            ExtensionMap.Add(".dcr", "application/x-director");
            ExtensionMap.Add(".disco", "text/xml");
            ExtensionMap.Add(".exe", "application/octet-stream");
            ExtensionMap.Add(".etx", "text/x-setext");
            ExtensionMap.Add(".evy", "application/envoy");
            ExtensionMap.Add(".eml", "message/rfc822");
            ExtensionMap.Add(".eps", "application/postscript");
            ExtensionMap.Add(".flr", "x-world/x-vrml");
            ExtensionMap.Add(".fif", "application/fractals");
            ExtensionMap.Add(".gtar", "application/x-gtar");
            ExtensionMap.Add(".gif", "image/gif");
            ExtensionMap.Add(".gz", "application/x-gzip");
            ExtensionMap.Add(".hta", "application/hta");
            ExtensionMap.Add(".htc", "text/x-component");
            ExtensionMap.Add(".htt", "text/webviewhtml");
            ExtensionMap.Add(".h", "text/plain");
            ExtensionMap.Add(".hdf", "application/x-hdf");
            ExtensionMap.Add(".hlp", "application/winhlp");
            ExtensionMap.Add(".html", "text/html");
            ExtensionMap.Add(".htm", "text/html");
            ExtensionMap.Add(".hqx", "application/mac-binhex40");
            ExtensionMap.Add(".isp", "application/x-internet-signup");
            ExtensionMap.Add(".iii", "application/x-iphone");
            ExtensionMap.Add(".ief", "image/ief");
            ExtensionMap.Add(".ivf", "video/x-ivf");
            ExtensionMap.Add(".ins", "application/x-internet-signup");
            ExtensionMap.Add(".ico", "image/x-icon");
            ExtensionMap.Add(".jpg", "image/jpeg");
            ExtensionMap.Add(".jfif", "image/pjpeg");
            ExtensionMap.Add(".jpe", "image/jpeg");
            ExtensionMap.Add(".jpeg", "image/jpeg");
            ExtensionMap.Add(".js", "application/x-javascript");
            ExtensionMap.Add(".lsx", "video/x-la-asf");
            ExtensionMap.Add(".latex", "application/x-latex");
            ExtensionMap.Add(".lsf", "video/x-la-asf");
            ExtensionMap.Add(".manifest", "application/x-ms-manifest");
            ExtensionMap.Add(".mhtml", "message/rfc822");
            ExtensionMap.Add(".mny", "application/x-msmoney");
            ExtensionMap.Add(".mht", "message/rfc822");
            ExtensionMap.Add(".mid", "audio/mid");
            ExtensionMap.Add(".mpv2", "video/mpeg");
            ExtensionMap.Add(".man", "application/x-troff-man");
            ExtensionMap.Add(".mvb", "application/x-msmediaview");
            ExtensionMap.Add(".mpeg", "video/mpeg");
            ExtensionMap.Add(".m3u", "audio/x-mpegurl");
            ExtensionMap.Add(".mdb", "application/x-msaccess");
            ExtensionMap.Add(".mpp", "application/vnd.ms-project");
            ExtensionMap.Add(".m1v", "video/mpeg");
            ExtensionMap.Add(".mpa", "video/mpeg");
            ExtensionMap.Add(".me", "application/x-troff-me");
            ExtensionMap.Add(".m13", "application/x-msmediaview");
            ExtensionMap.Add(".movie", "video/x-sgi-movie");
            ExtensionMap.Add(".m14", "application/x-msmediaview");
            ExtensionMap.Add(".mpe", "video/mpeg");
            ExtensionMap.Add(".mp2", "video/mpeg");
            ExtensionMap.Add(".mov", "video/quicktime");
            ExtensionMap.Add(".mp3", "audio/mpeg");
            ExtensionMap.Add(".mpg", "video/mpeg");
            ExtensionMap.Add(".ms", "application/x-troff-ms");
            ExtensionMap.Add(".nc", "application/x-netcdf");
            ExtensionMap.Add(".nws", "message/rfc822");
            ExtensionMap.Add(".oda", "application/oda");
            ExtensionMap.Add(".ods", "application/oleobject");
            ExtensionMap.Add(".pmc", "application/x-perfmon");
            ExtensionMap.Add(".p7r", "application/x-pkcs7-certreqresp");
            ExtensionMap.Add(".p7b", "application/x-pkcs7-certificates");
            ExtensionMap.Add(".p7s", "application/pkcs7-signature");
            ExtensionMap.Add(".pmw", "application/x-perfmon");
            ExtensionMap.Add(".ps", "application/postscript");
            ExtensionMap.Add(".p7c", "application/pkcs7-mime");
            ExtensionMap.Add(".pbm", "image/x-portable-bitmap");
            ExtensionMap.Add(".ppm", "image/x-portable-pixmap");
            ExtensionMap.Add(".pub", "application/x-mspublisher");
            ExtensionMap.Add(".pnm", "image/x-portable-anymap");
            ExtensionMap.Add(".pml", "application/x-perfmon");
            ExtensionMap.Add(".p10", "application/pkcs10");
            ExtensionMap.Add(".pfx", "application/x-pkcs12");
            ExtensionMap.Add(".p12", "application/x-pkcs12");
            ExtensionMap.Add(".pdf", "application/pdf");
            ExtensionMap.Add(".pps", "application/vnd.ms-powerpoint");
            ExtensionMap.Add(".p7m", "application/pkcs7-mime");
            ExtensionMap.Add(".pko", "application/vndms-pkipko");
            ExtensionMap.Add(".ppt", "application/vnd.ms-powerpoint");
            ExtensionMap.Add(".pmr", "application/x-perfmon");
            ExtensionMap.Add(".pma", "application/x-perfmon");
            ExtensionMap.Add(".pot", "application/vnd.ms-powerpoint");
            ExtensionMap.Add(".prf", "application/pics-rules");
            ExtensionMap.Add(".pgm", "image/x-portable-graymap");
            ExtensionMap.Add(".qt", "video/quicktime");
            ExtensionMap.Add(".ra", "audio/x-pn-realaudio");
            ExtensionMap.Add(".rgb", "image/x-rgb");
            ExtensionMap.Add(".ram", "audio/x-pn-realaudio");
            ExtensionMap.Add(".rmi", "audio/mid");
            ExtensionMap.Add(".ras", "image/x-cmu-raster");
            ExtensionMap.Add(".roff", "application/x-troff");
            ExtensionMap.Add(".rtf", "application/rtf");
            ExtensionMap.Add(".rtx", "text/richtext");
            ExtensionMap.Add(".sv4crc", "application/x-sv4crc");
            ExtensionMap.Add(".spc", "application/x-pkcs7-certificates");
            ExtensionMap.Add(".setreg", "application/set-registration-initiation");
            ExtensionMap.Add(".snd", "audio/basic");
            ExtensionMap.Add(".stl", "application/vndms-pkistl");
            ExtensionMap.Add(".setpay", "application/set-payment-initiation");
            ExtensionMap.Add(".stm", "text/html");
            ExtensionMap.Add(".shar", "application/x-shar");
            ExtensionMap.Add(".sh", "application/x-sh");
            ExtensionMap.Add(".sit", "application/x-stuffit");
            ExtensionMap.Add(".spl", "application/futuresplash");
            ExtensionMap.Add(".sct", "text/scriptlet");
            ExtensionMap.Add(".scd", "application/x-msschedule");
            ExtensionMap.Add(".sst", "application/vndms-pkicertstore");
            ExtensionMap.Add(".src", "application/x-wais-source");
            ExtensionMap.Add(".sv4cpio", "application/x-sv4cpio");
            ExtensionMap.Add(".tex", "application/x-tex");
            ExtensionMap.Add(".tgz", "application/x-compressed");
            ExtensionMap.Add(".t", "application/x-troff");
            ExtensionMap.Add(".tar", "application/x-tar");
            ExtensionMap.Add(".tr", "application/x-troff");
            ExtensionMap.Add(".tif", "image/tiff");
            ExtensionMap.Add(".txt", "text/plain");
            ExtensionMap.Add(".texinfo", "application/x-texinfo");
            ExtensionMap.Add(".trm", "application/x-msterminal");
            ExtensionMap.Add(".tiff", "image/tiff");
            ExtensionMap.Add(".tcl", "application/x-tcl");
            ExtensionMap.Add(".texi", "application/x-texinfo");
            ExtensionMap.Add(".tsv", "text/tab-separated-values");
            ExtensionMap.Add(".ustar", "application/x-ustar");
            ExtensionMap.Add(".uls", "text/iuls");
            ExtensionMap.Add(".vcf", "text/x-vcard");
            ExtensionMap.Add(".wps", "application/vnd.ms-works");
            ExtensionMap.Add(".wav", "audio/wav");
            ExtensionMap.Add(".wrz", "x-world/x-vrml");
            ExtensionMap.Add(".wri", "application/x-mswrite");
            ExtensionMap.Add(".wks", "application/vnd.ms-works");
            ExtensionMap.Add(".wmf", "application/x-msmetafile");
            ExtensionMap.Add(".wcm", "application/vnd.ms-works");
            ExtensionMap.Add(".wrl", "x-world/x-vrml");
            ExtensionMap.Add(".wdb", "application/vnd.ms-works");
            ExtensionMap.Add(".wsdl", "text/xml");
            ExtensionMap.Add(".xml", "text/xml");
            ExtensionMap.Add(".xlm", "application/vnd.ms-excel");
            ExtensionMap.Add(".xaf", "x-world/x-vrml");
            ExtensionMap.Add(".xla", "application/vnd.ms-excel");
            ExtensionMap.Add(".xls", "application/vnd.ms-excel");
            ExtensionMap.Add(".xof", "x-world/x-vrml");
            ExtensionMap.Add(".xlt", "application/vnd.ms-excel");
            ExtensionMap.Add(".xlc", "application/vnd.ms-excel");
            ExtensionMap.Add(".xsl", "text/xml");
            ExtensionMap.Add(".xbm", "image/x-xbitmap");
            ExtensionMap.Add(".xlw", "application/vnd.ms-excel");
            ExtensionMap.Add(".xpm", "image/x-xpixmap");
            ExtensionMap.Add(".xwd", "image/x-xwindowdump");
            ExtensionMap.Add(".xsd", "text/xml");
            ExtensionMap.Add(".z", "application/x-compress");
            ExtensionMap.Add(".zip", "application/x-zip-compressed");
            ExtensionMap.Add(".*", "application/octet-stream");
        }

        internal static string GetMimeMapping(string fileExtension)
        {
            if (ExtensionMap.ContainsKey(fileExtension))
                return ExtensionMap[fileExtension];
            else
                return ExtensionMap[".*"];
        }
        internal static string SendEmail(EmailDetails oEmailDetails)
        {
            var mailMessage = new MailMessage();
            try
            {
                if (oEmailDetails.FileAttachments != null)
                {
                    foreach (var filePath in oEmailDetails.FileAttachments)
                    {
                        if (System.IO.File.Exists(filePath))
                        {
                            if (ExtensionMap == null)
                            {
                                ExtensionMap = new Dictionary<string, string>();
                                MimeMapping();
                            }

                            MemoryStream stream = new MemoryStream(System.IO.File.ReadAllBytes(filePath));
                            stream.Seek(0, System.IO.SeekOrigin.Begin);
                            System.Net.Mail.Attachment aa = new System.Net.Mail.Attachment(stream, Path.GetFileNameWithoutExtension(filePath), GetMimeMapping(Path.GetExtension(filePath)));
                            mailMessage.Attachments.Add(aa);
                        }
                    }
                }
                if (oEmailDetails.FileBytesAttachments != null)
                {
                    foreach (var fileDetails in oEmailDetails.FileBytesAttachments)
                    {
                        if (ExtensionMap == null)
                        {
                            ExtensionMap = new Dictionary<string, string>();
                            MimeMapping();
                        }
                        MemoryStream stream = new MemoryStream(fileDetails.FileBytes);
                        stream.Seek(0, System.IO.SeekOrigin.Begin);
                        if (oEmailDetails.Body.IndexOf("%Image%") != -1)
                        {
                            LinkedResource inline = new LinkedResource(stream);
                            inline.ContentId = Guid.NewGuid().ToString();
                            oEmailDetails.Body = oEmailDetails.Body.Replace("%Image%", inline.ContentId);
                            oEmailDetails.Body = oEmailDetails.Body.Replace("\\'", "'");
                            AlternateView alternateView = AlternateView.CreateAlternateViewFromString(oEmailDetails.Body, null, "text/html");
                            alternateView.LinkedResources.Add(inline);
                            mailMessage.AlternateViews.Add(alternateView);
                        }
                        else
                        {
                            System.Net.Mail.Attachment aa = new System.Net.Mail.Attachment(stream, fileDetails.FileName, GetMimeMapping(fileDetails.ContentType));
                            mailMessage.Attachments.Add(aa);
                        }
                    }
                }
                mailMessage.Subject = oEmailDetails.Subject;
                mailMessage.Body = oEmailDetails.Body;
                mailMessage.IsBodyHtml = oEmailDetails.IsHtml;
                mailMessage.From = new MailAddress(oEmailDetails.FromEmail, oEmailDetails.FromEmailName);
                foreach (var toEmail in oEmailDetails.ToEmail.Split(','))
                    mailMessage.To.Add(toEmail);
                if (!string.IsNullOrEmpty(oEmailDetails.CC))
                    foreach (var ccEmail in oEmailDetails.CC.Split(','))
                        mailMessage.CC.Add(ccEmail);
                if (!string.IsNullOrEmpty(oEmailDetails.BCC))
                    foreach (var bccEmail in oEmailDetails.BCC.Split(','))
                        mailMessage.Bcc.Add(bccEmail);
                var smtp = new SmtpClient(AppPress.Settings.Smtp.Host, AppPress.Settings.Smtp.Port);
                smtp.EnableSsl = AppPress.Settings.Smtp.EnableSsl;
                smtp.Credentials = new System.Net.NetworkCredential(AppPress.Settings.Smtp.UserName, AppPress.Settings.Smtp.Password);
                smtp.Send(mailMessage);
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
            return null;
        }
        // Encrypt the text

        static Dictionary<string, ICryptoTransform> desEncryptors = new Dictionary<string, ICryptoTransform>();

        //The function used to encrypt the text
        public static string EncryptDES(string strText, string newEncryptionKey = null)
        {
            if (strText.IsNullOrEmpty())
                return null;

            if (newEncryptionKey == null)
                newEncryptionKey = AppPress.Settings.encryptionKey;
            ICryptoTransform desObj;
            if (!desEncryptors.TryGetValue(newEncryptionKey, out desObj))
            {
                byte[] IV = { 0X12, 0X34, 0X56, 0X78, 0X90, 0XAB, 0XCD, 0XEF };
                string sDecrKey = newEncryptionKey;
                var byKey = System.Text.Encoding.UTF8.GetBytes(sDecrKey.Substring(0, 8));
                DESCryptoServiceProvider des = new DESCryptoServiceProvider();
                desObj = desEncryptors[newEncryptionKey] = des.CreateEncryptor(byKey, IV);
            }

            byte[] inputByteArray = Encoding.UTF8.GetBytes(strText);
            MemoryStream ms = new MemoryStream();
            CryptoStream cs = new CryptoStream(ms, desObj, CryptoStreamMode.Write);
            cs.Write(inputByteArray, 0, inputByteArray.Length);
            cs.FlushFinalBlock();
            return Convert.ToBase64String(ms.ToArray());
        }

        static Dictionary<string, ICryptoTransform> desDecryptors = new Dictionary<string, ICryptoTransform>();

        //The function used to decrypt the text
        public static string DecryptDES(string strText, string newEncryptionKey = null)
        {
            if (strText.IsNullOrEmpty())
                return strText;
            try
            {
                if (newEncryptionKey == null)
                    newEncryptionKey = AppPress.Settings.encryptionKey;
                ICryptoTransform desObj;
                if (!desDecryptors.TryGetValue(newEncryptionKey, out desObj))
                {
                    byte[] IV = { 0X12, 0X34, 0X56, 0X78, 0X90, 0XAB, 0XCD, 0XEF };
                    string sDecrKey = newEncryptionKey;
                    byte[] byKey = System.Text.Encoding.UTF8.GetBytes(sDecrKey.Substring(0, 8));
                    DESCryptoServiceProvider des = new DESCryptoServiceProvider();
                    desObj = desDecryptors[newEncryptionKey] = des.CreateDecryptor(byKey, IV);
                }
                MemoryStream ms = new MemoryStream();
                byte[] inputByteArray = new byte[strText.Length + 1];
                inputByteArray = Convert.FromBase64String(strText);
                var cs = new CryptoStream(ms, desObj, CryptoStreamMode.Write);

                cs.Write(inputByteArray, 0, inputByteArray.Length);
                cs.FlushFinalBlock();

                return Encoding.UTF8.GetString(ms.ToArray());
            }
            catch
            {
                //throw new Exception("Error in DES Decrypt of " + strText + " with Encryption Key: " + (newEncryptionKey == null ? "" : AppPress.ConvertToNonSecureString(newEncryptionKey)));
                throw new Exception("Error in DES Decrypt of " + strText);
            }
        }

        public static string ResetPassword()
        {
            string allowedChars = "abcdefghijkmnopqrstuvwxyzABCDEFGHJKLMNOPQRSTUVWXYZ0123456789";
            char[] chars = new char[6];
            Random rd = new Random();
            for (int i = 0; i < chars.Length; i++)
            {
                chars[i] = allowedChars[rd.Next(0, allowedChars.Length)];
            }
            string password = new string(chars);
            return password;
        }

        public static string CaptchaCode()
        {
            string allowedChars = "abcdefghijkmnopqrstuvwxyzABCDEFGHJKLMNOPQRSTUVWXYZ0123456789";
            char[] chars = new char[5];
            Random rd = new Random();
            for (int i = 0; i < chars.Length; i++)
            {
                chars[i] = allowedChars[rd.Next(0, allowedChars.Length)];
            }
            return new string(chars);

        }

        internal static string EscapeForCHash(string p)
        {
            if (p == null)
                return "null";
            return "\"" + p.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "\\n") + "\"";
        }
        public static void SplitCode(ref string str, ref string code, ref int functionId, ref Dictionary<string, int> codeFragments)
        {
            var c = new StringBuilder();
            int startIndex = 0;
            string search = "<!--@";
            string searchEnd = "@-->";
            while (true)
            {
                int index = str.IndexOf(search, startIndex);
                if (index == -1)
                    break;
                index += search.Length;
                int endIndex = str.IndexOf(searchEnd, index);
                if (endIndex == -1)
                    throw new Exception("Could not find ending '-->' of opening '<!--@' at char position:" + index + " in string:" + str);
                string codeFragment = str.Substring(index, endIndex - index).Trim();
                int fId;
                if (!codeFragments.TryGetValue(codeFragment, out fId))
                {
                    fId = ++functionId;
                    AppPress.SkinCode[fId] = codeFragment;
                    // create a compilable code string
                    c.Append("  case ").Append(fId).Append(": {var o = ").Append(codeFragment).Append(";return o==null?null:o.ToString();}\n");
                    codeFragments.Add(codeFragment, fId);
                }
                startIndex = endIndex + searchEnd.Length;
                startIndex -= str.Length;
                str = str.Substring(0, index - search.Length) + "{TempFunction" + fId + "}" + str.Substring(endIndex + searchEnd.Length);
                startIndex += str.Length;
            }
            code += c.ToString();
        }

        internal static string GenerateClasses(AppPress a, List<FormDef> formDefs, bool AppPress1, List<FormDef> formDefsParent)
        {
            if (formDefsParent != null)
                foreach (var formDef in formDefsParent)
                    InitializeFormDef(a, formDef, formDefsParent);
            AppPress.formDefs = new List<FormDef>();
            AppPress.formDefs.AddRange(formDefs);
            if (formDefsParent != null)
                AppPress.formDefs.AddRange(formDefsParent);
            var ExtensionFields = new List<FormField>();
            foreach (var formDef in formDefs)
            {
                foreach (var formField in formDef.formFields)
                {
                    formField.formDef = formDef;
                    if (formField.containerFormField != null)
                        formField.containerFormField = formField.formDef.formFields.Find(t => t.fieldName == formField.containerFormField.fieldName);
                }
            }
            var jsonCode = FormDef.Serialize(formDefs, typeof(List<FormDef>));
            foreach (var formDef in formDefs)
            {
                var skins = new List<FormSkin> { };
                OpenExtensionFields(a, formDef, skins);

            }
            string classCode = "";
            if (formDefs.Count() > 0)
            {

                foreach (var formDef in formDefs)
                    if (formDef.FormType != FormType.ContainerRowFormGenerated)
                    {
                        classCode += GenerateClassCode(a, formDef, formDefs, formDefsParent);
                    }
                classCode += @"[DataContract]
public static class FormDefs {
    public static string FormDefsJSon=" + EscapeForCHash(jsonCode) + @";
        public static string GenerateApplicationClasses(string[] jsFiles, string[] xmlFiles, string[] skinFiles)
        {
            return AppPress.GenerateAppPressClasses(jsFiles, xmlFiles, null, FormDefsJSon);
        }
    }
";
            }
            return classCode;
        }

        static string GetLine(string text, int lineNo)
        {
            if (lineNo == 0)
                return "";
            string[] lines = text.Replace("\r", "").Split('\n');
            return lines.Length >= lineNo ? lines[lineNo - 1] : null;
        }

        private static string GenerateClassCode(AppPress a, FormDef formDef, List<FormDef> formDefs, List<FormDef> formDefsParent)
        {
            var classNameF = formDef.formName;

            var classCode = "[DataContract]\npublic class " + classNameF + "Class : FormData ";
            classCode += @"{
/// <summary>
/// Internal Id assigned to the Form Definition
/// </summary>
";
            classCode += "\tpublic static long formDefId = " + formDef.id + "*10-AppPress.LocalInstanceId;\n";
            var idStr = ",formDefId";
            classCode += "\tpublic " + classNameF + "Class(AppPress a) : base(a" + idStr + "){}\n";
            classCode += "\tpublic " + classNameF + "Class(FormData formData) : base(formData){}\n";
            // generate Container and PopupContainer fields
            string className = null;
            foreach (var formDef1 in formDefs)
            {
                foreach (var formField1 in formDef1.formFields)
                {
                    var containerRowFormDef = formField1.GetContainerRowFormDef(a);
                    if (containerRowFormDef == null || containerRowFormDef.id != formDef.id)
                        continue;
                    if (className != null)
                        break;
                    if (className != null)
                    {
                        // form used as child form in 2 fields
                        className = null;
                        break;
                    }
                    className = formDef1.formName + "Class";
                    var formDef2 = formDef1;
                    while (formDef2.ContainerFormField != null)
                    {
                        formDef2 = formDef2.ContainerFormField.formDef;
                        className = formDef2.formName + "Class." + className;
                    }
                    break;
                }
                if (className != null)
                    break;
            }
            if (formDef.GenerationType == 0)
            {
                classCode += @"
    /// <summary>
    /// Redirects to this form
    /// </summary>
    /// <param name=""a""></param>
    /// <param name=""formId"">if null then create a new form, otherwise opens the form with the Id</param>
    /// <param name=""redirectParams"">Optional Parameters for Redirection. Can be null</param>
";
                classCode += "\tpublic static void Redirect(AppPress a, string formId, RedirectParams redirectParams) {a.Redirect( formDefId,formId, redirectParams);}\n";

            }
            if (formDef.GenerationType == 0 || formDef.GenerationType == 1)
            {

                if (className == null)
                    className = "FormData";
                classCode += @"
    /// <summary>
    /// Points to Form containing this form. If null then the form is root form of the Page.
    /// For example the forms in a FormContainer grid will have the parent form as container 
    /// </summary>
";
                classCode += "\tpublic " + className + " FormDataContainer { get { return (" + className + ")(this.containerFieldValue==null?null:this.containerFieldValue.FormData.CovertToFormDefClass()); } }";

            }
            if (formDef.GenerationType == 0 || formDef.GenerationType == 2)
            {
                classCode += @"
    /// <summary>
    /// Opens this form in a Popup
    /// </summary>
    /// <param name=""a""></param>
    /// <param name=""formId"">if null then create a new form, otherwise opens the form with the Id</param>
    /// <param name=""popupParams"">Optional Parameters for Popup. Can be null</param>
";
                classCode += "\tpublic static void Popup(AppPress a, string formId, PopupParams popupParams) {a.Popup(formDefId,formId, popupParams);}\n";

                var popupContainerClassName = "FormData";
                if (formDef.ContainerFormField != null)
                {
                    popupContainerClassName = formDef.ContainerFormField.formDef.formName + "Class";
                    var formDef3 = formDef.ContainerFormField.formDef;
                    while (formDef3.ContainerFormField != null)
                    {
                        popupContainerClassName = formDef3.ContainerFormField.formDef.formName + "Class." + popupContainerClassName;
                        formDef3 = formDef3.ContainerFormField.formDef;
                    }
                }
                classCode += @"
    /// <summary>
    /// Points to Form whose button created this Popup
    /// </summary>
";
                classCode += "\n\tpublic " + popupContainerClassName + @" FormDataPopupCaller { 
            get {";
                if (popupContainerClassName == "FormData")
                    classCode += " return (FormData)base.PopupContainer.CovertToFormDefClass();";
                else classCode += @"
                if (base.PopupContainer.GetType() == typeof(" + popupContainerClassName + @"))
                    return (" + popupContainerClassName + @")base.PopupContainer;
                else return new " + popupContainerClassName + @"(base.PopupContainer);  ";
                classCode += @"}}
";
            }
            // Generate Groups
            var groups = formDef.formFields.FindAll(t => t.GroupName != null).Select(t => t.GroupName).Distinct().ToList();
            if (groups.Count() > 0)
            {
                classCode += "\npublic enum Groups {\n";
                foreach (var fGroup in groups)
                    classCode += fGroup + ",\n";
                classCode += "\n}\n";
                classCode += "public void GroupHide(Groups group){base.GroupHide(group.ToString());}\n";
                classCode += "public void GroupReadonly(Groups group){base.GroupReadonly(group.ToString());}\n";
            }
            // Generate field classes
            foreach (var formField in formDef.formFields)
            {
                if (formField.Type == FormDefFieldType.UserControlScalar || formField.Type == FormDefFieldType.MergedForm)
                    continue;
                var formDataCode = @"
        /// <summary>
        /// Points to Form containing this field
        /// </summary>
";
                formDataCode += "\n\tpublic new " + classNameF + @"Class FormData { 
            get {
                if (base.FormData.GetType() == typeof(" + classNameF + @"Class))
                    return (" + classNameF + @"Class)base.FormData;
                else return new " + classNameF + @"Class(base.FormData);} }
                ";
                var hiddenCode = @"
        /// <summary>
        /// Hidden property of the field.
        /// </summary>
        public new FieldHiddenType Hidden { get { return base.Hidden; } set { base.Hidden = value; }}";

                formDataCode += "public " + formField.fieldName + "FieldClass(): base(){}\n";

                classCode += "\n[DataContract]\n";
                string fieldValueClass = null;
                var generateGet = true;
                switch (formField.Type)
                {
                    case FormDefFieldType.Text:
                    case FormDefFieldType.HTML:
                    case FormDefFieldType.TextArea:
                    case FormDefFieldType.Password:
                        fieldValueClass = "FieldValue";
                        formDataCode += "\npublic " + formField.fieldName + "FieldClass(FieldValue fieldValue): base(fieldValue){}\n";
                        classCode += @"
    public class " + formField.fieldName + @"FieldClass : FieldValue {
        public const long fieldDefId = " + formField.id + @";
        /// <summary>
        /// Value of the Field
        /// </summary>
        public string val {";
                        if (generateGet)
                            classCode += "get { return GetFieldString(); }";
                        classCode += @"set { SetFieldString(value); } }" +
hiddenCode +
formDataCode + @"
    }";
                        break;
                    case FormDefFieldType.FormContainerDynamic:
                        var rowFormDef = formField.GetContainerRowFormDef(a);
                        string rowFormName;
                        var fcfvName = "FormContainerGridFieldValue";
                        if (rowFormDef == null)
                        {
                            rowFormName = "FormData";
                            fcfvName = "FormContainerDynamicFieldValue";
                        }
                        else
                            rowFormName = rowFormDef.formName + "Class";
                        classCode += @"
    public class " + formField.fieldName + @"FieldClass :  " + fcfvName + @"{
        public const long fieldDefId = " + formField.id + @";
        /// <summary>
        /// Value of the Field
        /// </summary>
        public List<" + rowFormName + @"> val { get { var fds = GetFieldFormContainer(); var l = new List<" + rowFormName + @">();foreach (var f in fds) l.Add(";
                        if (rowFormName != "FormData")
                            classCode += "f.GetType() == typeof(" + rowFormName + @")?(" + rowFormName + @")f:new " + rowFormName + @"(f)";
                        else
                            classCode += "(FormData)f.CovertToFormDefClass()";
                        classCode += @"
); return l;  } set { var l = new List<FormData>();foreach (var f in value) l.Add((FormData)f);SetFieldFormContainer(l); }}" +
                            hiddenCode +
                            formDataCode;
                        classCode += @"
    }";
                        foreach (var rFormDef in formDefs.FindAll(t => t.ContainerFormField == formField))
                            classCode += GenerateClassCode(a, rFormDef, formDefs, formDefsParent);

                        break;
                    case FormDefFieldType.PickMultiple:
                        fieldValueClass = "PickFieldValue";
                        formDataCode += "\npublic " + formField.fieldName + "FieldClass(PickFieldValue fieldValue): base(fieldValue){}\n";
                        classCode += @"
    public class " + formField.fieldName + @"FieldClass : PickFieldValue {
        public const long fieldDefId = " + formField.id + @";
        /// <summary>
        /// Value of the Field
        /// </summary>
        public List<int> val {get { return GetFieldPickMultiple();} set { SetFieldPickMultiple(value); }}" +
hiddenCode +
formDataCode + @"
    }";
                        break;
                    case FormDefFieldType.Button:
                    case FormDefFieldType.Redirect:
                        fieldValueClass = "ButtonFieldValue";
                        formDataCode += "\npublic " + formField.fieldName + "FieldClass(ButtonFieldValue fieldValue): base(fieldValue){}\n";
                        classCode += @"
    public class " + formField.fieldName + @"FieldClass : ButtonFieldValue {
        public const long fieldDefId = " + formField.id + @";
        /// <summary>
        /// Value of the Field
        /// </summary>
        public string val {";
                        if (generateGet)
                            classCode += "get { return GetFieldString(); }";
                        classCode += @"set { SetFieldString(value); } }
        " +
               formDataCode + @"}";
                        break;
                    case FormDefFieldType.Number:
                        fieldValueClass = "NumberFieldValue";
                        formDataCode += "\npublic " + formField.fieldName + "FieldClass(NumberFieldValue fieldValue): base(fieldValue){}\n";
                        classCode += @"
    public class " + formField.fieldName + @"FieldClass : NumberFieldValue {
        public const long fieldDefId = " + formField.id + @";
        /// <summary>
        /// Value of the Field
        /// </summary>
        public decimal? val {";
                        if (generateGet)
                            classCode += "get { return GetFieldDecimal();  }";
                        classCode += @" set { SetFieldDecimal(value); }}" +
hiddenCode +
formDataCode + @"
    }";
                        break;
                    case FormDefFieldType.DateTime:
                        fieldValueClass = "DateTimeFieldValue";
                        formDataCode += "\npublic " + formField.fieldName + "FieldClass(DateTimeFieldValue fieldValue): base(fieldValue){}\n";
                        classCode += @"
    public class " + formField.fieldName + @"FieldClass : DateTimeFieldValue {
        public const long fieldDefId = " + formField.id + @";
        /// <summary>
        /// Value of the Field
        /// </summary>
        public DateTime? val { ";
                        if (generateGet)
                            classCode += "get { return GetFieldDateTime();  }";
                        classCode += @" set { SetFieldDateTime(value); }}" +
hiddenCode +
formDataCode + @"
    }";
                        break;
                    case FormDefFieldType.Checkbox:
                        fieldValueClass = "PickFieldValue";
                        formDataCode += "\npublic " + formField.fieldName + "FieldClass(PickFieldValue fieldValue): base(fieldValue){}\n";
                        classCode += @"
    public class " + formField.fieldName + @"FieldClass : PickFieldValue {
        public const long fieldDefId = " + formField.id + @";
        /// <summary>
        /// Value of the Field
        /// </summary>
        public bool val { ";
                        if (generateGet)
                            classCode += "get { return GetFieldBool();  }";
                        classCode += @" set { SetFieldBool(value);  } }" +
hiddenCode +
formDataCode + @"
    }";
                        break;
                    case FormDefFieldType.Pickone:
                        fieldValueClass = "PickFieldValue";
                        formDataCode += "\npublic " + formField.fieldName + "FieldClass(PickFieldValue fieldValue): base(fieldValue){}\n";
                        classCode += @"
    public class " + formField.fieldName + @"FieldClass : PickFieldValue {
        public const long fieldDefId = " + formField.id + @";
        /// <summary>
        /// Value of the Field
        /// </summary>
        public string val {";
                        if (generateGet)
                            classCode += "get { return GetFieldString();  }";
                        classCode += @" set { SetFieldString(value); }}
        public string AutoCompleteTerm { get {return this.autoCompleteTerm;}}
" +
hiddenCode +
formDataCode + @"
    }";
                        break;
                    case FormDefFieldType.ForeignKey:
                        classCode += @"
    public class " + formField.fieldName + @"FieldClass : FieldValue {
        public const long fieldDefId = " + formField.id + @";
        /// <summary>
        /// Value of the Field
        /// </summary>
        public string val {get { return GetFieldString();  } set { SetFieldString(value); }}" +
hiddenCode +
formDataCode + @"
    }";
                        break;
                    case FormDefFieldType.MultiFileUpload:
                        classCode += @"
    public class " + formField.fieldName + @"FieldClass : FieldValue {
        public const long fieldDefId = " + formField.id + @";
        /// <summary>
        /// Value of the Field
        /// </summary>
        public string val { get { return GetFieldString();  } set { SetFieldString(value); }}" +
hiddenCode +
formDataCode + @"
    }";
                        break;
                    case FormDefFieldType.FileUpload:
                        fieldValueClass = "FileUploadFieldValue";
                        formDataCode += "\npublic " + formField.fieldName + "FieldClass(FileUploadFieldValue fieldValue): base(fieldValue){}\n";
                        string valString;
                        if (formField.DoNotSaveInDB)
                            valString = "string val { get { return GetFieldString();  } set { SetFieldString(value); }}";
                        else
                            valString = "int? val { get { return GetFieldInt();  } set { SetFieldInt(value); }}";
                        classCode += @"
    public class " + formField.fieldName + @"FieldClass : FileUploadFieldValue {
        public const long fieldDefId = " + formField.id + @";
        /// <summary>
        /// Value of the Field
        /// </summary>
        public " + valString +
hiddenCode +
formDataCode + @"
    }";
                        break;
                }
                if (fieldValueClass != null)
                    classCode += "\n\tpublic " + formField.fieldName + "FieldClass " + formField.fieldName + " { get { var v = GetFieldValue(\"" + formField.fieldName + "\"); if (v.GetType()==typeof(" + formField.fieldName + "FieldClass)) return (" + formField.fieldName + "FieldClass)v; else return new " + formField.fieldName + "FieldClass((" + fieldValueClass + ")v); } }\n\n";
                else
                    classCode += "\n\tpublic " + formField.fieldName + "FieldClass " + formField.fieldName + " { get { return (" + formField.fieldName + "FieldClass)GetFieldValue(\"" + formField.fieldName + "\"); } }\n\n";

            }
            classCode += "\n}\n\n";
            return classCode;
        }

        private static string GenerateBoolean(bool p)
        {
            return p ? "true" : "false";
        }

        public static void _LoadFormDefs(AppPress a)
        {
            AppPress.SkinAssembly = null;
            try
            {

                string code = "";
                int functionId = 0;
                foreach (var assembly in AppPress.Assemblies)
                {
                    var obj = assembly.assembly.GetType("ApplicationClasses.FormDefs");
                    if (obj != null)
                    {
                        var formDefsJSon = ((string)obj.GetField("FormDefsJSon").GetValue(obj));
                        DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(List<FormDef>), new DataContractJsonSerializerSettings
                        {
                            UseSimpleDictionaryFormat = true,
                            KnownTypes = null
                        });
                        // Deserialize the data and read it from the instance.
                        var formDefs = (List<FormDef>)ser.ReadObject(new MemoryStream(Encoding.UTF8.GetBytes(formDefsJSon)));
                        foreach (var formDef in formDefs)
                            formDef.assembly = assembly;
                        AppPress.formDefs.AddRange(formDefs);
                    }
                }

                for (int i = 0; i < AppPress.formDefs.Count(); ++i)
                    foreach (var formField in AppPress.formDefs[i].formFields)
                    {
                        formField.formDef = AppPress.formDefs[i];
                        switch (formField.Type)
                        {
                            case FormDefFieldType.FileUpload:
                                if (!formField.DoNotSaveInDB && formField.formDef.TableName != null)
                                {
                                    var destTableName = "Application_Files";
                                    if (!a.site.CheckForeignKey(formField.formDef.TableName, formField.fieldName, destTableName, "id"))
                                        throw new Exception(formField.GetDescription() + " Could not find a ForeignKey From " + formField.formDef.TableName + ":" + formField.fieldName + " to " + destTableName + ":id");
                                    AppPress.EmbeddedFormsTables.Add(new EmbededFormField { TableName = formField.formDef.TableName, FieldName = formField.fieldName, EmbeddedTableName = destTableName });
                                }
                                break;
                            case FormDefFieldType.Redirect:
                                {
                                    formField.Type = FormDefFieldType.Button;
                                    formField.OriginalType = (int)FormDefFieldType.Redirect;
                                    formField.NoSubmit = true;
                                    var formName = formField.FormNameProperty ?? formField.fieldName;
                                    var formDef1 = AppPress.FindFormDef(formName);
                                    if (formDef1 == null)
                                        throw new Exception(formField.GetDescription() + " Could not find Redirect Form: " + formName);
                                    if (formDef1.FormType != FormType.Application)
                                        throw new Exception(formField.GetDescription() + " Redirect Form: " + formName + " should be of type Application");
                                    if (formDef1.TableName != null)
                                        throw new Exception(formField.GetDescription() + " Cannot Redirect to Form: " + formName + " as is it bound to a Table. Use OnClick Logic Function to Redirect to this form");
                                    var serverFunction = new ServerFunction(a, FunctionType.OnClick, "Redirect");
                                    serverFunction.Parameters.Add(new ServerFunctionParameter("FormName", formName));
                                    formField.FieldFunctions.Add(serverFunction);
                                    break;
                                }
                            case FormDefFieldType.FormContainerDynamic:
                                {
                                    if (formField.OriginalType == (int)FormDefFieldType.EmbeddedForm)
                                    {
                                        var domainFunction = new ServerFunction(a, FunctionType.Domain, "BindEmbeddedForm");
                                        var formName = formField.FormNameProperty;
                                        var formDef1 = AppPress.FindFormDef(formName);
                                        if (formDef1 == null)
                                            throw new Exception(formField.GetDescription() + " Could not find Embedded Form: " + formName);
                                        if (formDef1.FormType != FormType.Application)
                                            throw new Exception(formDef1.GetDescription() + " Embedded Form should be of type Application");
                                        if (formDef1.TableName == null)
                                            throw new Exception(formDef1.GetDescription() + " Cannot Embed as is does not have a TableName Property.");
                                        if (formField.formDef.TableName == null)
                                            throw new Exception(formField.formDef.GetDescription() + " Cannot Embed into as it does not have a TableName Property.");
                                        var fieldName = formField.fieldName.Substring(0, formField.fieldName.Length - "Container".Length);
                                        if (!a.site.CheckForeignKey(formField.formDef.TableName, fieldName, formDef1.TableName, "id"))
                                            throw new Exception(formField.GetDescription() + " Could not find a ForeignKey From " + formField.formDef.TableName + ":" + fieldName + " to " + formDef1.TableName + ":id");
                                        AppPress.EmbeddedFormsTables.Add(new EmbededFormField { TableName = formField.formDef.TableName, FieldName = fieldName, EmbeddedTableName = formDef1.TableName });
                                        // create a hidden field of type Text which will have the primary key of the embeded form
                                        domainFunction.Parameters.Add(new ServerFunctionParameter("FormName", formName));
                                        formField.FieldFunctions.Add(domainFunction);
                                    }
                                    break;
                                }
                        }
                    }
                for (int i = 0; i < AppPress.formDefs.Count(); ++i)
                {
                    // make formDefId unique across instances
                    AppPress.formDefs[i].id = AppPress.formDefs[i].id * 10 - AppPress.LocalInstanceId;
                }
                for (int i = 0; i < AppPress.formDefs.Count(); ++i)
                {
                    AppPress.formDefs[i] = InitializeFormDef(a, AppPress.formDefs[i], AppPress.formDefs);
                }
                AppPress.originalFormDefs = (List<FormDef>)FormDef.Deserialize(FormDef.Serialize(AppPress.formDefs, typeof(List<FormDef>)), typeof(List<FormDef>));
                FormDef.knownTypes = null; // will need to create it again after OpenExtensionFields

                List<string> skinFiles = new List<string>();

                foreach (var assembly in AppPress.Assemblies)
                {
                    var dllName = Path.GetFileName(assembly.assembly.Location);
                    var folderName = dllName.Trim().Replace(".dll", "", StringComparison.OrdinalIgnoreCase);
                    var folder = HttpContext.Current.Server.MapPath("~/Skins/" + folderName + "/");
                    if (Directory.Exists(folder))
                    {
                        var referenceSkinFiles = Directory.EnumerateFiles(folder, "*.html", SearchOption.TopDirectoryOnly);
                        foreach (var otherSkin in referenceSkinFiles)
                            skinFiles.Add(otherSkin);
                        referenceSkinFiles = Directory.EnumerateFiles(folder, "*.fo", SearchOption.TopDirectoryOnly);
                        foreach (var otherSkin in referenceSkinFiles)
                            skinFiles.Add(otherSkin);
                    }

                }
                var otherSkinFiles = Directory.EnumerateFiles(HttpContext.Current.Server.MapPath("~/Skins/"), "*.html", SearchOption.TopDirectoryOnly);
                foreach (var otherSkin in otherSkinFiles)
                {
                    if (skinFiles.Any(s => Path.GetFileNameWithoutExtension(s).Equals(Path.GetFileNameWithoutExtension(otherSkin), StringComparison.OrdinalIgnoreCase)))
                        continue;
                    skinFiles.Add(otherSkin);
                }

                AppPress.skins = new Dictionary<string, SkinFileData>(StringComparer.OrdinalIgnoreCase);
                var skin = FileTexts.PageDesignerHtml;
                SplitCode(ref skin, ref code, ref functionId, ref AppPress.codeFragments);
                AppPress.skins.Add("PageDesigner.html", new SkinFileData { skin = skin });
                foreach (var skinFile in skinFiles)
                {
                    skin = System.IO.File.ReadAllText(skinFile);
                    skin = String.Join(Environment.NewLine,
                    skin.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                         .Select(s => s.Trim()).Where(s => s != "\r" && s != "\n" && s != "\r\n" && s != "\n\r"));
                    var fId = functionId;
                    SplitCode(ref skin, ref code, ref functionId, ref AppPress.codeFragments);
                    AppPress.skins.Add(Path.GetFileName(skinFile), new SkinFileData { skin = skin, skinFileName = skinFile, functionId = fId });

                }
                var foskinFiles = Directory.EnumerateFiles(HttpContext.Current.Server.MapPath("~/Skins/"), "*.fo", SearchOption.TopDirectoryOnly);
                foreach (var skinFile in foskinFiles)
                {
                    skin = System.IO.File.ReadAllText(skinFile);
                    SplitCode(ref skin, ref code, ref functionId, ref AppPress.codeFragments);
                    AppPress.skins.Add(Path.GetFileName(skinFile), new SkinFileData { skin = skin });
                }
                foreach (var formDef in AppPress.formDefs)
                {
                    // add foreignKey field if neededs
                    /*
                                        if (a.site != null && formDef.TableName != null && formDef.formFields.Find(t => t.Type == FormDefFieldType.ForeignKey) == null && formDef.FormType == FormType.ContainerRowFormGenerated &&
                                                        formDef.ContainerFormField.formDef != null // from Forms of other Dlls
                                                        )
                                        {
                                            var tableName = formDef.ContainerFormField.formDef.TableName;
                                            if (tableName != null)
                                            {
                                                var q = a.site.GetForeignKeysQuery(tableName, formDef.ContainerFormField.formDef.PrimaryKey);
                                                var dr = a.site.ExecuteQuery(q);
                                                try
                                                {
                                                    string fkColumName = null;
                                                    while (dr.Read())
                                                    {
                                                        var fkTableName = dr.GetString(0);
                                                        if (fkTableName == formDef.TableName)
                                                        {
                                                            if (fkColumName != null)
                                                                throw new Exception("Found 2 Foreign Keys from Table: " + tableName + " Column: " + formDef.ContainerFormField.formDef.PrimaryKey + " to Table: " + formDef.TableName + " Columns: " + fkColumName + "," + dr.GetString(1) + ".\nAdd a ForeignKey Field in Form: " + formDef.formName + " to resolve.");
                                                            fkColumName = dr.GetString(1);
                                                            if (formDef.formFields.Find(t => t.fieldName == fkColumName) != null)
                                                                fkColumName = null;
                                                        }
                                                    }
                                                    if (fkColumName != null)
                                                        formDef.formFields.Add(new FormField(fkColumName, FormDefFieldType.ForeignKey, formDef));
                                                }
                                                finally
                                                {
                                                    dr.Close();
                                                }
                                            }
                                        }*/
                    SkinFileData skinFileData;
                    AppPress.skins.TryGetValue(formDef.formName + ".html", out skinFileData);
                    if (formDef.Skins == null)
                        formDef.Skins = new List<FormSkin>();
                    if (skinFileData == null)
                    {
                        var htmlSkin = formDef.GenerateSkin(a, false, null);
                        SplitCode(ref htmlSkin, ref code, ref functionId, ref AppPress.codeFragments);
                        formDef.Skins.Add(new FormSkin { skinType = SkinType.HTML, skin = htmlSkin });
                    }
                    else
                        formDef.Skins.Add(new FormSkin { skinType = SkinType.HTML, skin = skinFileData.skin, skinFileName = skinFileData.skinFileName, skinFileTime = System.IO.File.GetLastWriteTime(skinFileData.skinFileName) });

                    AppPress.skins.TryGetValue(formDef.formName + ".fo", out skinFileData);
                    if (skinFileData == null)
                    {
                        a.skinType = SkinType.FO;
                        var foSkin = formDef.GenerateSkin(a, false, null);
                        a.skinType = SkinType.HTML;
                        SplitCode(ref foSkin, ref code, ref functionId, ref AppPress.codeFragments);
                        formDef.Skins.Add(new FormSkin { skinType = SkinType.FO, skin = foSkin });
                    }
                    else
                    {
                        var suffix = "";
                        int index = 0;
                        while (true)
                        {
                            var formSkin = new FormSkin { index = index, skinType = SkinType.FO, skin = skinFileData.skin, skinFileName = formDef.formName + suffix + ".fo" };
                            formSkin.skinFileTime = System.IO.File.GetLastWriteTime(formSkin.skinFileName);
                            formDef.Skins.Add(formSkin);
                            index++;
                            suffix = "-" + index;
                            AppPress.skins.TryGetValue(formDef.formName + suffix + ".fo", out skinFileData);
                            if (skinFileData == null)
                                break;
                        }
                    }
                }
                // move plugin and merged forms to top so that any nested UserControlScalar gets opened first
                AppPress.formDefs.Sort((x, y) => ((x.FormType == FormType.MergedForm || x.FormType == FormType.PluginForm) ? 0 : 1) - ((y.FormType == FormType.MergedForm || y.FormType == FormType.PluginForm) ? 0 : 1));

                foreach (var formDef in AppPress.formDefs)
                {
                    if (formDef.FormType == FormType.ContainerRowFormGenerated && formDef.GenerationType == 1)
                        continue;
                    OpenExtensionFields(a, formDef, formDef.Skins);
                }
                foreach (var formDef in AppPress.formDefs)
                {
                    if (formDef.FormType != FormType.MergedForm)
                    {
                        var dupFormDef = AppPress.formDefs.Find(t => t.formName == formDef.formName && t != formDef && t.FormType != FormType.ContainerRowFormGenerated);
                        if (dupFormDef != null)
                            throw new AppPressException("Duplicate FormName: " + dupFormDef.formName);
                    }
                    foreach (var formField in formDef.formFields)
                    {

                        if (a.site != null) // from TT build
                        {
                            if (formField.Type != FormDefFieldType.Button && formField.Type != FormDefFieldType.FormContainerDynamic)
                            {
                                // if in DB column is not null, Add Required Validation
                                if (formField.Type != FormDefFieldType.Checkbox)
                                    if (!formField.DoNotSaveInDB && formDef.TableName != null && !formField.Required && !formField.Static)
                                    {
                                        var tableSchema = "TABLE_SCHEMA";
                                        if (a.site.databaseType == DatabaseType.SqlServer)
                                            tableSchema = "TABLE_CATALOG";
                                        var obj = a.ExecuteScalar(@"
                                                Select * From INFORMATION_SCHEMA.COLUMNS 
                                                Where is_nullable = 'NO' and table_name='" + formDef.TableName + @"' and column_name='" + formField.fieldName + @"'  and " + tableSchema + "='" + a.site.dbName + "'");
                                        if (obj != null)
                                        {
                                            formField.Required = true;
                                        }
                                    }
                            }
                            if (formField.Type == FormDefFieldType.PickMultiple && formField.Style == FormDefFieldStyle.None)
                                formField.Style = FormDefFieldStyle.Checkboxes;
                            if (formField.Type == FormDefFieldType.Number && formDef.TableName != null)
                            {
                                var tableSchema = "TABLE_SCHEMA";
                                if (a.site.databaseType == DatabaseType.SqlServer)
                                    tableSchema = "TABLE_CATALOG";
                                if (!formField.decimalsAssigned)
                                {
                                    var o = a.ExecuteString(@"
                                    Select Numeric_Scale From INFORMATION_SCHEMA.COLUMNS 
                                    Where data_type='decimal' and table_name='" + formDef.TableName + @"' and column_name='" + formField.fieldName + @"'  and " + tableSchema + "='" + a.site.dbName + "'");
                                    if (o != null)
                                        formField.decimals = int.Parse(o);
                                }
                                if (formField.MinimumValue == null)
                                {
                                    var o = a.ExecuteString(@"
                                    Select " + (a.site.databaseType == DatabaseType.SqlServer ? "Data_Type" : "Column_Type") + @" From INFORMATION_SCHEMA.COLUMNS 
                                    Where data_type='decimal' and table_name='" + formDef.TableName + @"' and column_name='" + formField.fieldName + @"'  and " + tableSchema + "='" + a.site.dbName + "'");
                                    if (o != null && o.IndexOf("unsigned") != -1)
                                        formField.MinimumValue = 0.0m;
                                }
                                if (formField.MaximumValue == null)
                                {
                                    var o = a.ExecuteString(@"
                                    Select NUMERIC_PRECISION-NUMERIC_SCALE From INFORMATION_SCHEMA.COLUMNS 
                                    Where data_type='decimal' and table_name='" + formDef.TableName + @"' and column_name='" + formField.fieldName + @"'  and " + tableSchema + "='" + a.site.dbName + "'");
                                    if (o != null)
                                        formField.MaximumValue = (decimal)Math.Pow(10.0, double.Parse(o));
                                }
                            }
                            if (formField.Type == FormDefFieldType.Text || formField.Type == FormDefFieldType.TextArea)
                                if (!formField.DoNotSaveInDB && formField.MaxChars == null && formDef.TableName != null)
                                    if (formField.EncryptionType == EncryptionType.None || formField.EncryptionType == null)
                                    {
                                        var tableSchema = "TABLE_SCHEMA";
                                        if (a.site.databaseType == DatabaseType.SqlServer)
                                            tableSchema = "TABLE_CATALOG";
                                        var o = a.ExecuteString(@"
                            Select CHARACTER_MAXIMUM_LENGTH From INFORMATION_SCHEMA.COLUMNS 
                            Where table_name='" + formDef.TableName + @"' and column_name='" + formField.fieldName + @"'  and " + tableSchema + "='" + a.site.dbName + "'");
                                        if (o != null)
                                        {
                                            long maxChars = long.Parse(o);
                                            if (maxChars < 10000)
                                                formField.MaxChars = (int)maxChars;
                                        }
                                    }
                            if (formField.Type == FormDefFieldType.PickMultiple || formField.Type == FormDefFieldType.Pickone || formField.Type == FormDefFieldType.Checkbox)
                            {
                                formField.BuildOptions(a, formDef);
                            }
                        }
                        var dupField = formDef.formFields.Find(t => t.fieldName == formField.fieldName && t.id != formField.id);
                        if (dupField != null)
                            throw new Exception("Duplicate " + formField.GetDescription());
                        if (formField.Type == FormDefFieldType.FormContainerDynamic)
                        {
                            formField.rowFormDef = formField.GetContainerRowFormDef(a);
                            var t1 = Util.GetType(a, formDef, formField.id);
                            if (t1 != null)
                            {
                                var method = Util.GetMethod(a, "Domain", new Type[] { AppPress.Settings.ApplicationAppPress, t1 }, true);
                                if (method != null && formField.FieldFunctions != null)
                                {
                                    formField.FieldFunctions.RemoveAll(t => t.ServerFunctionType == FunctionType.Domain);
                                }
                            }
                        }
                    }
                }
                code = @"using System; 
                         using System.Web; 
                         using System.Collections.Generic; 
                         using AppPressFramework; 
                         using Application;

                         namespace SkinCode 
                         {
                             public class SplitCodeExecute 
                             {
                                 public static string CodeFragmentExecute(AppPress a,int key)
                                 {
                                     switch (key) 
                                     {
                                     " + code + @"
                                     default: 
                                         throw new NotImplementedException();
                                     }
                                 }
                             }
                         }";
                var csc = new CSharpCodeProvider();
                var parameters = new CompilerParameters();
                foreach (var assembly in AppPress.Assemblies)
                    parameters.ReferencedAssemblies.Add(assembly.assembly.Location);
                parameters.ReferencedAssemblies.Add("System.dll");
                parameters.ReferencedAssemblies.Add("System.Web.dll");

                string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                Directory.CreateDirectory(tempDirectory);
                parameters.OutputAssembly = Path.Combine(tempDirectory, "AppPressSkinCode.dll");
                //if (AppPress.DEBUG)
                //{
                //    parameters.GenerateInMemory = false; //default
                //    parameters.TempFiles = new TempFileCollection(Environment.GetEnvironmentVariable("TEMP"), true);
                //    parameters.IncludeDebugInformation = true;
                //}
                //parameters.CompilerOptions = "/keyfile:c:\\AppPress\\AppPress\\AppPress.snk";
                CompilerResults results = csc.CompileAssemblyFromSource(parameters, code);
                if (results.Errors.HasErrors)
                {
                    string errors = "";
                    results.Errors.Cast<CompilerError>().ToList().ForEach(error => errors += GetLine(code, error.Line) + "\nLine: " + error.Line + " " + error.ErrorText + "\n");
                    throw new Exception("Error in Skin Code:" + errors + "\n\n");
                }
                AppPress.SkinAssembly = results.CompiledAssembly;

            }
            catch
            {
                AppPress.SkinAssembly = null;
                AppPress.formDefs = null; //In case of error regarding LoadFormDef, setting null will call the LoadFunction again and show the real error on page. Avoid app_start error here.
                throw;
            }
        }

        private static void OpenExtensionFields(AppPress a, FormDef formDef, List<FormSkin> skins)
        {
            for (int i = 0; i < formDef.formFields.Count(); ++i)
            {
                var formField = formDef.formFields[i];
                FormDef containerRowForm = null;
                bool open = formField.Type == FormDefFieldType.MergedForm || formField.Type == FormDefFieldType.UserControlScalar;
                if (!open)
                {
                    if (formField.Type == FormDefFieldType.FormContainerGrid || formField.Type == FormDefFieldType.FormContainerDynamic)
                    {
                        containerRowForm = formField.GetContainerRowFormDef(a);
                        open = containerRowForm != null;
                    }
                }
                if (!open)
                    continue;
                var fieldSkins = new List<List<FormSkin>>();
                var indexes = new List<List<string>>();
                var skinsOriginal = new List<FormSkin>();
                for (int j = 0; j < skins.Count(); ++j)
                {
                    skinsOriginal.Add(skins[j]);
                    fieldSkins.Add(new List<FormSkin>());
                    indexes.Add(new List<string>());
                    if (skins[j] != null)
                    {
                        string fieldSkin, fieldUnique, beginMarker, endMarker;
                        bool outer;
                        skins[j].skin = formField.ExtractBetweenMarkers(a, skins[j].skin, 0, "", out fieldUnique, out fieldSkin, out outer, out beginMarker, out endMarker);
                        if (fieldSkin != null)
                        {
                            if (containerRowForm != null)
                            {
                                while (true)
                                {
                                    string headerSkin, headerUnique, beginMarker1, endMarker1;
                                    fieldSkin = formField.ExtractBetweenMarkers(a, fieldSkin, 0, "Header", out headerUnique, out headerSkin, out outer, out beginMarker1, out endMarker1);
                                    if (headerSkin == null)
                                        break;
                                    fieldSkins[j].Add(new FormSkin { skinType = skins[j].skinType, skin = beginMarker1 + headerSkin + endMarker1 });
                                    indexes[j].Add(headerUnique);
                                }
                                while (true)
                                {
                                    string rowSkin, rowUnique, beginMarker1, endMarker1;
                                    fieldSkin = formField.ExtractBetweenMarkers(a, fieldSkin, 0, "Row", out rowUnique, out rowSkin, out outer, out beginMarker1, out endMarker1);
                                    if (rowSkin == null)
                                        break;
                                    fieldSkins[j].Add(new FormSkin { skinType = skins[j].skinType, skin = beginMarker1 + rowSkin + endMarker1 });
                                    indexes[j].Add(rowUnique);
                                }
                                skins[j].skin = skins[j].skin.Replace(fieldUnique, beginMarker + fieldSkin + endMarker);
                            }
                            else
                            {
                                fieldSkins[j].Add(new FormSkin { skinType = skins[j].skinType, skin = beginMarker + fieldSkin + endMarker });
                                indexes[j].Add(fieldUnique);
                            }
                        }
                    }
                }
                if (containerRowForm != null && (containerRowForm.FormType == FormType.ContainerRowFormGenerated || containerRowForm.FormType == FormType.ContainerRowForm))
                {
                    var fs = new List<FormSkin>();
                    for (int j = 0; j < fieldSkins.Count(); ++j)
                        for (int k = 0; k < fieldSkins[j].Count(); ++k)
                        {
                            fs.Add(fieldSkins[j][k]);
                        }
                    string bm = "<!--|AppPress." + formField.fieldName + ".RowBegin|-->";
                    string em = "<!--|AppPress." + formField.fieldName + ".RowEnd|-->";
                    foreach (var formSkin in containerRowForm.Skins)
                        fs.Add(new FormSkin { skinType = formSkin.skinType, skin = bm + formSkin.skin + em });
                    OpenExtensionFields(a, containerRowForm, fs);
                    int fsIndex = 0;
                    for (int j = 0; j < fieldSkins.Count(); ++j)
                        for (int k = 0; k < fieldSkins[j].Count(); ++k)
                        {
                            skins[j].skin = skins[j].skin.Replace(indexes[j][k], fs[fsIndex].skin);
                            fsIndex++;
                        }
                    foreach (var formSkin in containerRowForm.Skins)
                    {
                        formSkin.skin = fs[fsIndex].skin.Substring(bm.Length, fs[fsIndex].skin.Length - bm.Length - em.Length);
                        fsIndex++;
                    }
                    continue;
                }
                List<FormDef> extensionFormDefs = null;
                if (formField.ExtensionFormName == null)
                {
                    // MergedForm, UserControlScalar
                    extensionFormDefs = AppPress.formDefs.FindAll(t => t.formName == formField.fieldName);
                    if (formField.Type == FormDefFieldType.MergedForm)
                    {
                        var mismatch = extensionFormDefs.Find(t => t.FormType != FormType.MergedForm);
                        if (mismatch != null)
                            throw new Exception(formField.GetDescription() + " is of type MergedForm but Form: " + mismatch.formName + " is of Type: " + mismatch.FormType + " but should be of type MergedForm");
                    }
                }
                else
                {
                    //PluginForm
                    extensionFormDefs = AppPress.formDefs.FindAll(t => t.formName == formField.ExtensionFormName);
                    if (formField.Type == FormDefFieldType.UserControlScalar)
                    {
                        var mismatch = extensionFormDefs.Find(t => t.FormType != FormType.UserControlScalarForm);
                        if (mismatch != null)
                            throw new Exception(formField.GetDescription() + " is of type UserControlScalar but Form: " + mismatch.formName + " is not of type UserControlScalar");
                    }
                    if (formField.Type == FormDefFieldType.MergedForm)
                    {
                        var mismatch = extensionFormDefs.Find(t => t.FormType != FormType.PluginForm && t.FormType != FormType.MergedForm);
                        if (mismatch != null)
                            throw new Exception(formField.GetDescription() + " is of type PluginForm but Form: " + mismatch.formName + " is not of type PluginForm");
                    }
                }
                if (formField.Type == FormDefFieldType.UserControlScalar)
                    if (extensionFormDefs.Count() > 1)
                        throw new Exception(formField.GetDescription() + " should have one and only one FormDefinedControl");
                foreach (var extensionFormDef in extensionFormDefs)
                {
                    var extFormDefId = extensionFormDef.id;
                    var cloneFormDef = extensionFormDef;
                    cloneFormDef.extensionFormDefId = formDef.id;
                    //foreach (var f in AppPress.formDefs)
                    //    if (f.ContainerFormField != null && f.ContainerFormField.formDef.id == extFormDefId)
                    //        f.ContainerFormField.formDef = cloneFormDef;
                    var addedFormFields = new List<FormField>();
                    foreach (var eFormField in cloneFormDef.formFields)
                    {
                        if (formDef.formFields.Find(t => t.Type != FormDefFieldType.MergedForm && t.Type != FormDefFieldType.UserControlScalar && t.fieldName == eFormField.fieldName) != null)
                            throw new Exception("Internal Error: " + formDef.formFields.Find(t => t.fieldName == eFormField.fieldName).GetDescription() + " being added twice");
                        var ef = eFormField.Clone();
                        addedFormFields.Add(ef);
                        if (formField.Static)
                            ef.Static = formField.Static;
                        if (formField.StaticSubmitValue)
                            ef.StaticSubmitValue = formField.StaticSubmitValue;
                        if (formField.DoNotSaveInDB)
                            ef.DoNotSaveInDB = formField.DoNotSaveInDB;
                        ef.UserControlParameters = formField.UserControlParameters;
                        ef.GroupName = formField.GroupName;
                        ef.Sortable = formField.Sortable;
                        ef.Extension = true;
                        if (formField.Type == FormDefFieldType.UserControlScalar)
                        {
                            //ef = form field in the user conreol.
                            ef.OriginalType = (int)FormDefFieldType.UserControlScalar;
                            ef.UserControlScalarFieldName = ef.fieldName;
                            ef.ExtensionFormName = formField.ExtensionFormName;
                            ef.fieldName = formField.fieldName;
                            //ef.StaticSubmitValue = formField.StaticSubmitValue;
                            if (formField.Style != FormDefFieldStyle.None)
                                ef.Style = formField.Style;
                            if (!ef.DoNotSaveInDB)
                                ef.DoNotSaveInDB = formField.DoNotSaveInDB;
                            if (formField.Label != null)
                                ef.Label = formField.Label;
                            ef.Required = formField.Required;
                            ef.id = formField.id;
                        }
                        formDef.formFields.Insert(i, ef);
                        i++;
                    }
                    foreach (var eFormField in addedFormFields)
                        if (eFormField.containerFormField != null)
                            eFormField.containerFormField = addedFormFields.Find(t => t.fieldName == eFormField.containerFormField.fieldName);

                    for (int j = 0; j < skins.Count(); ++j)
                    {
                        var skin = skins[j].skin;
                        if (skin == null)
                            continue;
                        for (int k = 0; k < fieldSkins[j].Count(); ++k)
                        {
                            var fieldSkin = (string)fieldSkins[j][k].skin.Clone();
                            var fieldUnique = indexes[j][k];

                            int insertPoint = fieldSkin.IndexOf("<!--|AppPress.FieldContent|-->");
                            if (extensionFormDef.FormType == FormType.MergedForm && insertPoint != -1 && extensionFormDef.Skins.Find(t => t.skinFileName != null && t.skinType == fieldSkins[j][k].skinType) != null)
                            {
                                // insert form skin
                                fieldSkin = fieldSkin.Substring(0, insertPoint) + extensionFormDef.Skins.Find(t => t.skinType == fieldSkins[j][k].skinType).skin + fieldSkin.Substring(insertPoint);
                                fieldSkin = fieldSkin.Replace("<!--|AppPress.FieldContent|-->", "");
                                fieldSkin = fieldSkin.Replace("<!--|AppPress." + formField.fieldName + ".Begin|-->", "").
                                    Replace("<!--|AppPress." + formField.fieldName + ".End|-->", "");
                                skin = skin.Replace(fieldUnique, fieldSkin);
                            }
                            else
                            {
                                foreach (var eFormField in addedFormFields)
                                {
                                    if (eFormField.containerFormField != null || eFormField.Hidden)
                                        // will be generated along with formContainer
                                        continue;
                                    var fs = fieldSkin.Clone().ToString();
                                    if (insertPoint != -1)
                                    {
                                        // generate and insert field skin
                                        fs = fs.Replace("<!--|AppPress.FieldContent|-->", eFormField.GenerateSkin(a, false, false, false));
                                    }
                                    var mb = "<!--|AppPress." + formField.fieldName + ".Begin|-->";
                                    var me = "<!--|AppPress." + formField.fieldName + ".End|-->";
                                    if (formField.formDef.Pivot)
                                    {
                                        // Clugde to adjust skin for Plugin Fields in Pivot Case
                                        // To check see fs before and after this
                                        int rb = fs.IndexOf("<!--|AppPress." + formField.formDef.ContainerFormField.fieldName + ".RowBegin|-->");
                                        if (rb != -1)
                                        {
                                            fs = fs.Substring(0, rb) + me + "<!--|AppPress." + formField.formDef.ContainerFormField.fieldName + ".HeaderEnd|-->" + fs.Substring(rb);
                                            fs = fs.Substring(0, fs.Length - me.Length);
                                            fs += "<!--|AppPress." + formField.formDef.ContainerFormField.fieldName + ".HeaderBegin|-->";
                                        }
                                    }
                                    fs = fs.Replace(mb, "<!--|AppPress." + eFormField.fieldName + ".Begin|-->").
                                        Replace(me, "<!--|AppPress." + eFormField.fieldName + ".End|-->");
                                    skin = skin.Replace(fieldUnique, fs + fieldUnique);

                                }
                            }
                        }
                        skins[j].skin = skin;

                    }
                }
                for (int j = 0; j < indexes.Count(); ++j)
                    foreach (var u in indexes[j])
                        skins[j].skin = skins[j].skin.Replace(u, "");
                formDef.formFields.Remove(formField);
                i--;
            }
        }
        internal class FieldContainerFields
        {
            public List<CommentClass> Fields;
        }
        internal class CommentClass
        {
            public string Container;
            public FormDefFieldType Type;
        }
        internal static FormDef InitializeFormDef(AppPress a, FormDef formDef, List<FormDef> formDefs)
        {

            if (!System.Text.RegularExpressions.Regex.IsMatch(formDef.formName, "^[a-zA-Z][a-zA-Z0-9_-_]*$"))
                throw new Exception("Form Name: " + formDef.formName + " has invalid Character.");
            //if (formDef.FormType != FormType.ContainerRowFormGenerated)
            //    if (formDefs.Find(t => t != formDef && t.FormType != FormType.ContainerRowFormGenerated && t.formName == formDef.formName) != null)
            //        throw new Exception("Form: " + formDef.formName + " is Duplicate.");
            if (formDef.ContainerFormField != null)
            {
                var fid = formDef.ContainerFormField.id;
                var formDefId = formDef.ContainerFormField.formDefId;
                formDef.ContainerFormField = null;
                foreach (var fd in formDefs)
                    foreach (var ff in fd.formFields)
                        if (ff.id == fid && (formDefId == null || formDefId == fd.id))
                            formDef.ContainerFormField = ff;
                if (formDef.ContainerFormField == null)
                    throw new Exception("Internal Error: ContainerFormField cannot be null");
            }

            if (formDef.MasterFormName != null)
            {
                var masterFormDef = formDefs.Find(t => t.formName == formDef.MasterFormName);
                if (masterFormDef == null)
                    throw new Exception("Could not Find MasterFormName: " + formDef.MasterFormName);
                if (masterFormDef.FormType != FormType.MasterForm)
                    throw new Exception("MasterFormName: " + formDef.MasterFormName + " is not of Type MasterForm");
            }
            if (formDef.FormType == FormType.PluginForm)
            {
                //if (formDef.MergeIntoForms == null || formDef.MergeIntoForms.Count() == 0)
                //    throw new Exception("MergedForm: " + formDef.formName + " should have at least one MergeIntoForm child node");
                foreach (var mergedIntoForm in formDef.MergeIntoForms)
                {
                    if (mergedIntoForm.FormName == null)
                        throw new Exception("MergedForm: " + formDef.formName + " MergeIntoForm cannot have FormName null");
                    var mergeIntoFormDefs = formDefs.FindAll(t => t.formName == mergedIntoForm.FormName);
                    if (mergeIntoFormDefs.Count == 0)
                        throw new Exception("Could not find Form: " + mergedIntoForm.FormName + " used as MergeIntoForm in Form: " + formDef.formName);
                    if (mergeIntoFormDefs.Count > 1)
                        throw new Exception("Found 2 Forms: " + mergedIntoForm.FormName + " used as MergeIntoForm in Form: " + formDef.formName);
                    int beforeFormFieldIndex = -1;
                    var mergeIntoFormDef = mergeIntoFormDefs[0];
                    if (mergedIntoForm.BeforeFieldName != null)
                    {
                        beforeFormFieldIndex = mergeIntoFormDef.formFields.FindIndex(t => t.fieldName == mergedIntoForm.BeforeFieldName || t.Label == mergedIntoForm.BeforeFieldName);
                        if (beforeFormFieldIndex == -1)
                            throw new Exception("Could not find Field: " + mergedIntoForm.BeforeFieldName + " used as MergeIntoForm BeforeFieldName in Form: " + formDef.formName);
                    }
                    var insertFormField = new FormField();
                    insertFormField.formDef = mergeIntoFormDef;
                    insertFormField.fieldName = formDef.formName;
                    insertFormField.Type = FormDefFieldType.MergedForm;
                    insertFormField.ExtensionFormName = formDef.formName;
                    if (mergeIntoFormDef.GenerationType == 1)
                        insertFormField.Static = true; // for RowFields
                    if (beforeFormFieldIndex == -1)
                        mergeIntoFormDef.formFields.Add(insertFormField);
                    else
                        mergeIntoFormDef.formFields.Insert(beforeFormFieldIndex, insertFormField);
                }
            }
            foreach (var formField in formDef.formFields)
            {
                if (formField.SaveTableForeignKey == null && formField.SaveTableName != null)
                    throw new Exception(formField.GetDescription() + " Could not find Property SaveTableForeignKey");
                if (formField.Sortable && formField.OriginalType != (int)FormDefFieldType.None)
                {
                    formField.optionsCache = new List<Option>();
                    formField.optionsCache.Add(new Option { id = null, value = "fa fa-sort appPressColumnHeaderButtons" });
                    formField.optionsCache.Add(new Option { id = "1", value = "fa fa-sort-asc appPressColumnHeaderButtons" });
                    formField.optionsCache.Add(new Option { id = "2", value = "fa fa-sort-desc appPressColumnHeaderButtons" });
                }
                formField.formDef = formDef;
                if (formField.containerFormField != null)
                    formField.containerFormField = formField.formDef.formFields.Find(t => t.fieldName == formField.containerFormField.fieldName);
                if (a.site != null) // from TT Build
                {

                    if (formField.FieldFunctions != null)
                        foreach (var function in formField.FieldFunctions)
                        {
                            function.SetMethod(a);
                        }
                }
                if (formField.Static && !formField.DoNotSaveInDB)
                    formField.StaticSubmitValue = true;
            }
            foreach (var formField in formDef.formFields.FindAll(t => t.containerFormField != null))
            {
                string containerFormField = formField.containerFormField.fieldName;
                formField.containerFormField = formDef.formFields.Find(t => containerFormField == t.fieldName);
                if (formField.containerFormField == null)
                    throw new Exception("In FormDef:" + formDef.formName + " Field: " + formField.fieldName + " has a non existant ContainerFieldName:" + containerFormField);
                if (formField.containerFormField.Type != FormDefFieldType.FormContainerDynamic)
                    throw new Exception("In FormDef:" + formDef.formName + " Field: " + formField.fieldName + " ContainerFieldName:" + containerFormField + " is not of type FormContainerDynamic");
            }
            if (formDef.TableName != null && a.site != null /* from TT */)
            {
                // Get Primary Key
                string q;
                if (a.site.databaseType == DatabaseType.SqlServer)
                    q = @"SELECT COLUMN_NAME
                        FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE
                        WHERE OBJECTPROPERTY(OBJECT_ID(CONSTRAINT_SCHEMA + '.' + CONSTRAINT_NAME), 'IsPrimaryKey') = 1
                        AND TABLE_NAME = '" + formDef.TableName + @"' AND TABLE_CATALOG = '" + a.dbName + @"'";
                else
                    q = @"SELECT `COLUMN_NAME`
                        FROM `information_schema`.`COLUMNS`
                        WHERE (`TABLE_SCHEMA` = '" + a.dbName + @"')
                          AND (`TABLE_NAME` = '" + formDef.TableName + @"')
                          AND (`COLUMN_KEY` = 'PRI');";
                formDef.PrimaryKey = a.site.GetPrimaryKey(formDef.TableName);
            }
            if (formDef.PrimaryKey == null)// can be a View
                formDef.PrimaryKey = "id";
            // Add Foreign Key

            return formDef;
        }

        private static void LoadFunctionParameters(AppPress a, FormDef formDef)
        {
            foreach (var formField in formDef.formFields)
                foreach (var serverFunction in formField.FieldFunctions)
                    if (serverFunction.parameterFormId != null)
                    {
                        serverFunction.Parameters.Clear();
                        // TDB: Add formData as serverFunction parameter
                        var functionFormDef = AppPress.FindFormDef(serverFunction.FunctionName);
                        string containerColumnName = null;
                        if (functionFormDef.formFields.Find(t => t.Type == FormDefFieldType.ForeignKey) == null)
                            continue;
                        else
                            containerColumnName = functionFormDef.formFields.Find(t => t.Type == FormDefFieldType.ForeignKey).fieldName;
                        var tableName = functionFormDef.TableName;
                        if (!tableName.IsNullOrEmpty())
                        {
                            var parameterId = a.ExecuteString("Select cast(id as char) From " + tableName + " Where " + containerColumnName + "=" + serverFunction.parameterFormId);
                            if (parameterId != null)
                            {
                                var formData = a.LoadFormData(functionFormDef.id, parameterId, null, null);
                                foreach (var fieldValue in formData.fieldValues)
                                {
                                    if (!fieldValue.Value.IsNullOrEmpty())
                                        if (fieldValue.formField.Type == FormDefFieldType.Checkbox)
                                        {
                                            if (fieldValue.Value == "1")
                                                serverFunction.Parameters.Add(new ServerFunctionParameter(fieldValue.formField.fieldName, ""));
                                        }
                                        else
                                        {
                                            var fName = fieldValue.formField.fieldName;
                                            var v = fieldValue.Value;
                                            if (fName == "FormContainerId")
                                            {
                                                fName = "ContainerRowForm";
                                                var fromId = Convert.ToInt32(a.ExecuteScalar("Select ContainerRowFormId From apppress_getcontainerrowforms Where FormContainerId=" + v));
                                                v = AppPress.FindFormDef(fromId).formName;
                                            }
                                            serverFunction.Parameters.Add(new ServerFunctionParameter(fName, v));
                                        }
                                }
                            }
                        }
                    }

        }
        /// <summary>
        /// Get Field Index in FormDef
        /// </summary>
        /// <param name="formDefIndex">formDefIndex is the index of the formDef</param>
        /// <param name="fieldName">fieldName to find</param>
        /// <returns></returns>
        public static int GetFieldIndex(DAOBasic site, int formDefIndex, string fieldName)
        {
            FormDef formDef = AppPress.formDefs[formDefIndex];
            for (int i = 0; i < formDef.formFields.Count; i++)
            {
                if (formDef.formFields[i].fieldName.Equals(fieldName, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }
            return -1;
        }


        /// <summary>
        /// This is used to fetch all resource from database and save in application cache.
        /// </summary>

        internal static string ReplaceSingleInstance(FormDef formDef, string fileContent, string source, string replacement)
        {
            int firstIndex = fileContent.IndexOf(source, StringComparison.OrdinalIgnoreCase);
            int lastIndex = fileContent.IndexOf(source, StringComparison.OrdinalIgnoreCase);
            if (firstIndex == -1)
                return fileContent;
            if (firstIndex != lastIndex)
                throw new Exception("Skin: " + formDef.formName + ".html contains more than one specified TemplateTag:" + source);
            fileContent = fileContent.Replace(source, replacement);
            return fileContent;
        }

        #region //For remember me

        public static void SaveUserInfoInCookie(string cookieName, string email, string password)
        {
            var cookie = HttpContext.Current.Request.Cookies[cookieName] ?? new HttpCookie(cookieName);
            cookie["Email"] = EncryptDecrypt.Encrypt(email);
            cookie["Password"] = EncryptDecrypt.Encrypt(password);
            cookie.Expires = DateTime.UtcNow.AddDays(7);
            HttpContext.Current.Response.Cookies.Add(cookie);
        }

        public static void RemoveUserInfofromCookie(string cookieName)
        {
            var cookie = HttpContext.Current.Request.Cookies[cookieName];
            if (cookie != null)
            {
                cookie.Expires = DateTime.UtcNow.AddYears(-7);
                HttpContext.Current.Response.Cookies.Add(cookie);
            }

        }
        #endregion
        public static void ExecuteCommandSync(object command)
        {
            var procStartInfo = new System.Diagnostics.ProcessStartInfo("cmd", "/c " + command);
            procStartInfo.RedirectStandardOutput = true;
            procStartInfo.UseShellExecute = false;
            procStartInfo.CreateNoWindow = true;
            var proc = new System.Diagnostics.Process();
            proc.StartInfo = procStartInfo;
            proc.Start();

            var result = proc.StandardOutput.ReadToEnd();

        }
        public static string CompileSkin(AppPress a, string skin, bool onlyField, SkinType skinType, bool header)
        {

            var formData = a.fieldValue.FormData;
            var formContainerFieldValues = new List<FormContainerFieldValue>();
            for (int i = 0; i < formData.fieldValues.Count(); ++i)
            {
                var fieldValue = formData.fieldValues[i];
                if (onlyField)
                    if (fieldValue.formField.id != a.fieldValue.formField.id || fieldValue.FormData.id != a.fieldValue.FormData.id)
                        continue;
                if (!onlyField && fieldValue.formField.containerFormField != null)
                    continue;
                var formField = fieldValue.formField;

                if (formField.Type == FormDefFieldType.FormContainerDynamic || formField.Type == FormDefFieldType.FormContainerGrid)
                {
                    if (fieldValue.Hidden == FieldHiddenType.Hidden)
                    {
                        bool outer;
                        var r = "";
                        if (a.skinType != SkinType.FO)
                        {
                            string fieldUnique, fieldSkin, beginMarker, endMarker;
                            bool outer1;
                            skin = fieldValue.formField.ExtractBetweenMarkers(a, skin, 0, "", out fieldUnique, out fieldSkin, out outer1, out beginMarker, out endMarker);
                            if (fieldSkin != null)
                            {
                                int hiddenMarkerBeginIndex = fieldSkin.IndexOf("<!--|AppPress.HiddenBegin|-->");
                                if (hiddenMarkerBeginIndex != -1)
                                {
                                    int hiddenMarkerEndIndex = fieldSkin.LastIndexOf("<!--|AppPress.HiddenEnd|-->");
                                    if (hiddenMarkerEndIndex == -1)
                                        throw new Exception(fieldValue.GetFieldDescription() + " Cound not find Marker HiddenEnd");
                                    fieldSkin = fieldSkin.Substring(0, hiddenMarkerBeginIndex) + fieldSkin.Substring(hiddenMarkerEndIndex + "<!--|AppPress.HiddenEnd|-->".Length);
                                }
                                else
                                    r = "<div id='" + fieldValue.GetHtmlContainerId() + "'></div>";
                                skin = skin.Replace(fieldUnique, beginMarker + fieldSkin + endMarker);
                            }
                        }
                        skin = fieldValue.formField.RemoveBetweenMarkers(a, skin, "", out outer, r);
                    }
                    else
                        formContainerFieldValues.Add((FormContainerFieldValue)fieldValue);
                }
                else
                {
                    skin = fieldValue.CompileSkin(a, skin, header);
                    if (header && fieldValue.formField.fieldName == "SelectRow" && fieldValue.FormData.containerFieldValue != null)
                        a.JsStr.Append("$(JQueryEscape('#SelectRow_" + fieldValue.GetHtmlId() + "')).prop('disabled'," + (((FormContainerGridFieldValue)fieldValue.FormData.containerFieldValue).allowMultiSelect ? "false" : "true") + ");");
                    if (AppPress.Settings.developer && fieldValue.formField.Type == FormDefFieldType.Button && fieldValue.Hidden == FieldHiddenType.None)
                        if (fieldValue.formField.FieldFunctions.Count() == 0 && !fieldValue.InvokeOn(a, "OnClick", false))
                        {
                            var message = fieldValue.formField.formDef._GenerateCode(0, fieldValue.formField, "OnClick");
                            fieldValue.Error = "<a onclick=\"AlertMessage('Could not find OnClick function " + HttpUtility.HtmlAttributeEncode(message.Replace("'", "\\'").Replace("\"", "\\\"").Replace(Environment.NewLine, "<br/>")) + "',900)\"><span style='color:red'>X</span></a>";
                        }
                }

            }
            // find grid container rows
            foreach (var fieldValue in formContainerFieldValues)
            {
                var formField = fieldValue.formField;
                string fieldSkin, beginMarker, endMarker, fieldUnique;
                bool outer;
                if (fieldValue.GetType().BaseType == typeof(FormContainerGridFieldValue))
                {
                    ((FormContainerGridFieldValue)fieldValue).AllowMultiSelect(((FormContainerGridFieldValue)fieldValue).allowMultiSelect);
                }
                foreach (var fieldValue1 in formData.fieldValues)
                    if (fieldValue1.formField.containerFormField != null && fieldValue1.formField.containerFormField.id == fieldValue.formField.id)
                    {
                        string rowSkin, rowBeginMarker, rowEndMarker, rowFieldUnique;
                        // remove rowSkin marker
                        skin = fieldValue.formField.ExtractBetweenMarkers(a, skin, 0, "Row", out rowFieldUnique, out rowSkin, out outer, out rowBeginMarker, out rowEndMarker);
#if DEBUG
                        var found1 = false;
#endif
                        while (true)
                        {
                            skin = fieldValue1.formField.ExtractBetweenMarkers(a, skin, 0, "", out fieldUnique, out fieldSkin, out outer, out beginMarker, out endMarker);
                            if (fieldSkin == null)
                                break;
#if DEBUG
                            found1 = true;
#endif
                            fieldSkin = fieldValue1.CompileSkin(a, beginMarker + fieldSkin + endMarker, header);
                            skin = skin.Replace(fieldUnique, fieldSkin);
                        }
                        if (rowSkin != null)
                            skin = skin.Replace(rowFieldUnique, rowBeginMarker + rowSkin + rowEndMarker);
#if DEBUG
                        if (!found1 && !header && a.fieldsNotGenerated.Find(t => t == fieldValue1) == null && fieldValue1.Hidden != FieldHiddenType.Hidden)
                            a.fieldsNotGenerated.Add(fieldValue1);
#endif
                    }

                var oskin = skin;
                skin = formField.ExtractBetweenMarkers(a, skin, 0, "", out fieldUnique, out fieldSkin, out outer, out beginMarker, out endMarker);
                if (fieldSkin == null)
                {
#if DEBUG
                    if (a.fieldsNotGenerated != null && a.fieldsNotGenerated.Find(t => t == fieldValue) == null)
                        a.fieldsNotGenerated.Add(fieldValue);
#endif
                    if (fieldValue.Hidden != FieldHiddenType.None || fieldValue.formField.Hidden || fieldValue.formField.Type == FormDefFieldType.ForeignKey)
                        continue;
                    throw new Exception("Could not find Markers in Skin for " + formField.GetDescription());
                    //continue;
                }
                var htmlId = "AppPress" + AppPress.IdSep + (int)formField.Type + AppPress.IdSep + fieldValue._GetHtmlId();

                var containerFormData = formData;
                //FormData pFormData = formData;
                var pFieldValue = a.fieldValue;
                try
                {
                    // popup form containerFieldValue is same as containerFieldValue of rows in formContainer, so remove forms with callerFieldValue
                    var rowFormDef = fieldValue.formField.GetContainerRowFormDef(a);
                    var rowFormDatas = a.formDatas.FindAll(t => !t.IsDeleted && t.containerFieldValue == fieldValue);

                    while (true)
                    {
                        string headerUnique;
                        string headerSkin;
                        fieldSkin = fieldValue.formField.ExtractBetweenMarkers(a, fieldSkin, 0, "Header", out headerUnique, out headerSkin, out outer, out beginMarker, out endMarker);
                        if (headerSkin == null)
                            break;
                        FormData formData1 = null;
                        List<FieldValue> fieldValues = null;
                        try
                        {
                            a.fieldValue = new FormContainerFieldValue();
                            a.fieldValue.formField = new FormField();
                            a.fieldValue.formField.formDef = rowFormDef;
                            if (rowFormDatas.Count == 0)
                            {
                                formData1 = new FormData(a, rowFormDef, fieldValue);
                                a.fieldValue.FormData = formData1;
                            }
                            else
                            {
                                // there may be hidden fields in first row, eg Total in HRmates Approve Salary
                                a.fieldValue.FormData = rowFormDatas[0];
                                fieldValues = new List<FieldValue>();
                                foreach (var fv in a.fieldValue.FormData.fieldValues)
                                {
                                    var fvn = new FieldValue();
                                    fvn.Hidden = fv.Hidden;
                                    fieldValues.Add(fvn);
                                    fv.Hidden = FieldHiddenType.None;
                                }
                            }
                            //formData1.fieldValues.RemoveAll(t => t.formField.Type == FormDefFieldType.FormContainerDynamic || t.formField.Type == FormDefFieldType.FormContainerGrid);
                            var HiddenColumns = ((FormContainerGridFieldValue)fieldValue).HiddenColumns;
                            if (HiddenColumns != null)
                            {
                                // if all cells of a column hidden. hide header also
                                foreach (var fh in a.fieldValue.FormData.fieldValues)
                                {
                                    if (HiddenColumns.Find(t => t == fh.formField.fieldName) != null)
                                    {
                                        if (rowFormDef.Pivot)
                                            a.JsStr.Append("$(JQueryEscape('#" + fh.GetHtmlColumnId() + "')).hide();");
                                        else
                                        {
                                            bool outer1;
                                            headerSkin = fh.formField.RemoveBetweenMarkers(a, headerSkin, "", out outer1);
                                        }
                                    }
                                }
                            }
                            headerSkin = Util.CompileSkin(a, headerSkin, false, skinType, true);
                            fieldSkin = fieldSkin.Replace(headerUnique, headerSkin);
                        }
                        finally
                        {
                            a.formDatas.RemoveAll(t => t == formData1);
                            if (fieldValues != null)
                                for (int i = 0; i < rowFormDatas[0].fieldValues.Count; ++i)
                                    rowFormDatas[0].fieldValues[i].Hidden = fieldValues[i].Hidden;
                        }
                    }
                    var rowSkins = new List<string>();
                    var rowSkinUnique = new List<string>();
                    while (true)
                    {
                        string rowUnique, rowSkin;
                        fieldSkin = fieldValue.formField.ExtractBetweenMarkers(a, fieldSkin, 0, "Row", out rowUnique, out rowSkin, out outer, out beginMarker, out endMarker);
                        if (rowSkin == null)
                            break;
                        rowSkins.Add(rowSkin);
                        rowSkinUnique.Add(rowUnique);
                    }
                    if (rowSkins.Count() == 0)
                    {
                        if (!header)
                            throw new Exception(formField.GetDescription() + " Could not find Marker: " + HttpUtility.HtmlDecode(beginMarker));
                    }
                    else if (rowFormDef == null)
                    {
                        rowSkins.Clear();
                        rowSkins.Add(null);
                    }

                    var rowNumber = 0;
                    bool foundC = true;
                    a.fieldValue = new FormContainerFieldValue();
                    a.fieldValue.formField = new FormField();
                    foreach (var rowFormData in rowFormDatas)
                        if (!rowFormData.IsDeleted)
                        {
                            a.fieldValue.formField.formDef = rowFormData.formDef;
                            a.fieldValue.FormData = rowFormData;
                            ((FormContainerFieldValue)a.fieldValue).rowNumber = rowNumber;
                            bool foundCall = true;
                            for (int i = 0; i < rowSkins.Count(); ++i)
                            {
                                var rSkin = rowSkins[i];
                                if (rSkin != null)
                                    rSkin = rSkin.Replace("AppPressRowNumber", rowNumber.ToString());
                                if (fieldValue.formField.OriginalType == (int)FormDefFieldType.FormContainerGrid)
                                {
                                    var HiddenColumns = ((FormContainerGridFieldValue)fieldValue).HiddenColumns;
                                    if (HiddenColumns != null && !rowFormDef.Pivot)
                                    {
                                        // if all cells of a column hidden. hide header also
                                        foreach (var fh in rowFormData.fieldValues)
                                        {
                                            if (HiddenColumns.Find(t => t == fh.formField.fieldName) != null)
                                            {
                                                bool outer1;
                                                rSkin = fh.formField.RemoveBetweenMarkers(a, rSkin, "", out outer1);
                                            }
                                        }
                                    }
                                }

                                string rHTML = rowFormData.GetHtml(a, formField.Static, true, rSkin, skinType);
                                // preserve ScriptBeginEnd as they have to extracted in same order
                                //rHTML = rHTML.Replace(Markers.ScriptBeginMarker, "SBjdnfjkndfjknfkjnfkjndfkjndfjkfn").Replace(Markers.ScriptEndMarker, "SEjdnfjkndfjknfkjnfkjndfkjndfjkfn");
                                //rHTML = rHTML.Replace("SBjdnfjkndfjknfkjnfkjndfkjndfjkfn", Markers.ScriptBeginMarker).Replace("SEjdnfjkndfjknfkjnfkjndfkjndfjkfn", Markers.ScriptEndMarker);
                                if (foundC)
                                {
                                    // for performance check for first row
                                    bool f;
                                    rHTML = ExecuteEmbededCalls(a, rHTML, out f);
                                    foundCall = foundCall || f;
                                }
                                fieldSkin = fieldSkin.Replace(rowSkinUnique[i], rHTML + rowSkinUnique[i]);
                            }
                            foundC = foundCall;
                            rowNumber++;
                        }

                    if (skinType == SkinType.FO && rowNumber == 0)
                    {
                        // FOP complaints when fo:table-body is empty
                        // fieldSkin = fieldSkin.Replace(rowSkinUnique[0], "<fo:table-row><fo:table-cell><fo:block/></fo:table-cell></fo:table-row>");
                    }
                    for (int i = 0; i < rowSkins.Count(); ++i)
                        fieldSkin = fieldSkin.Replace(rowSkinUnique[i], "");
                }
                finally
                {
                    //a.fieldValue.FormData = pFormData;
                    a.fieldValue = pFieldValue;
                }
                fieldSkin = fieldValue.ReplaceAppPress(a, fieldSkin);
                skin = skin.Replace(fieldUnique, fieldSkin);

            }
            bool found;
            skin = ExecuteEmbededCalls(a, skin, out found);
            skin = skin.Replace("AppPressFormErrorId", a.GetHtmlFormErrorId());
            return skin;
        }

        public static string ExecuteEmbededCalls(AppPress a, string str, out bool found)
        {
            found = false;
            int startIndex = 0;
            string search = "{TempFunction";
            var method = AppPress.SkinAssembly.GetType("SkinCode.SplitCodeExecute").GetMethod("CodeFragmentExecute");
            while (startIndex >= 0)
            {
                int index = str.IndexOf(search, startIndex);
                if (index == -1)
                    break;
                found = false;
                index += 1;
                int endIndex = str.IndexOf("}", index);
                if (endIndex == -1)
                    throw new Exception("Could not find ending '}' of '{' at offset:" + index);
                string variable = str.Substring(index + (search.Length - 1), endIndex - index - (search.Length - 1)).Trim();
                int key = int.Parse(variable);
                var s = "";
                try
                {
                    s = (string)InvokeMethod(a, method, new object[] { a, key });
                }
                catch (Exception ex)
                {
                    throw new Exception("Error in Executing Skin Code:<br/>" + AppPress.SkinCode[key] + " Error Message: " + ex.Message);
                }
                startIndex = endIndex;
                startIndex -= str.Length;
                str = str.Substring(0, index - 1) + s + str.Substring(endIndex + 1);
                startIndex += str.Length;

            }
            return str;
        }

        class DotNetOutputMemoryStream : OutputStream
        {

            private System.IO.MemoryStream ms = new System.IO.MemoryStream();

            public System.IO.MemoryStream Stream
            {

                get
                {

                    return ms;

                }

            }

            public override void write(int i)
            {

                ms.WriteByte((byte)i);

            }

            public override void write(byte[] b, int off, int len)
            {

                ms.Write(b, off, len);

            }

            public override void write(byte[] b)
            {

                ms.Write(b, 0, b.Length);

            }

            public override void close()
            {

                ms.Close();

            }

            public override void flush()
            {

                ms.Flush();

            }

        }
        //generate pdf code
        internal static byte[] GeneratePDF(string formName, string id, HttpRequest request, Object appLogic, int foIndex, object filerObj)
        {
            var site = new DAOBasic();
            var formDef = FormDef.FindFormDef(AppPress.formDefs, formName);
            if (formDef == null)
                throw new AppPressException("Could not Find Form: " + formName);
            var a = (AppPress)Activator.CreateInstance(AppPress.Settings.ApplicationAppPress, new object[] { site, !formDef.NonSecure });
            a.ApplicationData = filerObj;
            a.CreateAppPress(request, null, formDef, id, SkinType.FO);
            a.CallReason = CallReasonType.PageLoad;
            try
            {
                //if (a.sessionData.formDefIdAndFormIds.Find(t => t.FormDefId == formDef.id && t.FormId == id) == null)
                //    throw new Exception("You are not allowed access to this.");
                var str = formDef.GetSkin(a, false, false, null, SkinType.FO, foIndex);

                try
                {
                    str = Util.CompileSkin(a, str, false, SkinType.FO, false);
                    return FOtoPDF(str);
                }
                catch (Exception ex)
                {
                    var message = ex.Message;
#if DEBUG
                    message += "\n\n" + str;
#endif
                    throw new Exception(message);
                }
            }
            finally
            {
                site.Close();
            }

        }
        static string fopLog;
        public class TraceStream : OutputStream
        {

            public override void write(int i)
            {

            }
            public override void write(byte[] b)
            {

            }
            public override void write(byte[] b, int off, int len)
            {
                fopLog += System.Text.Encoding.UTF8.GetString(b, off, len);
            }
        }
        internal static byte[] FOtoPDF(string str)
        {
            fopLog = "";
            java.lang.System.setProperty("org.apache.commons.logging.Log", "org.apache.commons.logging.impl.SimpleLog");
            java.lang.System.setErr(new java.io.PrintStream(new TraceStream()));
            java.lang.System.setOut(new java.io.PrintStream(new TraceStream()));
            var path = HttpContext.Current.Server.MapPath("~/bin/fopconfig.xml");
            FopFactory fopFactory = FopFactory.newInstance(new java.io.File(path));
            OutputStream o = new DotNetOutputMemoryStream();
            try
            {

                Fop fop = fopFactory.newFop(MimeConstants.MIME_PDF, o);

                TransformerFactory factory = TransformerFactory.newInstance();
                Transformer transformer = factory.newTransformer();


                //read the template from disc

                Source src = new StreamSource(IOUtils.toInputStream(str, "UTF-8"));

                Result res = new SAXResult(fop.getDefaultHandler());

                transformer.transform(src, res);

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message + "\nLog: \n" + fopLog);
            }

            finally
            {

                o.close();

            }
            return ((DotNetOutputMemoryStream)o).Stream.GetBuffer();
        }

        internal static void RefreshFields(AppPress a, List<FieldValue> fieldValues)
        {
            var fv = fieldValues.OrderBy(t => -t.fieldDefId);
            foreach (var fieldValue1 in fv)
            {
                //if (fieldValue1.formField.Type == FormDefFieldType.FileUpload || fieldValue1.formField.OriginalType == FormDefFieldType.MultiFileUpload)
                //    continue; // Refresh of fileupload will be inside refresh of container field so no need to do this
                //if (fieldValue1.formField.Type == FormDefFieldType.Pickone || fieldValue1.formField.Type == FormDefFieldType.PickMultiple)
                //    fieldValue1.Value = null; //This will be called from auto refresh.
                a.appPressResponse.Add(AppPressResponse.RefreshField(a, fieldValue1, true));
            }

        }

        internal static void AjaxCallHandler(HttpRequest Request, HttpResponse Response, Object appLogic)
        {
            var pageData = Request.Form["p"];
            pageData = pageData.ToUnEscapeString();
            var a = (AppPress)FormDef.Deserialize(pageData, AppPress.Settings.ApplicationAppPress);
            if (Request.UrlReferrer != null)// from RemotePopup
                a.PageURL = HttpUtility.ParseQueryString(Request.UrlReferrer.Query);
            a.JsStr = new StringBuilder();
            a.functionCall = Request.QueryString["functionCall"];
            a.appPressResponse = new List<AppPressResponse>();
            var sData = AppPress.TryGetSessionData();
            if (sData != null)
            {
                sData.screenWidth = a.screenWidth;
                sData.screenHeight = a.screenHeight;
                sData.documentWidth = a.documentWidth;
                sData.documentHeight = a.documentHeight;
                sData.windowWidth = a.windowWidth;
                sData.windowHeight = a.windowHeight;
            }
            FormData RefreshFieldFormData = null;
            long RefreshFieldFieldDefId = -1;
            var savedFunctionCall = a.functionCall;
            //if (a.remoteFormDefs != null)
            //    foreach (var formDef in a.remoteFormDefs)
            //    {
            //        if (AppPress.FindFormDef(formDef.id) != null)
            //            AppPress.formDefs.RemoveAll(t => t.id == formDef.id);
            //        InitializeFormDef(a, formDef, a.remoteFormDefs);
            //        AppPress.formDefs.Add(formDef);
            //    }
            //a.remoteFormDefs = null;
            if (a.formDatas == null)
            {
                a.formDatas = new List<FormData>();
                a.DoNotSerialize = true;
            }
            else
            {
                // check if request for another instance
                if (a.instanceId != AppPress.LocalInstanceId)
                {
                    var formDatas = new List<FormData>(a.formDatas);
                    // dispatch request to that instance
                    a.remoteLoginUserId = a.sessionData.loginUserId;
                    a.remoteInstanceId = AppPress.LocalInstanceId;
                    a.remoteData = new RemoteData();
                    a.remoteData.JQueryDateFormat = AppPress.Settings.JQueryDateFormat;
                    a.remoteData.NetDateFormat = AppPress.Settings.NetDateFormat;
                    a.remoteData.NetDateTimeFormat = AppPress.Settings.NetDateTimeFormat;
                    // send formDatas of that instance only
                    a.formDatas.RemoveAll(t => Math.Abs(t.formDefId) % 10 != a.instanceId);
                    // remove references for formDatas removed
                    //foreach (var formData in a.formDatas)
                    //    if (formData.cfv != null)
                    //    {
                    //        long containerFomDefId;
                    //        string containerid;
                    //        long containerFieldDefId;
                    //        long callerFomDefId;
                    //        string callerId;
                    //        long callerFieldDefId;
                    //        FormData.ParseCFV(formData.cfv, out containerFomDefId, out containerid, out containerFieldDefId, out callerFomDefId, out callerId, out callerFieldDefId);
                    //        if (containerid == null || Math.Abs(containerFomDefId) % 10 != a.instanceId)
                    //        {
                    //            var atIndex = formData.cfv.IndexOf("@");
                    //            if (atIndex == -1)
                    //                formData.cfv = null;
                    //            else
                    //                formData.cfv = formData.cfv.Substring(atIndex);
                    //        }
                    //        if (containerid == null || Math.Abs(containerFomDefId) % 10 != a.instanceId)
                    //        {
                    //            var atIndex = formData.cfv.IndexOf("@");
                    //            if (atIndex == -1)
                    //                formData.cfv = null;
                    //            else
                    //                formData.cfv = formData.cfv.Substring(atIndex);
                    //        }
                    //    }
                    var remoteUrl = AppPress.Settings.Instances.Find(t => t.InstanceId == a.instanceId).InstanceBaseUrl + "?functionCall=" + a.functionCall;

                    a.remoteFormDefs = new List<FormDef>();
                    foreach (var formData in a.formDatas)
                        a.remoteFormDefs.Add(AppPress.FindFormDef(formData.formDefId));
                    var pairs = new NameValueCollection();
                    pairs.Add("p", FormDef.Serialize(a, AppPress.Settings.ApplicationAppPress));
                    a.remoteFormDefs = null;
                    WebClient client = new WebClient();
                    byte[] response = client.UploadValues(new Uri(remoteUrl), pairs);

                    var responseA = (AppPress)FormDef.Deserialize(System.Text.Encoding.UTF8.GetString(response), AppPress.Settings.ApplicationAppPress);

                    a.pageStackCount = responseA.pageStackCount;
                    //var s1 = responseA.Serialize();
                    a.functionCall = null;
                    a.formDatas = formDatas;
                    a.remoteLoginUserId = null;
                    if (responseA.formDatas != null) // in case of error
                    {
                        a.formDatas.RemoveAll(t => responseA.formDatas.Find(t1 => t1.formDefId == t.formDefId && t1.id == t.id) != null);
                        a.formDatas.AddRange(responseA.formDatas);
                    }
                    foreach (var remoteFormDef in responseA.remoteFormDefs)
                    {
                        AppPress.formDefs.RemoveAll(t => t.id == remoteFormDef.id);
                        remoteFormDef.MasterFormName = null;
                        Util.InitializeFormDef(responseA, remoteFormDef, responseA.remoteFormDefs);
                        AppPress.formDefs.Add(remoteFormDef);
                    }
                    AppPress.formDefs.AddRange(responseA.remoteFormDefs);
                    for (int i = 0; i < responseA.appPressResponse.Count(); ++i)
                    {
                        var remoteRefresh = responseA.appPressResponse[i];
                        if (remoteRefresh.appPressResponseType == AppPressResponseType.RemoteRefresh)
                        {
                            a.functionCall = "RefreshField";
                            RefreshFieldFormData = a.formDatas.Find(t => t.formDefId == remoteRefresh.formDefId && t.id == remoteRefresh.id);
                            if (RefreshFieldFormData == null)
                                throw new Exception("In Remote Refresh Cannot find FormData");
                            RefreshFieldFieldDefId = remoteRefresh.fieldDefId;
                            //a.fieldValue = formData.fieldValues.Find(t => t.fieldDefId == remoteRefresh.fieldDefId);
                            //if (a.fieldValue == null)
                            //    throw new Exception("In Remote Refresh Cannot find FormData");
                            //var refresh = AppPressResponse.RefreshField(a, fieldValue, true);
                            //refresh.appPressResponseType = AppPressResponseType.RefreshField;
                            //responseA.appPressResponse[i] = refresh;
                        }
                        else if (remoteRefresh.appPressResponseType == AppPressResponseType.RemoteRefreshMasterContentArea)
                        {
                            remoteRefresh.appPressResponseType = AppPressResponseType.RefreshField;
                            remoteRefresh.formDefId = a.formDatas[0].formDefId;
                            remoteRefresh.id = a.formDatas[0].id;
                            remoteRefresh.fieldDefId = AppPress.FindFormDef(a.formDatas[0].formDefId).GetFormField("MasterContentArea").id;
                            a.appPressResponse.Add(remoteRefresh);
                        }
                        else if (remoteRefresh.appPressResponseType == AppPressResponseType.RemoteRedirect)
                        {
                            a.formDatas.RemoveAll(t => Math.Abs(t.formDefId) % 10 == a.instanceId);
                            foreach (var formData in a.formDatas)
                            {
                                if (formData.formDef == null)
                                    formData.formDef = AppPress.FindFormDef(formData.formDefId);
                                foreach (var fieldValue in formData.fieldValues)
                                    if (fieldValue.formField == null)
                                        fieldValue.formField = formData.formDef.GetFormField(fieldValue.fieldDefId);
                            }
                            a.RemoteRedirect(a.instanceId, remoteRefresh.message, remoteRefresh.id);
                        }
                        else
                        {
                            //if (remoteRefresh.appPressResponseType == AppPressResponseType.ClosePopupWindow)
                            //    a.pageStackCount--;
                            a.appPressResponse.Add(remoteRefresh);
                        }
                    }
                }
            }
            a.LinksGenerated = new List<string>();
            a.Request = Request;
            a.Response = Response;
            a.site = new DAOBasic();
            a.CallReason = CallReasonType.Ajax;
            Log.Writeln("Ajax Request: " + Request.Url.AbsoluteUri);
            try
            {
                string s1 = null;
                try
                {
                    a.SetFormDataFieldValue();
                    var initMethod = AppPress.Settings.ApplicationAppPress.GetMethod("Init");
                    if (initMethod == null)
                        throw new Exception("Must have a public void Init() {} method in ApplicationAppPress Class: " + AppPress.Settings.ApplicationAppPress.Name);
                    initMethod.Invoke(a, new object[] { a.fieldValue != null && a.fieldValue.FormData != null && a.fieldValue.FormData.formDef != null && !a.fieldValue.FormData.formDef.NonSecure });
                    Log.Writeln(a.fieldValue != null && a.fieldValue.formField != null && a.fieldValue.formField.formDef != null ? (" Field: " + a.fieldValue.formField.formDef.formName + ":" + a.fieldValue.formField.fieldName) : "");
                    //if (AppPress.IsSecureForm(a.rootFormData.formDef.formName))
                    //{
                    //    // check for Session. This will throw session expired exception
                    //    var s = a.sessionData;
                    //}
                    // convert datetime to standard format
                    //if ((a.fieldValue.Hidden != FieldHiddenType.None && !((ButtonFieldValue)a.fieldValue).HiddenForDialogButton) || a.fieldValue.ReadOnly != FieldReadonlyType.None)
                    //    throw new Exception("Security Error");
                    for (int j = 0; j < a.formDatas.Count(); ++j)
                    {
                        var formData = a.formDatas[j];
                        for (int i = 0; i < formData.fieldValues.Count; i++)
                        {
                            var fieldValue = formData.fieldValues[i];
                            if (fieldValue.formField.Type == FormDefFieldType.DateTime)
                                try
                                {
                                    var dateTimeFieldValue = ((DateTimeFieldValue)fieldValue);
                                    DateTime d;
                                    if (!fieldValue.Value.IsNullOrEmpty())
                                        if (!DateTime.TryParseExact(fieldValue.Value, DAOBasic.DBDateTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out d))
                                        {
                                            if (fieldValue.formField.Style == FormDefFieldStyle.Time)
                                            {
                                                if (dateTimeFieldValue.BaseDate == null)
                                                    throw new Exception("Form: " + fieldValue.formField.formDef.formName + " Field: " + fieldValue.formField.formDef.formName + ". Cannot have BaseDateTime null for DateTime field of Style: Time");
                                                var s = fieldValue.Value;
                                                int spaceIndex = s.IndexOf(" ");
                                                var days = 0;
                                                if (spaceIndex != -1)
                                                {
                                                    days = int.Parse(s.Substring(0, spaceIndex).Trim());
                                                    s = s.Substring(spaceIndex + 1);
                                                }
                                                int hours, minutes;
                                                int colonIndex = s.IndexOf(":");
                                                string hstr;
                                                if (colonIndex == -1)
                                                {
                                                    hstr = s;
                                                    minutes = 0;
                                                }
                                                else
                                                {
                                                    hstr = s.Substring(0, colonIndex);
                                                    var mstr = s.Substring(colonIndex + 1);
                                                    if (mstr.Length != 2)
                                                        fieldValue.ErrorException("Minutes should be 2 digits: " + mstr);
                                                    minutes = int.Parse(mstr.Trim());
                                                }
                                                if (!int.TryParse(hstr.Trim(), out hours))
                                                    fieldValue.ErrorException("Invalid Hours: " + hstr);
                                                if (hours > 23)
                                                    fieldValue.ErrorException(fieldValue.GetFieldDescription() + " Hours cannot be more than 23");
                                                if (minutes > 59)
                                                    fieldValue.ErrorException(fieldValue.GetFieldDescription() + " Minutes cannot be more than 59");
                                                if (fieldValue.formField.IsDateRange == 2)
                                                {
                                                    if (spaceIndex == -1 && hours * 60 + minutes <= dateTimeFieldValue.BaseDate.Value.Hour * 60 + dateTimeFieldValue.BaseDate.Value.Minute)
                                                        days += 1;// if base date time starts after 00:00 and time entered is < then it is assumed to be next day
                                                }
                                                else
                                                {
                                                    if (spaceIndex == -1 && hours * 60 + minutes < dateTimeFieldValue.BaseDate.Value.Hour * 60 + dateTimeFieldValue.BaseDate.Value.Minute)
                                                        days += 1;// if base date time starts after 00:00 and time entered is < then it is assumed to be next day
                                                }
                                                fieldValue.Value = dateTimeFieldValue.BaseDate.Value.Date.
                                                     AddDays(days).
                                                     AddHours(hours).
                                                     AddMinutes(minutes).
                                                     ToString(DAOBasic.DBDateTimeFormat);
                                            }
                                            else
                                            {
                                                var dateFormat = AppPress.Settings.NetDateFormat;
                                                if (a.remoteLoginUserId != null)
                                                    dateFormat = a.remoteData.NetDateFormat;
                                                if (fieldValue.formField.Style == FormDefFieldStyle.Month)
                                                    dateFormat = AppPress.Settings.NetDateMonthFormat;
                                                var dateFormats = new List<string>();
                                                dateFormats.Add(dateFormat);
                                                string sDateFormats = AppPress.Settings.AdditionalInputDateFormats;
                                                if (sDateFormats != null)
                                                    dateFormats.AddRange(sDateFormats.Split('|'));
                                                var Error = "Date '" + fieldValue.Value + "' should be entered as: ";
                                                foreach (var df in dateFormats)
                                                    try
                                                    {
                                                        fieldValue.Value = DateTime.ParseExact(fieldValue.Value, df.Trim(), CultureInfo.InvariantCulture).Date.ToString(DAOBasic.DBDateTimeFormat);
                                                        Error = null;
                                                        break;
                                                    }
                                                    catch
                                                    {
                                                    }
                                                if (Error != null)
                                                {
                                                    sDateFormats = null;
                                                    foreach (var df in dateFormats)
                                                    {
                                                        if (sDateFormats != null)
                                                            sDateFormats += ", ";
                                                        sDateFormats += DateTime.Now.Date.ToString(df.Trim(), CultureInfo.InvariantCulture);
                                                    }

                                                    fieldValue.Error = Error + sDateFormats;
                                                    throw new AppPressException();
                                                }

                                            }
                                        }
                                }
                                catch (Exception)
                                {
                                    a.appPressResponse.Add(AppPressResponse.SetFocus(formData.formDefId, formData.id, fieldValue.formField));
                                    throw;
                                }
                            if (!fieldValue.NotFromClient && (Math.Abs(fieldValue.FormData.formDef.id) % 10 == AppPress.LocalInstanceId))
                            {
                                var s = fieldValue.GetSecurityKey(a);
                                if (fieldValue.security != s)
                                    throw new Exception("Security Error: O: " + fieldValue.security + " N: " + s);
                            }
                        }
                        formData.fieldValues.Sort((x, y) => formData.formDef.formFields.FindIndex(t => t == x.formField) - formData.formDef.formFields.FindIndex(t => t == y.formField));
                    }

                    var topContainer = a.fieldValue.FormData;
                    while (topContainer.containerFieldValue != null && topContainer.containerFieldValue.formField.Type == FormDefFieldType.FormContainerDynamic)
                        topContainer = topContainer.containerFieldValue.FormData;

                    if (a.functionCall == "AutoCompleteOptions")
                    {
                        ((PickFieldValue)a.fieldValue).options = null;
                        a.autoCompleteOptions = ((PickFieldValue)a.fieldValue).GetOptions(a);

                    }
                    else if (a.functionCall == "OnChange" || a.functionCall == "DeleteFile")
                    {
                        if (a.RefreshFields == null)
                            a.RefreshFields = new List<string>();
                        if (a.fieldValue.formField.Type == FormDefFieldType.FileUpload)
                            a.RefreshFields.Add(a.GetHtmlId(a.fieldValue.formField.fieldName));
                        if (a.RefreshFields.Count() > 0)
                        {
                            var fieldValues = new List<FieldValue>();
                            foreach (var htmlId in a.RefreshFields)
                            {
                                string[] s = htmlId.Split(',')[0].Split(AppPress.IdSep.ToCharArray());
                                var formDefId = long.Parse(s[2]);
                                var formId = s[4];
                                var fieldDefId = long.Parse(s[3]);
                                var formData = a.formDatas.Find(t => t.formDefId == formDefId && t.id == formId);
                                FieldValue fieldValue = null;
                                if (formData != null)
                                    if (formData.IsDeleted)
                                        continue;
                                    else
                                        fieldValue = formData.fieldValues.Find(t => t.formField.id == fieldDefId);
                                if (fieldValue == null && formData != null)
                                {
                                    // this field may not be submitted from Client as it is Static. Create a new Field
                                    var className = formData.formDef.GetFormField(fieldDefId).GetClassName(formData.formDef);
                                    fieldValue = AppPress.CreateFieldValue(className);


                                    fieldValue.formField = formData.formDef.formFields.Find(t => t.id == fieldDefId);
                                    fieldValue.fieldDefId = fieldValue.formField.id;
                                    fieldValue.FormData = formData;
                                    fieldValue.NotFromClient = true;
                                    formData.fieldValues.Add(fieldValue);
                                }
                                if (fieldValue != null)
                                    fieldValues.Add(fieldValue);

                                RefreshFields(a, fieldValues);
                            }
                        }
                        if (a.functionCall == "DeleteFile")
                        {
                            a.fieldValue.Value = null;
                            a.SetPageDirty(true);
                        }
                        a.fieldValue.InvokeOn(a, a.functionCall, true);
                        Util.InvokeFunctions(a.fieldValue.formField.GetFieldFunctions(FunctionType.OnChange), a);
                    }
                    else if (a.functionCall == "OnClick")
                    {
                        if (a.fieldValue.ReadOnly == FieldReadonlyType.Readonly)
                            throw new AppPressException("Cannot Call OnClick of a disabled button");
                        if (!a.fieldValue.InvokeOn(a, a.functionCall, true))
                        {
                            var fieldFunctions = a.fieldValue.formField.GetFieldFunctions(FunctionType.OnClick);
                            if (fieldFunctions.Count() > 0)
                                Util.InvokeFunctions(fieldFunctions, a);
                            else
                                throw new AppPressException("No OnClick Function defined with Class: " + a.fieldValue.formField.GetClassName().Replace("+", "."));
                        }
                    }
                    else if (a.functionCall == "RefreshField")
                    {
                        a.RefreshContainer(RefreshFieldFormData.fieldValues.Find(t => t.fieldDefId == RefreshFieldFieldDefId));
                    }
                    else if (a.functionCall != null)
                    {
                        ServerFunction serverFunction = new ServerFunction(a, FunctionType.None, a.functionCall);
                        Util.InvokeFunction(serverFunction, a);
                    }
                    //var sData = SessionData.TryGetSessionData();
                    //if (sData != null && a.SessionID != 0 && a.SessionID != a.sessionData.uniqueId)
                    //    throw new Exception("Session Id and Page Session Id do not Match. SessionId:" + a.SessionID + " Page Session Id" + a.sessionData.uniqueId);

                    //throw new SessionExpiredException(); // for testing
                    // move all FieldError and FormError messages to end as they will need popups to rendered
                    var errorResponses = a.appPressResponse.FindAll(t => t.appPressResponseType == AppPressResponseType.FormError || t.appPressResponseType == AppPressResponseType.FieldError || t.appPressResponseType == AppPressResponseType.FieldHelp);
                    a.appPressResponse.RemoveAll(t => errorResponses.Find(t1 => t1 == t) != null);
                    a.appPressResponse.AddRange(errorResponses);

                    if (a.remoteLoginUserId != null)
                    {
                        a.remoteFormDefs = new List<FormDef>();
                        foreach (var formData in a.formDatas)
                            a.remoteFormDefs.Add(formData.formDef);
                    }
                    a.functionCall = savedFunctionCall;
                    s1 = a.Serialize();
                    a.remoteFormDefs = null;

                    if (a.site.trans != null)
                        throw new Exception("BeginTrans not followed by CommitTrans or RollbackTrans.");
                }
                catch (Exception ex)
                {
                    if (a.site.trans != null)
                        throw new Exception("BeginTrans not followed by CommitTrans or RollbackTrans.");
                    Log.Writeln("Ajax Error: " + ex.Message);

                    if (!(ex.GetBaseException() is AppPressException))
                    {
                        a.appPressResponse = new List<AppPressResponse>();
                        if (ex.GetType().Equals(typeof(AppPressFramework.SessionExpiredException)) || (ex.InnerException != null && ex.InnerException.GetType().Equals(typeof(AppPressFramework.SessionExpiredException))))
                            try
                            {
                                AppPressLogic.OnSessionExpiredException(a);
                            }
                            catch (AppPressException)
                            {
                                // ignore 
                            }
                        else
                        {
                            a.formDatas = null;
                            Exception e = ex.InnerException != null ? ex.InnerException : ex;
                            string error = @"Oops! Error occurred in the application. Please try again.\n
                                            If you frequently face this error then please contact to System Administration with detail of steps to reproduce.\n\n\n";

                            error += "<div style=\"font-size:12px; color:#808080\" onclick=\"$('#errorDetails').slideToggle('slow');\"> More Details</div>";
                            error += "<div id='errorDetails' style=\"margin-top:15px;font-size:13px; color:#808080; display:none\" > ";
                            error += "Error: " + e.Message;
                            if (Request.Url != null)
                                error += "<br /><br />Url:" + Request.Url;
                            if (Request.UrlReferrer != null)
                                error += "<br />UrlReferrer:" + Request.UrlReferrer;
                            error += "<br />StackTrace:<br />" + e.StackTrace + "</div>";
                            try
                            {
                                string loginUserId = null;
                                if (a.sessionData != null)
                                    loginUserId = a.sessionData.loginUserId;
                                a.SendEmail(AppPress.Settings.DebugEmail, null, "Ajax Error in AppPress", "URL: " + Request.Url.AbsoluteUri + "\nIP: " + HttpContext.Current.Request.UserHostAddress + "\nUrlReferrer: " + Request.UrlReferrer + "\n Login User: " + loginUserId + "\n Error: " + e.Message + "\n Stack: " + e.StackTrace, null, null, false);
                            }
                            catch { }
                            a.appPressResponse.Add(AppPressResponse.AlertMessage(error, "Application Error", 550, true));
                        }
                    }
                    else
                    {
                        var sex = (AppPressException)ex.GetBaseException();
                        if (sex.clientAction != null)
                            a.appPressResponse.Add(sex.clientAction);

                    }
                    a.functionCall = savedFunctionCall;
                    s1 = a.Serialize();
                }


                //throw new SessionExpiredException(); // for Testing

                Response.Clear();
                Response.ContentType = "Application/JSON";
                Response.Write(s1);
                Log.Writeln("AjaxResponse: " + Request.Url.AbsoluteUri);

            }
            catch (Exception ex)
            {
                Log.Writeln("Ajax Unknown Error: " + ex.Message);


            }
            finally
            {

                a.site.Close();
            }

        }


        internal static List<Type> GetTypes(string className)
        {
            var types = new List<Type>();
            foreach (var assembly in AppPress.Assemblies)
            {
                var t1 = assembly.assembly.GetType(assembly.namespaceName + "." + className);
                if (t1 != null)
                    types.Add(t1);
            }
            return types;
        }
        internal static Type GetType(string className, bool fromRuntimeAssemblyOnly = false)
        {
            foreach (var assembly in AppPress.Assemblies)
            {
                if (fromRuntimeAssemblyOnly)
                    if (assembly.runtime)
                        continue;
                var t1 = assembly.assembly.GetType("ApplicationClasses." + className);
                if (t1 != null)
                    return t1;
            }
            return null;
        }
        internal static Type GetType(AppPress a, FormDef formDef, long fieldDefId)
        {
            var formField = formDef.GetFormField(fieldDefId);
            var calcClassName = "";
            if (formField.formDef.FormType == FormType.MergedForm || formField.formDef.FormType == FormType.PluginForm)
                calcClassName = formField.GetClassName();
            else
                calcClassName = FormField.GetClassName(formDef, fieldDefId);
            return GetType(calcClassName);
        }

        internal static string RemoveScripts(AppPress a, string html)
        {
            // extract the scripts in the html
            int startIndex = 0;
            string betweenStr = null;
            while (true)
            {
                var sIndex = startIndex;
                html = html.Substring(0, startIndex) + FormField.RemoveBetweenMarkers(a, html.Substring(startIndex), 0, Markers.ScriptBeginMarker, Markers.ScriptEndMarker, out startIndex, out betweenStr, "");
                if (startIndex < 0)
                    break;
                a.JsStr.Append(betweenStr.Substring(Markers.ScriptBeginMarker.Length, betweenStr.Length - Markers.ScriptBeginMarker.Length - Markers.ScriptEndMarker.Length));
                startIndex += sIndex;
            }
            html = RemoveBeginEndMarkers(a, html);
            return html;
        }

        internal static string RemoveBeginEndMarkers(AppPress a, string html)
        {
            // remove begin end markers
            if (a.skinType == SkinType.DOCX)
                html = Regex.Replace(html, @"&lt;!--\|[a-zA-Z0-9_.]+\|--&gt;", "", RegexOptions.Singleline);
            else
            {
                //var match = Regex.Match(html, @"<!--\|[a-zA-Z0-9_.]+\|-->", RegexOptions.Singleline);
                //if (match.Success)
                //    throw new Exception("In Skin Found Marker: " + match.Value + " which does not match any field.");
                html = Regex.Replace(html, @"<!--\|[a-zA-Z0-9_.]+\|-->[\r\n]*", "", RegexOptions.Multiline);
                html = Regex.Replace(html, @"^\s*$\n|\r", "", RegexOptions.Multiline);
            }
            html = html.Replace(AppPress.HeaderSignature, a.GetBottomScript() + AppPress.HeaderSignature);
            return html;
        }

        internal static Object CreateInstance(string className)
        {
            foreach (var assembly in AppPress.Assemblies)
            {
                var obj = assembly.assembly.CreateInstance("ApplicationClasses." + className);
                if (obj != null)
                    return obj;
            }
            return null;
        }

        internal static void ChangeColumnEncryption(DAOBasic site, string tableName, string columnName, EncryptionType currentType, EncryptionType newType, string dbName)
        {
            long id = -1;
            tableName = site.SQLQuote + "" + dbName + @"" + site.SQLQuote + "." + site.SQLQuote + "" + tableName + @"" + site.SQLQuote;
            while (true)
            {
                var dr = site.ExecuteQuery(@"Select Id," + site.SQLQuote + "" + columnName + "" + site.SQLQuote + " From " + tableName + " Where id > " + id + " and " + site.SQLQuote + "" + columnName + "" + site.SQLQuote + " like 'EAAAA%' Order by id asc");
                try
                {
                    if (!dr.Read())
                        break;
                    id = dr.GetInt64(0);
                    if (dr.IsDBNull(1))
                    {
                        dr.Close();
                        continue;
                    }
                    var data = dr.GetString(1);
                    if (data.IsNullOrEmpty())
                    {
                        dr.Close();
                        site.ExecuteNonQuery("Update " + tableName + " Set " + site.SQLQuote + "" + columnName + "" + site.SQLQuote + "=null Where Id=" + id);
                        continue;
                    }
                    if (currentType != EncryptionType.None)
                    {
                        switch (currentType)
                        {
                            case EncryptionType.AES:
                                data = APCrypto.DecryptStringAES(data);
                                break;
                            case EncryptionType.DES:
                                data = Util.DecryptDES(data);
                                break;
                        }
                    }
                    if (newType != EncryptionType.None)
                    {
                        switch (newType)
                        {
                            case EncryptionType.AES:
                                try
                                {
                                    // check if already encrypted
                                    var t = APCrypto.DecryptStringAES(data);

                                }
                                catch
                                {
                                    data = APCrypto.EncryptStringAES(data);
                                }
                                break;
                            case EncryptionType.DES:
                                try
                                {
                                    // check if already encrypted
                                    var t = Util.DecryptDES(data);
                                }
                                catch
                                {
                                    data = Util.EncryptDES(data);
                                }
                                break;
                        }
                    }
                    dr.Close();
                    site.ExecuteNonQuery("Update " + tableName + " Set " + site.SQLQuote + "" + columnName + "" + site.SQLQuote + "='" + site.EscapeSQLString(data) + "' Where Id=" + id);
                }
                finally
                {
                    dr.Close();
                }
            }
        }
    }
}
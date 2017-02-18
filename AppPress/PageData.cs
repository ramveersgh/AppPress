using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Globalization;
using System.Web;
using System.IO;
using System.Data;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Text.RegularExpressions;
using System.Configuration;
using System.Net.Configuration;
using System.Runtime.Serialization;
using System.Security;
using System.Runtime.InteropServices;
using System.Net;
using System.Collections.Specialized;

namespace AppPressFramework
{
    [DataContract]
    internal class DialogData
    {
        [DataMember]
        internal int InstanceId;
        [DataMember]
        internal long callerFormDefId;
        [DataMember]
        internal long callerFieldDefId;
        [DataMember]
        internal string callerFormDataId;

    }

    [DataContract]
    public class RemoteData
    {
        [DataMember]
        public string JQueryDateFormat;
        [DataMember]
        public string NetDateFormat;
        [DataMember]
        public string NetDateTimeFormat;
        [DataMember]
        public List<string> SecureUrls = new List<string>();
    }
    [DataContract]
    internal class EmbededFormField
    {
        [DataMember]
        internal string TableName;
        [DataMember]
        internal string FieldName;
        [DataMember]
        internal string EmbeddedTableName;
    }

    public class OptionsCacheTables
    {
        public string tableName;
        public FormField optionFormField;
        public FormDef formDef;
    }
    public class FormSkin
    {
        public SkinType skinType;
        public string skin;
        public string skinFileName;
        public DateTime? skinFileTime;
        public int index = 0;
    }
    public class SkinFileData
    {
        public string skin;
        public string skinFileName;
        public int functionId;
    }
    [DataContract]
    public class AppPress
    {
        public static int LocalInstanceId = 0; // junk to ensure initialization
        internal static AppPressSettings Settings = null;
        internal static Dictionary<string, Dictionary<string, string>> LocalizationData = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        internal static List<string> LocalizationLanguages = new List<string>();
        internal static Dictionary<string, Dictionary<string, string>> InternalMessageData = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        public const string IdSep = ":";
        public const string auditEndOfLine = ";   ";
        internal const string HeaderSignature = "<!--824798749872984729724984927-->";
        /// <summary>
        /// Display for "Multiple" in UI for Number field.
        /// </summary>
        public const decimal DecimalMultiple = -999999999923212999.377834784m;
        /// <summary>
        /// Display for Multiple Text
        /// </summary>
        public const string MultipleString = "Multiple";

        /// <summary>
        /// HTTP Request. Same as HttpContext.Current.Request in .net
        /// </summary>
        public HttpRequest Request;
        /// <summary>
        /// HTTP Request. Same as HttpContext.Current.Response in .net
        /// </summary>
        public HttpResponse Response;
        /// <summary>
        /// URL of the Page in Browser. In Ajax Call like OnClick the Request.URL containes URL of Ajax Call. To get the URL of Page visible in browser use this
        /// </summary>
        public System.Collections.Specialized.NameValueCollection PageURL = null;

        // Data
        [DataMember]
        internal List<FormData> formDatas = new List<FormData>();
        /// <summary>
        /// Time stamp for the Page.
        /// </summary>
        [DataMember]
        public long PageTimeStamp = DateTime.UtcNow.Ticks;
        [DataMember]
        internal FieldValue fieldValue;// points to the fieldvalue which initiated this call
        internal FieldValue originalFieldValue;// Copy of field value for use in Prompt Client
        [DataMember]
        internal DialogData[] PopupDatas = new DialogData[10]; // max 10 popups
        [DataMember]
        internal string functionCall;
        [DataMember]
        internal string autoCompleteTerm;
        [DataMember]
        internal int pageStackCount = 0;
        // Server instructions to Client
        [DataMember]
        internal List<AppPressResponse> appPressResponse = new List<AppPressResponse>();
        [DataMember]
        internal long SessionID; // This is serialized to browser. On Ajax Call check this against the ID in Session. Use case: Login with admin. Open new tab login with nonadmin. Go back to admin Tab and click on Employee Administration. Should give error
        [DataMember]
        internal long pageID; // uniqueID generated for Page
        [DataMember]
        internal Dictionary<string, List<string>> DependentFields = new Dictionary<string, List<string>>();
        // used to get result of PromtAction like Delete confirmation
        [DataMember]
        internal string PromptClientResult;
        // used to get the fieldValue
        [DataMember]
        internal string formDataId;
        [DataMember]
        internal List<Option> autoCompleteOptions;
        // serialized to client for JS to get formFields
        [DataMember]
        internal List<FormFieldJS> formFields = null;
        [DataMember]
        internal List<string> RefreshFields = null;
        /// <summary>
        /// If the Page has some unsaved Data
        /// </summary>
        [DataMember]
        public bool PageDirty = false;

        public bool IgnoreDemoHrmatesSaveError = false; // move this AppPress derived class in Application Logic

        public string SQLQuote { get { return site.SQLQuote; } }
        public DatabaseType databaseType { get { return site.databaseType; } }

        public static List<AppPressAssembly> Assemblies = new List<AppPressAssembly>();

        [DataMember]
        public int screenWidth, screenHeight;
        [DataMember]
        public int documentWidth, documentHeight;
        [DataMember]
        public int windowWidth, windowHeight;
        internal static long startTime; // get a unique number in life time of application start
        internal static List<FormDef> formDefs = new List<FormDef>();
        internal static List<OptionsCacheTables> optionsCacheTables = new List<OptionsCacheTables>();
        internal static List<FormDef> originalFormDefs = null;
        internal static List<EmbededFormField> EmbeddedFormsTables = new List<EmbededFormField>();
        [DataMember]
        public int instanceId; // instance id of Server who should server this request.
        [DataMember]
        public string remoteLoginUserId = null; // Login User id of remote Server who made the remote Redirect or Popup Request
        [DataMember]
        public int remoteInstanceId; // instanceId of remoteLoginUserId
        [DataMember]
        public List<FormDef> remoteFormDefs = null; // List of remote FormDefs for RemoteForms
        [DataMember]
        public RemoteData remoteData = null; // Date Forms etc for this instance sent to remote
        [DataMember]
        internal string MasterContentAreaFormName = null;
        [DataMember]
        public int MasterContentAreaInstanceId = AppPress.LocalInstanceId; // instanceId of Form in Content Area. By RemoteRedirect

        internal static Dictionary<string, SkinFileData> skins = null;
        internal static Dictionary<string, int> codeFragments = new Dictionary<string, int>();

        internal static Assembly SkinAssembly = null;
        internal static Dictionary<int, string> SkinCode = new Dictionary<int, string>();
        public StringBuilder JsStr = new StringBuilder();
        internal List<string> LinksGenerated = new List<string>();
        public const string QuerySeperator = "kdkddkmdmdklm";
        public const string QuerySeperator1 = "hjjfjhebfjb";

        public object ApplicationData = null;

        internal static long newFormID = -2;
        internal bool DoNotSerialize = false;
        internal bool inTransaction = false;


        public static SessionData TryGetSessionData()
        {
            if (HttpContext.Current == null || HttpContext.Current.Session == null)
                return null;
            return HttpContext.Current.Session["SessionData"] as SessionData;
        }
        /// <summary>
        /// SessionData containing the session.
        /// </summary>
        public SessionData sessionData
        {
            get
            {
                if (HttpContext.Current == null)
                    return null;
                var sessionData = HttpContext.Current.Session["SessionData"] as SessionData;
                if (sessionData == null)
                {
                    AppPressLogic.OnSessionExpiredException(this);
                }
                return sessionData;
            }
            set
            {
                var dateF = DateTime.Now.ToString(DAOBasic.DBDateTimeFormat);
                if (value == null)
                    site.ExecuteNonQuery(@"Insert into Application_Audit(UserName,Time,AuditType,TableName,RowId," + site.SQLQuote + @"Change" + site.SQLQuote + @",TimeStamp)
                        Values ('" + sessionData.email + @"','" + dateF + "'," + (int)AuditType.Logout + @",''," + sessionData.loginUserId + @",''," + DateTime.UtcNow.Ticks + @")");

                else if (value.loginUserId != null)
                {
                    site.ExecuteNonQuery(@"Insert into Application_Audit(UserName,Time,AuditType,TableName,RowId," + site.SQLQuote + @"Change" + site.SQLQuote + @",TimeStamp)
                        Values ('" + value.email + @"','" + dateF + "'," + (int)AuditType.Login + @",''," + value.loginUserId + @",''," + DateTime.UtcNow.Ticks + @")");
                    SessionID = value.uniqueId;
                }
                HttpContext.Current.Session["SessionData"] = value;
            }
        }
        /// <summary>
        /// Contains Methods for Database operations like Query Execution, Updation etc.
        /// Bound to Database references in AppPressSqlServer connection string in web.config
        /// </summary>
        internal DAOBasic site;
        public DAOBasic GetSite()
        {
            return site;
        }
        public string dbName { get { return site.dbName; } }
        /// <summary>
        /// Checks if Table Exists in Database
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public bool DBTableExists(string tableName)
        {
            return site.DBTableExists(tableName);
        }
        /// <summary>
        /// Checks if Routine Exists in Database
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public bool DBRoutineExists(string routineName)
        {
            return site.DBRoutineExists(routineName);
        }
        /// <summary>
        /// Checks if View Exists in Database
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public bool DBViewExists(string viewName)
        {
            return site.DBViewExists(viewName);
        }
        /// <summary>
        /// Executes a query 
        /// </summary>
        /// <param name="query"></param>
        /// <returns>returns the value of first column of first row read. returns null if no Row read</returns>
        public Object ExecuteScalar(string query)
        {
            return site.ExecuteScalar(query);
        }
        /// <summary>
        /// Executes a query 
        /// </summary>
        /// <param name="query"></param>
        /// <returns>returns the string value of first column of first row read. returns null if no Row read</returns>
        public string ExecuteString(string query)
        {
            return site.ExecuteString(query);
        }
        /// <summary>
        /// Executes a query 
        /// </summary>
        /// <param name="query"></param>
        /// <returns>returns the string value of first column of all rows read as a List.</returns>
        public List<string> ExecuteStringList(string query)
        {
            return site.ExecuteStringList(query);
        }
        public List<int> ExecuteIntList(string query)
        {
            return site.ExecuteIntList(query);
        }
        public List<DateTime> ExecuteDateTimeList(string query)
        {
            return site.ExecuteDateTimeList(query);
        }
        /// <summary>
        /// Executes the Query and returns all columns of first row as List of object values
        /// </summary>
        /// <param name="query">Query to Execute</param>
        /// <returns>List of Object values, null if no row if returned</returns>
        public List<Object> ExecuteScalarList(string query)
        {
            return site.ExecuteScalarList(query);
        }
        /// <summary>
        /// Executes the Query and returns all columns of first row as List of object values
        /// </summary>
        /// <param name="query">Query to Execute</param>
        /// <returns>List of Object values, null if no row if returned</returns>
        public List<List<Object>> ExecuteArrayList(string query)
        {
            return site.ExecuteArrayList(query);
        }
        /// <summary>
        /// Executes a query 
        /// </summary>
        /// <param name="query"></param>
        /// <returns>returns the int value of first column of all rows read as a List.</returns>
        public List<int> ExecuteInt32List(string query)
        {
            return site.ExecuteIntList(query);
        }
        /// <summary>
        /// Executes a query 
        /// </summary>
        /// <param name="query"></param>
        /// <returns>returns the int value of first column of all rows read as a List.</returns>
        public List<Int64> ExecuteInt64List(string query)
        {
            return site.ExecuteInt64List(query);
        }
        /// <summary>
        /// Executes the Query
        /// </summary>
        /// <param name="query"></param>
        /// <returns>DateTime value of first column of first row read. null of no row read</returns>
        public DateTime? ExecuteDateTime(string query)
        {
            return site.ExecuteDateTime(query);
        }
        /// <summary>
        /// Executes the Query
        /// </summary>
        /// <param name="query"></param>
        /// <returns>Int32 value of first column of first row read. null of no row read</returns>
        public int? ExecuteInt32(string query)
        {
            return site.ExecuteInt(query);
        }
        /// <summary>
        /// Executes the Query
        /// </summary>
        /// <param name="query"></param>
        /// <returns>Int64 value of first column of first row read. null of no row read</returns>
        public long? ExecuteInt64(string query)
        {
            return site.ExecuteInt(query);
        }

        /// <summary>
        /// Executes the Query
        /// </summary>
        /// <param name="query"></param>
        /// <returns>Decimal value of first column of first row read. null of no row read</returns>
        public decimal? ExecuteDecimal(string query)
        {
            return site.ExecuteDecimal(query);
        }
        /// <summary>
        /// Executes the Query
        /// </summary>
        /// <param name="query"></param>
        /// <returns>DataReader</returns>
        public IDataReader ExecuteQuery(string query)
        {
            return site.ExecuteQuery(query);
        }
        /// <summary>
        /// Executes a insert query.
        /// This query can be executed only after BeginTrans is executed.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="tableName">Table where the insert is being executed</param>
        /// <returns>value of primary key of the table for the new row which is inserted</returns>
        public long ExecuteIdentityInsert(string query, string tableName)
        {
            if (!inTransaction)
                throw new Exception("Updates can be executed only after you have done BeginTrans.");
            return site.ExecuteIdentityInsert(query, tableName);
        }
        /// <summary>
        /// Executes a update query.
        /// This query can be executed only after BeginTrans is executed.
        /// </summary>
        /// <param name="query"></param>
        /// <returns>number of record effected</returns>
        public int ExecuteNonQuery(string query)
        {
            if (!inTransaction)
                throw new Exception("Updates can be executed only after you have done BeginTrans.");
            return site.ExecuteNonQuery(query);
        }

        internal bool AddToDependentFields = false;
        public void DisableAutoRefresh()
        {
            AddToDependentFields = false;
        }
        /// <summary>
        /// Type of Skin that is being generated
        /// </summary>
        public SkinType skinType = SkinType.HTML;
        public CallReasonType CallReason = 0;
        /// <summary>
        /// Adds the Form to Application. If Form by this name already exists then replaces the Existing Form. 
        /// Both added form and replaced form is any should be of Type Application
        /// Any Custom Skin created for the form will be ignored
        /// </summary>
        /// <param name="formDef"></param>
        public void AddFormDef(FormDef formDef)
        {
            if (formDef.FormType != FormType.Application && formDef.FormType != FormType.MergedForm)
                throw new AppPressException("Can add Form: " + formDef.formName + " of Type Application or MergedForm only");
            foreach (var formField in formDef.formFields)
            {
                if (formField.formDef == null)
                {
                    formField.formDef = formDef;
                }
            }
            var existingFormDef = AppPress.formDefs.Find(t => t.formName == formDef.formName);
            if (existingFormDef != null)
            {
                if (existingFormDef.FormType != formDef.FormType)
                    throw new AppPressException("While adding Form: " + formDef.formName + " Found Existing Form with same name of Type: " + existingFormDef.FormType + " Which is different from Type: " + formDef.FormType + " of Form being added");
                AppPress.formDefs.RemoveAll(t => t.formName == formDef.formName);
            }
            AppPress.formDefs.Add(formDef);
            formDef.Skins.RemoveAll(t => t.skinType == SkinType.HTML);
            formDef.Skins.Add(new FormSkin { skinType = SkinType.HTML, skin = formDef.GenerateSkin(this, false, null) });
        }

        internal ServerFunction serverFunction;
        internal bool ignoreSkin;
        internal static System.Object lockLoadFormDefs = new System.Object();
#if DEBUG
        internal List<FieldValue> fieldsNotGenerated;
#endif
        internal FieldValue sourceField;
        internal long lastApplicationAuditId; // Id of row at time of Begin Transaction. Will check for concurrent updates before this id
        internal string URLFormName;
        internal string SkinFileName;
        internal bool StopExecution = false;
        internal string GetPDFParams = null;
        internal string GetDOCXParams = null;

        public AppPress()
        {
            if (System.Web.HttpContext.Current != null)
            {
                this.Request = System.Web.HttpContext.Current.Request;
                this.Response = System.Web.HttpContext.Current.Response;
            }
            for (int i = 0; i < PopupDatas.Count(); ++i)
                PopupDatas[i] = new DialogData();
            if (Settings != null) // From TT
                PopupDatas[0].InstanceId = LocalInstanceId;
        }
        /// <summary>
        /// Gets the name of Database for the current top Page or Popup. Used when Popup is displayed from another Database. For Details see Using AppPress across multiple Database Instances
        /// </summary>
        /// <returns></returns>
        public int GetInstanceId()
        {
            return this.PopupDatas[this.pageStackCount].InstanceId;
        }

        public AppPress(DAOBasic site)
            : this()
        {
            // TODO: Complete member initialization
            this.site = site;
            if (HttpContext.Current != null)

            {
                this.Request = HttpContext.Current.Request;
                this.Response = HttpContext.Current.Response;
                if (Request != null && Request.Url != null)
                    PageURL = HttpUtility.ParseQueryString(Request.Url.Query);
            }

        }

        internal void CreateAppPress(HttpRequest Request, HttpResponse Response, FormDef formDef, string id, SkinType skinType)
        {
            this.skinType = skinType;
            CallReason = CallReasonType.PageLoad;
            fieldValue = new FieldValue();
            fieldValue.formField = new FormField();

            JsStr = new StringBuilder();
            LinksGenerated = new List<string>();

            if (id == "l")
                id = sessionData.loginUserId;
            MasterContentAreaFormName = formDef.formName;
            MasterContentAreaInstanceId = AppPress.LocalInstanceId;
            // check security for a root Form loaded from URL
            if (formDef.formName == "SessionExpired")
                throw new SessionExpiredException();
            id = CheckFormSecurity(formDef, id);
            SessionID = 0;
            var sData = AppPress.TryGetSessionData();
            if (sData != null)
            {
                SessionID = sData.uniqueId;
            }

            if (skinType == SkinType.HTML)
            {
                var masterName = formDef.MasterFormName;
                if (masterName != null)
                {
                    formDef = FormDef.FindFormDef(AppPress.formDefs, masterName);
                    if (formDef == null)
                        throw new AppPressException("Could not Find Form:" + masterName);
                }
                id = "-1";
            }

            formDatas = new List<FormData>();
            var formData = fieldValue.FormData = LoadFormData(formDef.id, id, null, null);
            CalcFormDatas(formData, null, true);
        }

        private static void CheckAppPressFunctions()
        {
            var formTypeList = new List<Type>();
            var fieldTypeList = new List<Type>();
            foreach (var assembly in AppPress.Assemblies)
            {
                var methods = assembly.appLogicType.GetMethods();
                var types = assembly.assembly.GetTypes().ToList();
                formTypeList.AddRange(types.FindAll(t => t.BaseType == typeof(FormData)));
                while (true)
                {
                    // find types based on AppPressTypes
                    var moreTypes = types.FindAll(t => formTypeList.Find(t1 => t1 == t) == null && formTypeList.Find(t1 => t1 == t.BaseType) != null);
                    if (moreTypes.Count() == 0)
                        break;
                    formTypeList.AddRange(moreTypes);
                }
                fieldTypeList.AddRange(types.FindAll(t => t.BaseType == typeof(FieldValue) || (t.BaseType != null && (t.BaseType.BaseType == typeof(FieldValue) || (t.BaseType.BaseType != null && t.BaseType.BaseType.BaseType == typeof(FieldValue))))));
                while (true)
                {
                    // find types based on AppPressTypes
                    var moreTypes = types.FindAll(t => fieldTypeList.Find(t1 => t1 == t) == null && fieldTypeList.Find(t1 => t1 == t.BaseType) != null);
                    if (moreTypes.Count() == 0)
                        break;
                    fieldTypeList.AddRange(moreTypes);
                }
            }
            foreach (var assembly in AppPress.Assemblies)
            {
                var methods = assembly.appLogicType.GetMethods();
                foreach (var method in methods)
                {
                    string methodCaseName = null;
                    switch (method.Name.ToLower())
                    {
                        case "aftersave":
                            methodCaseName = "AfterSave";
                            goto case "INIT";
                        case "beforesave":
                            methodCaseName = "BeforeSave";
                            goto case "INIT";
                        case "init":
                            methodCaseName = "Init";
                            goto case "INIT";
                        case "INIT":
                            {
                                if (method.ReturnType != typeof(void))
                                    throw new AppPressException(method.Name + " Function in Assembly: " + assembly.assemblyName + " should be of type void");
                                var parameterErrorMessage = method.Name + " Function in Assembly: " + assembly.assemblyName + " should have 2 Parameters. First Parameter should be of Type AppPress and second of AppPress ClassName Type";
                                var parameters = method.GetParameters();
                                if (parameters.Length != 2)
                                    throw new AppPressException(parameterErrorMessage);
                                if (parameters[0].ParameterType != AppPress.Settings.ApplicationAppPress)
                                    throw new AppPressException(parameterErrorMessage);
                                if (formTypeList.Find(t => t == parameters[1].ParameterType) == null)
                                    throw new AppPressException(method.Name + " Function in Assembly: " + assembly.assemblyName + " should have 2nd Parameter as One of Types Generated by AppPress");
                                break;
                            }
                        case "calc":
                            methodCaseName = "Calc";
                            goto case "ONCHANGE";
                        case "onclick":
                            methodCaseName = "OnClick";
                            goto case "ONCHANGE";
                        case "onchange":
                            methodCaseName = "OnChange";
                            goto case "ONCHANGE";
                        case "ONCHANGE":
                            {
                                if (method.ReturnType != typeof(void))
                                    throw new AppPressException(method.Name + " Function in Assembly: " + assembly.assemblyName + " should be of type void");
                                var parameterErrorMessage = method.Name + " Function in Assembly: " + assembly.assemblyName + " should have 2 Parameters. First Parameter should be of Type AppPress and second of AppPress ClassName Type";
                                var parameters = method.GetParameters();
                                if (parameters.Length != 2)
                                    throw new AppPressException(parameterErrorMessage);
                                if (parameters[0].ParameterType != AppPress.Settings.ApplicationAppPress)
                                    throw new AppPressException(parameterErrorMessage);
                                if (fieldTypeList.Find(t => t == parameters[1].ParameterType) == null)
                                    throw new AppPressException(method.Name + " Function in Assembly: " + assembly.assemblyName + " should have 2nd Parameter as One of Types Generated by AppPress");
                                break;
                            }
                        case "options":
                            methodCaseName = "Options";
                            if (method.ReturnType != typeof(string) && method.ReturnType != typeof(List<Option>))
                                throw new AppPressException(method.Name + " Function in Assembly: " + assembly.assemblyName + " should be of type string or List<Option>");
                            goto case "DOMAIN";
                        case "domain":
                            methodCaseName = "Domain";
                            if (method.ReturnType != typeof(string) && method.ReturnType != typeof(List<FormData>))
                                throw new AppPressException(method.Name + " Function in Assembly: " + assembly.assemblyName + " should be of type string or List<FormData>");
                            goto case "DOMAIN";
                        case "DOMAIN":
                            {
                                var parameterErrorMessage = method.Name + " Function in Assembly: " + assembly.assemblyName + " should have 2 Parameters. First Parameter should be of Type AppPress and second of AppPress ClassName Type";
                                var parameters = method.GetParameters();
                                if (parameters.Length != 2)
                                    throw new AppPressException(parameterErrorMessage);
                                if (parameters[0].ParameterType != AppPress.Settings.ApplicationAppPress)
                                    throw new AppPressException(parameterErrorMessage);
                                if (fieldTypeList.Find(t => t == parameters[1].ParameterType) == null)
                                    throw new AppPressException(method.Name + " Function in Assembly: " + assembly.assemblyName + " should have 2nd Parameter as One of Types Generated by AppPress. At Present it is of Type: " + parameters[1].ParameterType);
                                break;
                            }
                    }
                    if (methodCaseName != null)
                    {
                        if (!method.IsStatic || !method.IsPublic)
                            throw new Exception(method.Name + " should be public static in Assemply: " + assembly.assemblyName);
                        if (methodCaseName != method.Name)
                            throw new Exception("Wrong Case: " + method.Name + " should be " + methodCaseName + " in Assemply: " + assembly.assemblyName);
                    }
                }
            }
        }

        /// <summary>
        /// Saves the file in Application_Files after encryption
        /// </summary>
        /// <param name="fullFilePath">Path to file</param>
        /// <param name="encryptionType">Type of Encryption</param>
        /// <returns></returns>
        public long SaveFile(string fullFilePath, EncryptionType? encryptionType)
        {
            FileInfo fInfo = new FileInfo(fullFilePath);
            string fileName = fInfo.Name;
            long fileSize = fInfo.Length / 1024; //Convert into KB.
            var fileType = AppPressLogic.GetFileType(System.IO.Path.GetExtension(fullFilePath));

            FileStream fs = fInfo.OpenRead();
            byte[] fileData = new byte[fs.Length];
            fs.Read(fileData, 0, System.Convert.ToInt32(fs.Length));
            fs.Close();
            return Util.SaveFile(FileUploadStorageType.Database, null, fileName, fileData, fileType, fileSize, encryptionType, false);
        }
        /// <summary>
        /// Saves the file in Application_Files after encryption
        /// </summary>
        /// <param name="fileContent">Content of file</param>
        /// <param name="encryptionType">Type of Encryption</param>
        /// <returns></returns>
        public long SaveFile(byte[] fileContent, EncryptionType? encryptionType)
        {

            return Util.SaveFile(FileUploadStorageType.Database, null, "Temp", fileContent, "application/octet-stream", fileContent.Count(), encryptionType, false);
        }
        /// <summary>
        /// Add a Audit Entry to Application_Audit table. If you are changing database in Business Logic, use this to add audit entry for the change
        /// </summary>
        /// <param name="TableName">Table where change was made</param>
        /// <param name="TableRowId">Primary key value where change was made in table</param>
        /// <param name="FormName">Form in UI which triggered the change</param>
        /// <param name="PageId">Page Id in UI which triggered the change</param>
        /// <param name="Change">Description of change</param>
        public void SaveDBAudit(AuditType auditType, string TableName, string TableRowId, string FormName, string PageId, string Change, string extraData)
        {
            if (TableName == null)
                TableName = "'n/a'";
            else
                TableName = "'" + EscapeSQLString(TableName) + "'";

            if (FormName == null)
                FormName = "Null";
            else
                FormName = "'" + EscapeSQLString(FormName) + "'";

            if (PageId == null)
                PageId = "Null";
            else
                PageId = "'" + EscapeSQLString(PageId) + "'";

            if (Change == null)
                Change = "Null";
            else
                Change = "'" + EscapeSQLString(Change) + "'";
            if (extraData == null)
                extraData = "Null";
            else
                extraData = "'" + EscapeSQLString(extraData) + "'";

            string qry = @"Insert into Application_Audit(UserName,Time,AuditType,TableName,RowId,Page," + SQLQuote + @"Change" + SQLQuote +
                        @",TimeStamp, PageId, LoginUserId, ExtraData)
                        Values ('" + sessionData.email + @"','" + DateTime.Now.ToString(DAOBasic.DBDateTimeFormat) + "'," + (int)auditType + "," + TableName + "," + TableRowId + @"," + FormName + "," + Change + "," +
                            DateTime.UtcNow.Ticks + @"," + PageId + ", '" + sessionData.loginUserId + "'," + extraData + ")";
            ExecuteNonQuery(qry);

        }
        public void SaveDBAudit(AuditType auditType, string tableName, string TableRowId, FormData pageFormData, string oldValue, string newValue, string extraData)
        {
            if (oldValue != newValue)
            {
                var change = "";
                if (oldValue != null)
                    change += oldValue;
                change += " => ";
                if (newValue != null)
                    change += newValue;
                SaveDBAudit(auditType, tableName, TableRowId, pageFormData.GetFormName(), pageFormData.id, change, extraData);
            }
        }
        /// <summary>
        /// ???
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public string GetJSONFromQuery(string query)
        {
            var dr = ExecuteQuery(query);
            try
            {
                var data = new List<List<string>>();
                while (dr.Read())
                {
                    var dataRow = new List<string>();
                    data.Add(dataRow);
                    for (int i = 0; i < dr.FieldCount; i++)
                    {
                        dataRow.Add(dr[i].ToString());
                    }
                }
                return FormDef.Serialize(data, typeof(List<List<string>>));
            }
            finally
            {
                dr.Close();
            }
        }


        internal static bool IsNewId(string id)
        {
            int intid;
            if (id == null)
                return true;
            if (!int.TryParse(id, out intid))
                return false;
            return intid < 0;
        }
        /// <summary>
        /// Returns a URL which can be emailed. If user open the Link within the given time stamp, he will be able to open the Form with the given formName and formDataId.
        /// </summary>
        /// <param name="formName">Name of Form to Open from Link</param>
        /// <param name="formDataId">Id of Form to open from Link</param>
        /// <param name="loginUserId">Id of user who is Logged in</param>
        /// <param name="ts">TimeSpan for link to expire</param>
        /// <param name="BaseUrl">if not null use this URL to open the Form. Used if opening the form from Another Database. For Details see Using AppPress across multiple Database Instances</param>
        /// <returns></returns>
        public string GetSecureUrl(string formName, string formDataId, string loginUserId, TimeSpan ts, string BaseUrl = null)
        {
            return GetUrl(formName, formDataId, null, BaseUrl) + "&s=" + HttpUtility.UrlEncode(Util.EncryptDES(formName + AppPress.IdSep + formDataId + AppPress.IdSep + loginUserId + AppPress.IdSep + (DateTime.Now + ts).ToString(DAOBasic.DBDateTimeFormat)));
        }
        internal static bool IsSecureForm(FormDef formDef, string id)
        {
            //if (!IsNewId(id))
            //    return true;
            var formName = formDef.formName;
            if (AppPress.Settings.developer)
                return (formName != "SessionExpired" && formName != "ApplicationManager" && formName != "FormsManager" && formName != "DBCrypto") && !formDef.NonSecure;
            return formName != "ApplicationManager" && formName != "SessionExpired" && !formDef.NonSecure;
        }
        /// <summary>
        /// ??? remove after iframe is removed
        /// </summary>
        /// <returns></returns>
        public string ParseSecureUrl()
        {
            var s = Request["s"];
            if (s != null)
            {
                s = Util.DecryptDES(s);
                var s1 = s.Split(new string[] { AppPress.IdSep }, StringSplitOptions.None);
                var id = s1[1];
                if (!id.IsNullOrEmpty() && long.Parse(id) < 0)
                    id = null;
                if (id == null)
                    id = "";
                var formId = s1[1];
                var loginUserId = s1[2];
                var tillTime = s1[3] + ":" + s1[4] + ":" + s1[5];
                if (DateTime.Now > DateTime.ParseExact(tillTime, DAOBasic.DBDateTimeFormat, CultureInfo.InvariantCulture))
                    throw new AppPressException("This link has Expired.");
                return loginUserId;
            }
            return null;
        }
        internal string CheckFormSecurity(FormDef formDef, string id)
        {
            if (IsSecureForm(formDef, id))
            {
                var loginUserId = ParseSecureUrl();
                if (loginUserId != null)
                    return loginUserId;
                var url = this.Request.Url.AbsoluteUri.Replace("&ignoreSkin=", "");
                if (sessionData.formDefIdAndFormIds.Where(t => t == url).Count() == 0)
                    throw new Exception("Security Exception: You are not allowed to access this page. (Form:" + formDef.formName + " Id:" + id + ")");
            }
            return id;

        }
        /// <summary>
        /// ???
        /// </summary>
        /// <param name="formName"></param>
        /// <param name="formDataId"></param>
        /// <param name="redirectParams"></param>
        /// <param name="BaseUrl"></param>
        /// <returns></returns>
        public string GetUrl(string formName, string formDataId, RedirectParams redirectParams, string BaseUrl = null)
        {
            if (BaseUrl.IsNullOrEmpty())
                BaseUrl = GetBaseUrl();
            var u = BaseUrl + GetDefaultAspx() + "?Form=" + formName;
            if (redirectParams != null)
                u += redirectParams.urlParams;
            if (formDataId != null)
            {
                var ssData = AppPress.TryGetSessionData();
                if (ssData != null && formDataId == ssData.loginUserId)
                    formDataId = "l";// Do not show Id to enduser. this will be converted back to LoginUsedId in Request Processing
                u += "&id=" + formDataId;
            }
            return u;
        }


        /// <summary>
        /// To show an Alert to User
        /// </summary>
        /// <param name="message">Message to be displayed in Alert</param>
        /// <param name="title">Title of Alert Popup</param>
        /// <param name="popupWidth">Width of Alert Popup in pixels</param>
        /// <param name="isHtml">if false message will be shown as plain text</param>
        public void AlertMessage(string message, string title = null, int popupWidth = 0, bool isHtml = false)
        {
            appPressResponse.Add(AppPressResponse.AlertMessage(message, title, popupWidth, isHtml));

        }
        /// <summary>
        /// Runs the given Query and returns the result as CSV file.
        /// </summary>
        /// <param name="query">Query to Execute</param>
        /// <param name="CSVFileName">Name of CSV file</param>
        public void DownloadCSV(string query, string CSVFileName)
        {
            string url = Path.GetFileName(Request.Url.LocalPath) + "?getCSV=" + CSVFileName;
            appPressResponse.Add(AppPressResponse.DownloadFile(this, url, "viewQuery", HttpUtility.HtmlEncode(query)));
        }
        /// <summary>
        /// Runs the given Query and returns the result as Excel file.
        /// </summary>
        /// <param name="query">Query to Execute. If Query have multiple results each results is places as seperate sheet in Excel File</param>
        /// <param name="excelFileName">Name of Excel File</param>
        /// <param name="sheetNames">List of Sheet Names in same order as Results from Query</param>
        public void DownloadExcel(string query, string excelFileName, List<string> sheetNames)
        {
            string strSheetNames = "";
            if (sheetNames != null && sheetNames.Count > 0)
            {
                foreach (var item in sheetNames)
                {
                    strSheetNames += item + QuerySeperator;
                }
            }
            string url = Path.GetFileName(Request.Url.LocalPath) + "?getExcel=" + excelFileName;
            if (strSheetNames.Length > 0)
                url += "&sheetNames=" + HttpUtility.HtmlEncode(strSheetNames) + "";
            appPressResponse.Add(AppPressResponse.DownloadFile(this, url, "viewQuery", HttpUtility.HtmlEncode(query)));
        }

        /// <summary>
        /// Show results of the query on a new Tab in Browser as HTML Table
        /// </summary>
        /// <param name="query">Query to execute</param>
        /// <param name="reportTitle">Title of HTML Table</param>
        /// <param name="NumericZeroAsBlank">Show 0 numeric value as blank</param>
        /// <param name="tableHeader">Use this report table header <tr>...</tr>. if null header is generated using column headings</param>
        public void ViewHTML(string query, string reportTitle, bool NumericZeroAsBlank, string tableHeader)
        {
            var path = Path.GetFileName(Request.Url.LocalPath);
            string url = path + "?getHTMLTable=" + reportTitle.Trim() + "&NumericZeroAsBlank=" + (NumericZeroAsBlank ? "1" : "0");
            var message = "<input type='hidden' name=\"viewQuery\" value=\"" + HttpUtility.HtmlAttributeEncode(query.Replace("\r", " ").Replace("\n", " ")) + "\"/>";
            if (tableHeader != null)
                message += "<input type='hidden' name=\"header\" value=\"" + HttpUtility.HtmlAttributeEncode(tableHeader.Replace("\r", " ").Replace("\n", " ")) + "\"/>";
            appPressResponse.Add(AppPressResponse.OpenUrl(this, url, "_new", message));
        }

        /// <summary>
        ///  Generate download URL for uploaded file.
        /// </summary>
        /// <param name="fileId">Id of the file to download</param>
        /// <returns></returns>
        public string GetFileUrl(int fileId)
        {
            string url = "";
            if (remoteLoginUserId != null)
            {
                url = AppPress.Settings.Instances.Find(t => t.InstanceId == remoteInstanceId).InstanceBaseUrl + "?InstanceId=" + AppPress.LocalInstanceId + "&GetFile=&Download=true&id=" + fileId;
                remoteData.SecureUrls.Add(url);
            }
            else
            {
                url = AppPress.GetBaseUrl() + AppPress.GetDefaultAspx() + "?GetFile=&Download=true&id=" + fileId;
                sessionData.AddSecureUrl(url);
            }
            return url;
        }

        /// <summary>
        ///  Generate download URL for uploaded file.
        /// </summary>
        /// <param name="fileId">Id of the file to download</param>
        /// <returns></returns>
        public string GetFileUrl(string fullFilePath)
        {
            string url = AppPress.GetBaseUrl() + AppPress.GetDefaultAspx() + "?GetFile=&Download=true&FilePath=" + fullFilePath;
            sessionData.AddSecureUrl(url);
            return url;
        }
        /// <summary>
        /// Download file uploaded earlier using FileUpload field type in AppPress
        /// </summary>
        /// <param name="fileId">Id of the file to download</param>
        public void DownloadFile(int? fileId)
        {
            if (fileId == null)
                throw new AppPressException("There is no File to Download.");
            string url = Path.GetFileName(Request.Url.LocalPath) + "?GetFile=&Download=true&id=" + fileId;
            appPressResponse.Add(AppPressResponse.DownloadFile(this, url, null, null));
        }
        /// <summary>
        /// Download file uploaded earlier using FileUpload field type in AppPress
        /// </summary>
        /// <param name="fileId">Id of the file to download</param>
        public void DownloadFile(string filePath, string fileName, string contentType)
        {
            string url = Path.GetFileName(Request.Url.LocalPath) + "?GetFile=&Download=true&FilePath=" + HttpUtility.UrlEncode(filePath) + "&ContentType=" + HttpUtility.UrlEncode(contentType);
            if (fileName != null)
                url += "&FileName=" + HttpUtility.UrlEncode(fileName);
            appPressResponse.Add(AppPressResponse.DownloadFile(this, url, null, null));
        }

        internal void OpenUrl(string url, string target)
        {
            appPressResponse.Add(AppPressResponse.OpenUrl(this, url, target, null));
        }
        /// <summary>
        /// Close the top Popup displayed on Page
        /// This function terminates the current. Should be last call in the Logic function
        /// </summary>
        public void ClosePopup()
        {
            appPressResponse.Add(AppPressResponse.CloseWindow(this));
        }
        internal static FormDef FindFormDef(string formName)
        {
            return FormDef.FindFormDef(AppPress.formDefs, formName);
        }
        public static FormDef FindSingleFormDef(string formName)
        {
            var forms = AppPress.formDefs.FindAll(t => t.formName == formName);
            if (forms.Count == 0)
                throw new AppPressException("Could not find Form: " + formName);
            if (forms.Count > 1)
                throw new AppPressException("Found more than 1 Form: " + formName);
            return forms[0];
        }
        /// <summary>
        /// Find Form Definition of given formDefId
        /// </summary>
        /// <param name="formDefId"></param>
        /// <returns></returns>
        public static FormDef FindFormDef(long formDefId)
        {
            return AppPress.formDefs.Find(t => t.id == formDefId);
        }
        internal void CheckContainerfieldValues()
        {
            //if (Util.DEBUG)
            //{
            //    foreach (var f in formDatas)
            //    {

            //        foreach (var fi in f.fieldValues)
            //            if (fi.FormData != f)
            //                throw new Exception("Internal Error: formData in fieldValue does not match FormData");
            //        if (f.containerFieldValue != null)
            //        {
            //            var containerFormData = formDatas.Find(t => t == f.containerFieldValue.FormData);
            //            if (containerFormData == null)
            //                throw new Exception("Internal Error: ContainerFieldValue FormData outside p");
            //            if (containerFormData.GetFieldValue(f.containerFieldValue.formField.fieldName) != f.containerFieldValue)
            //                throw new Exception("Internal Error: ContainerFieldValue not in formData");
            //        }
            //    }
            //}
        }

        internal void CalcFormData(FormData formData, FieldValue fieldValue, bool init)
        {
            var fieldValues = formData.fieldValues;
            if (fieldValue != null)
            {
                fieldValues = new List<FieldValue>();
                fieldValues.Add(fieldValue);
                if (fieldValue.formField.Type == FormDefFieldType.FormContainerDynamic)
                {
                    // add container fields
                    foreach (var f in formData.formDef.formFields)
                        if (f.containerFormField == fieldValue.formField)
                        {
                            fieldValues.Add(formData.GetFieldValue(f.fieldName));
                        }
                }
            }

            if (init && fieldValue == null)
            {
                if (formData.formDef.InitMethods == null)
                {
                    formData.formDef.InitMethods = new List<MethodCache>();
                    var className = formData.formDef.GetClassName();
                    var typeList = new List<Type>();
                    Type t1 = Util.GetType(className);
                    if (t1 != null)
                        typeList.Add(t1);
                    foreach (var formField in formData.formDef.formFields)
                        if (formField.Extension)
                        {
                            var t2 = Util.GetType(formField.formDef.formName + "Class");
                            if (t2 != null && typeList.Find(t => t == t2) == null)
                                typeList.Add(t2);
                        }
                    foreach (var t in typeList)
                    {
                        foreach (var assembly in AppPress.Assemblies)
                        {
                            var method = assembly.appLogicType.GetMethod("Init", BindingFlags.ExactBinding | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static, null, new Type[] { AppPress.Settings.ApplicationAppPress, t }, null);
                            if (method != null)
                                formData.formDef.InitMethods.Add(new MethodCache { method = method, SecondParam = t });
                        }
                    }
                }
                foreach (var methodCache in formData.formDef.InitMethods)
                {
                    var o = formData;
                    if (o.GetType() != methodCache.SecondParam)
                    {
                        o = (FormData)Activator.CreateInstance(methodCache.SecondParam, new object[] { (FormData)o });
                    }
                    Util.InvokeMethod(this, methodCache.method, new object[] { this, o });
                    //if (o != formData)
                    //    o.CovertToFormDefClass();
                }
            }
            //Call Domain functions.
            for (int i = 0; i < fieldValues.Count(); ++i)
            {
                var fieldValue1 = fieldValues[i];
                if (fieldValue1.formField.Type == FormDefFieldType.FormContainerDynamic/* && !fieldValue1.formField.Hidden*/)
                {
                    var t1 = Util.GetType(this, formData.formDef, fieldValue1.formField.id);
                    LoadFormContainerFormDatas(fieldValue1);
                    if (t1 != null)
                    {
                        var methodFound = false;
                        for (int j = 0; j < AppPress.Assemblies.Count; ++j)
                        {
                            var assembly = AppPress.Assemblies[AppPress.Assemblies.Count - j - 1];
                            var method = assembly.appLogicType.GetMethod("Domain", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static, null, new Type[] { AppPress.Settings.ApplicationAppPress, t1 }, null);
                            if (method == null)
                                continue;

                            methodFound = true;
                            AddToDependentFields = true;
                            sourceField = fieldValue1;

                            try
                            {
                                Object obj = null;
                                var pFieldValue = this.fieldValue;
                                this.fieldValue = fieldValue1;
                                try
                                {
                                    obj = Util.InvokeMethod(this, method, new object[] { this, Convert.ChangeType(fieldValue1, t1) });
                                }
                                finally
                                {
                                    this.fieldValue = pFieldValue;
                                }
                                List<FormData> newFormDatas;
                                if (obj == null)
                                    newFormDatas = new List<FormData>();
                                else if (obj.GetType() == typeof(string))
                                {
                                    try
                                    {
                                        if (fieldValue1.formField.GetFormContainerStyle() == FormContainerStyle.InLine)
                                            throw new Exception("Cannot have Domain function for FormContaineInline returning string for " + fieldValue1.formField.GetDescription());
                                        //var rowFormDef = fieldValue1.formField.GetContainerRowFormDef(this);
                                        newFormDatas = FormData.ReadFormDatas(this, fieldValue1, fieldValue1.formField.rowFormDef, (string)obj);
                                    }
                                    catch (Exception ex)
                                    {
                                        throw new Exception(ex.Message + "<br/>SQL Query:<br/>" + obj.ToString());
                                    }
                                }
                                else if (obj.GetType() == typeof(List<FormData>))
                                {
                                    newFormDatas = (List<FormData>)obj;
                                }
                                else
                                    throw new Exception("Invalid Return Type for Function: Domain for " + fieldValue1.formField.GetDescription());
                                ((FormContainerFieldValue)fieldValue1).SetContainedFormDatas(newFormDatas);
                            }
                            catch (Exception Ex)
                            {
                                fieldValue1.Error = "Error in Domain Function";
                                this.AlertMessage(Ex.Message);
                                if (this.site.conn.State != ConnectionState.Open)
                                    throw;
                            }
                            finally
                            {
                                AddToDependentFields = false;
                            }
                        }
                        if (AppPress.Settings.developer && !methodFound)
                        {
                            if (fieldValue1.formField.TableName == null && fieldValue1.formField.OriginalType != (int)FormDefFieldType.MultiFileUpload)
                                if (fieldValue1.formField.FieldFunctions.Find(t => t.ServerFunctionType == FunctionType.Domain) == null)
                                {
                                    var message = fieldValue1.FormData.formDef._GenerateCode(0, fieldValue1.formField, "Domain");
                                    fieldValue1.Error = "<a onclick=\"AlertMessage('Could not find Domain function " + HttpUtility.HtmlAttributeEncode(message.Replace("'", "\\'").Replace("\"", "\\\"").Replace(Environment.NewLine, "<br/>")) + "',900);return true;\"><span style='color:red'>X</span></a>";

                                }
                        }
                    }
                }
            }


        }
        internal void CalcFormDatas(FormData formData, FieldValue fieldValue, bool init)
        {
            // Call Init
            CalcFormData(formData, fieldValue, init);
            foreach (var fieldValue1 in formData.fieldValues.FindAll(t => t.formField.Type == FormDefFieldType.FormContainerDynamic))
            {
                if (fieldValue != null && fieldValue != fieldValue1)
                    continue;
                var childFormDatas = ((FormContainerFieldValue)fieldValue1).GetContainedFormDatas(this);
                foreach (var formData1 in childFormDatas)
                    if (!formData1.IsDeleted)
                        CalcFormDatas(formData1, null, init);
            }
            // Call Calc
            var fieldValues = formData.fieldValues;
            for (int i = 0; i < fieldValues.Count; ++i)
            {
                var fieldValue1 = fieldValues[i];
                if (fieldValue != null && fieldValue != fieldValue1)
                    continue;
                var formField = fieldValue1.formField;
                if (formField.CalcMethods == null)
                {
                    formField.CalcMethods = new List<MethodCache>();
                    var className = formField.GetClassName();
                    var typeList = new List<Type>();
                    Type t1 = Util.GetType(className);
                    if (t1 != null)
                        typeList.Add(t1);
                    if (formField.Extension)
                    {
                        var t2 = Util.GetType(formField.formDef.formName + "Class");
                        if (t2 != null && typeList.Find(t => t == t2) == null)
                            typeList.Add(t2);
                    }
                    foreach (var t in typeList)
                    {
                        foreach (var assembly in AppPress.Assemblies)
                        {
                            var method = assembly.appLogicType.GetMethod("Calc", BindingFlags.ExactBinding | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static, null, new Type[] { AppPress.Settings.ApplicationAppPress, t }, null);
                            if (method != null)
                            {
                                formField.CalcMethods.Add(new MethodCache { method = method, SecondParam = t });
                            }
                        }
                    }
                }

                var calcFunction = formField.FieldFunctions.Find(t => t.ServerFunctionType == FunctionType.Calc);
                if (calcFunction != null)
                {
                    var pFieldValue = this.fieldValue;
                    try
                    {
                        this.fieldValue = fieldValue1;
                        Util.InvokeMethod(this, calcFunction.method, new object[] { this });
                    }
                    finally
                    {
                        this.fieldValue = pFieldValue;
                    }
                }
                foreach (var method in formField.CalcMethods)
                {
                    AddToDependentFields = true;
                    sourceField = fieldValue1;
                    try
                    {
                        // initialize values so that Calc will start from known state
                        if (!init)
                        {
                            fieldValue1.ReadOnly = FieldReadonlyType.None;
                            fieldValue1.Hidden = FieldHiddenType.None;
                            fieldValue1.FieldLabel = null;
                        }
                        Object o = fieldValue1;
                        if (o.GetType() != method.SecondParam)
                        {
                            o = Activator.CreateInstance(method.SecondParam, new object[] { o });
                            sourceField = (FieldValue)o;
                        }
                        var obj = Util.InvokeMethod(this, method.method, new object[] { this, o });
                        switch (fieldValue1.formField.Type)
                        {
                            case FormDefFieldType.Pickone:
                            case FormDefFieldType.PickMultiple:
                                {

                                    ((PickFieldValue)fieldValue1).options = ((PickFieldValue)fieldValue1).GetOptions(this);
                                    break;
                                }
                            case FormDefFieldType.FormContainerDynamic:

                                break;
                        }
                    }

                    finally
                    {
                        AddToDependentFields = false;
                    }
                }
            }

        }
        internal void AddDependentField(FieldValue destFieldValue, FieldValue sourceField)
        {
            var sourceFieldHtmlId = "AppPress" + AppPress.IdSep + (int)sourceField.formField.Type + AppPress.IdSep + sourceField._GetHtmlId();
            var destFieldHtmlId = "AppPress" + AppPress.IdSep + (int)destFieldValue.formField.Type + AppPress.IdSep + destFieldValue._GetHtmlId();
            if (!DependentFields.ContainsKey(sourceFieldHtmlId))
                DependentFields.Add(sourceFieldHtmlId, new List<string>());
            var list = DependentFields[sourceFieldHtmlId];
            if (list.Find(t => t == destFieldHtmlId) == null)
                list.Add(destFieldHtmlId);
        }
        internal static void CheckSubmitIfStaticUsage(FieldValue fieldValue)
        {
            if (fieldValue.Value == null && fieldValue.formField.Static && !fieldValue.formField.StaticSubmitValue && fieldValue.FormData.IsSubmitted)
                throw new Exception("Form: " + fieldValue.formField.formDef.formName + " Field: " + fieldValue.formField.fieldName + " is of type Static. To use its value outside of Init, Add attribute &lt;SubmitIfStatic/&gt; to the Field in XML");

        }
        internal void AddDependentField(FieldValue destFieldValue)
        {
            CheckSubmitIfStaticUsage(destFieldValue);
            if (AddToDependentFields /*&& destFieldValue != sourceField*/ && !destFieldValue.formField.Static && destFieldValue.formField.Type != FormDefFieldType.ForeignKey && !destFieldValue.formField.Hidden)
            {
                AddDependentField(destFieldValue, sourceField);
            }
        }
        internal void AddReverseDependentField(FieldValue fieldValue1)
        {
            fieldValue1.Modified = true;
            if (AddToDependentFields && fieldValue1 != sourceField && !fieldValue1.formField.Static && fieldValue1.formField.Type != FormDefFieldType.ForeignKey && !fieldValue1.formField.Hidden)
            {
                var sourceFieldHtmlId = "AppPress" + AppPress.IdSep + (int)sourceField.formField.Type + AppPress.IdSep + sourceField._GetHtmlId();
                var destFieldHtmlId = "AppPress" + AppPress.IdSep + (int)fieldValue1.formField.Type + AppPress.IdSep + fieldValue1._GetHtmlId();
                if (!DependentFields.ContainsKey(destFieldHtmlId))
                    DependentFields.Add(destFieldHtmlId, new List<string>());
                var list = DependentFields[destFieldHtmlId];
                if (list.Find(t => t == sourceFieldHtmlId) == null)
                    list.Add(sourceFieldHtmlId);
            }
        }

        internal FormData LoadFormData(long formDefId, string id, FieldValue callerFieldValue, FieldValue containerFieldValue)
        {
            var formDef = FindFormDef(formDefId);
            if (formDef == null)
                throw new Exception("Could not find Form with Id: " + formDefId);

            var formData = FormData.InitializeFormData(this, formDef, id);

            formDatas.Add(formData);

            formData.containerFieldValue = containerFieldValue;
            formData.callerFieldValue = callerFieldValue;

            return formData;
        }

        internal void LoadFormContainerFormDatas(FieldValue fieldValue)
        {
            var pFieldValue = this.fieldValue;
            try
            {
                this.fieldValue = fieldValue;
                var domainFunction = fieldValue.formField.FieldFunctions.Find(t => t.ServerFunctionType == FunctionType.Domain);
                if (domainFunction != null)
                {
                    var newFormDatas = (List<FormData>)Util.InvokeFunction(domainFunction, this);
                    if (newFormDatas != null)
                    {
                        newFormDatas.AddRange(formDatas.FindAll(t => t.containerFieldValue == fieldValue && t.IsNew && !t.IsDeleted));
                        foreach (var formData in newFormDatas)
                            if (formDatas.Find(t => t.formDefId == formData.formDefId && t.id == formData.id && t.IsDeleted) != null)
                                formData.IsDeleted = true;

                        ((FormContainerFieldValue)fieldValue).SetContainedFormDatas(newFormDatas);
                    }
                }
            }
            finally
            {
                this.fieldValue = pFieldValue;
            }
        }

        internal string TryGetFunctionParameterValue(string paramName)
        {
            return serverFunction.TryGetFunctionParameterValue(paramName);
        }

        internal string GetFunctionParameterValue(string paramName)
        {
            return serverFunction.GetFunctionParameterValue(paramName);
        }

        public static string GetBaseUrl()
        {
            return Util.GetBaseUrl();
        }
        public static string GetServerUrl()
        {
            return Util.serverUrl;
        }
        internal static string GetLocalizationKeyValue(string Key, string language)
        {
            string value = null;

            if (AppPress.LocalizationData.ContainsKey(Key))
                if (AppPress.LocalizationData[Key].ContainsKey(language))
                    value = AppPress.LocalizationData[Key][language];
                else if (AppPress.LocalizationData[Key].ContainsKey("English"))
                    value = AppPress.LocalizationData[Key]["English"];
            return value;
        }
        /// <summary>
        /// Gets the String for currently selected localization corresponding to Key. Look at Localization in AppPress from more details
        /// </summary>
        /// <param name="Key">Key for the string</param>
        /// <param name="ErrorIfNotFound">if true throws a Exception if Key is not found.</param>
        /// <returns></returns>
        public static string GetLocalizationKeyValue(string Key, bool ErrorIfNotFound = true)
        {
            var sdata = AppPress.TryGetSessionData();
            var CurrentLanguage = "English";
            if (sdata != null && sdata.CurrentLanguage != null)
            {
                int cl = int.Parse(sdata.CurrentLanguage);
                if (cl>= 0 && cl < AppPress.LocalizationLanguages.Count)
                    CurrentLanguage = AppPress.LocalizationLanguages[cl];
            }

            var value = GetLocalizationKeyValue(Key, CurrentLanguage);

            if (ErrorIfNotFound && value == null)
                return Key;

            return value;

        }

        internal string GetDisplayName(string formName, string fieldName)
        {
            if (fieldName == "SelectRow")
                return "";
            var fieldValue1 = fieldValue.FormData.GetFieldValue(fieldName);
            if (fieldValue1 == null)
                throw new Exception("Could not find Field:" + fieldName + " in Form:" + formName);
            return fieldValue1.GetLabel(this);

        }
        internal string GetHtmlErrorId(string fieldName)
        {
            var fieldValue1 = fieldValue.FormData.GetFieldValue(fieldName);
            if (fieldValue1 == null)
            {
                appPressResponse.Add(AppPressResponse.FormError(fieldValue.FormData, "Could not find Field: " + fieldName + " in Form: " + fieldValue.FormData.formDefId));
                return "";
            }
            return fieldValue1.GetHtmlErrorId();
        }
        internal string GetFileUploadScript(string fieldName)
        {
            FieldValue pFieldValue = fieldValue;
            try
            {
                fieldValue = new FieldValue();
                fieldValue.formField = pFieldValue.FormData.formDef.GetFormField(fieldName);
                fieldValue.FormData = pFieldValue.FormData;
                //var s = new StringBuilder();
                //s.Append("$(JQueryEscape('#upload_target" + AppPress.IdSep + fieldValue.GetHtmlId() + "')).load(\n");
                //s.Append("function() {\n");
                //s.Append("var ret = frames['upload_target" + AppPress.IdSep + fieldValue.GetHtmlId());
                //s.Append("'].document.getElementsByTagName('body')[0].innerHTML;\n");
                //s.Append("if (ret != '')\n");
                //s.Append("{\n");
                //s.Append("try { var datas = eval(\"(\"+ret+\")\"); if (datas.Error) {AlertMessage('Error In File Upload: '+datas.Error);return;} } catch (ex) { AlertMessage(ret, 700); return;}//parse json\n");
                //s.Append("for(var i=0;i<datas.length;i++) {\n");
                //s.Append("var data = datas[i];\n");
                //s.Append("$('#'+JQueryEscape('" + GetHtmlId(fieldName) + "')).attr('FileId',data.Id);\n");
                //s.Append("OnChange(document.getElementById('AppPress" + AppPress.IdSep + (int)FormDefFieldType.FileUpload + AppPress.IdSep + fieldValue.GetHtmlId() + "'),true);\n");
                //s.Append("}\n");
                //s.Append("}\n}\n");
                //s.Append(");");

                var param = "";
                var s = "";
                if (fieldValue.formField.FileUploadStorage == FileUploadStorageType.Directory)
                {
                    var directory = fieldValue.formField.FileUploadDirectory;
                    param += "&Directory=" + HttpUtility.UrlEncode(directory) + "";
                }

                if (fieldValue.formField.EncryptionType != null)
                    param += "&EncryptionType=" + (int)fieldValue.formField.EncryptionType;

                if (fieldValue.formField.MaxFileSizeInKB != null)
                    param += "&MaxFileSizeInKB=" + fieldValue.formField.MaxFileSizeInKB.Value;
                if (fieldValue.formField.ValidateOnServer)
                    param += "&AcceptFileTypes=" + fieldValue.formField.Accept;
                if (fieldValue.formField.NonSecure)
                    param += "&NonSecure=";
                var saveOnUpload = fieldValue.formField.SaveOnUpload && !fieldValue.FormData.IsNew;
                if (saveOnUpload)
                {
                    if (fieldValue.formField.formDef.TableName == null)
                        throw new Exception(fieldValue.GetFieldDescription() + " Containing Form should have TableName as SaveOnUpload is provided");
                    param += "&UpdateQuery=" + HttpUtility.UrlEncode("Update " + site.SQLQuote + fieldValue.formField.formDef.TableName + site.SQLQuote + " Set " + site.SQLQuote + fieldValue.formField.fieldName + site.SQLQuote + "=%%FileID%% Where " + fieldValue.formField.formDef.PrimaryKey + "=" + fieldValue.FormData.id);
                }

                var serverUrl = GetBaseUrl() + AppPress.GetDefaultAspx() + "?UploadFile=" + param + "" + (fieldValue.formField.DoNotSaveInDB && fieldValue.formField.OriginalType != (int)FormDefFieldType.MultiFileUpload ? "&DoNotSaveInDB=" : "");
                s += @"FileUploadUI('" + GetHtmlId(fieldName) + @"','" + serverUrl + "','" + HttpUtility.JavaScriptStringEncode(fieldValue.formField.Accept) + "'," + (fieldValue.formField.AutoUpload ? "true" : "false") + "," + AppPress.LocalInstanceId + "," + (saveOnUpload ? "true" : "false") + ");";
                //if (JsStr != null)
                //{
                //    JsStr.Append(s);
                //    s = "";
                //}





                ////string multiFileUpload = "";
                //if (fieldValue.formField.OriginalType == FormDefFieldType.MultiFileUpload)
                //    multiFileUpload = ""; // " multiple=\"multiple\"";
                //string uploadFormHtml =

                //"<Form method='post' enctype='multipart/form-data' name='Upload_" + fieldValue.GetHtmlId() +
                //"' id='Upload_" + fieldValue.GetHtmlId() +
                //"' action='" + GetBaseUrl() + Path.GetFileName(Request.Url.LocalPath) + "?UploadFile=" + param + "" + (fieldValue.formField.DoNotSaveInDB && fieldValue.formField.OriginalType != FormDefFieldType.MultiFileUpload ? "&DoNotSaveInDB=" : "") + "' target='upload_target" + AppPress.IdSep +
                //fieldValue.GetHtmlId() + "' > " +
                //"<input type='File' " + multiFileUpload + " name='FileUpload' FileId='" + GetFieldValue(fieldName) + "' id='" +
                //GetHtmlId(fieldName) + "'  onchange='SetDirty();document.getElementById(\"Upload_" + fieldValue.GetHtmlId() +
                //"\").submit();' " + (fieldValue.FormData.GetFieldValue(fieldName).ReadOnly == FieldReadonlyType.Readonly ? "disabled" : "") + "/>" +
                //"<iframe name='upload_target" + AppPress.IdSep + fieldValue.GetHtmlId() + "' id='upload_target" + AppPress.IdSep +
                //fieldValue.GetHtmlId() +
                //"' src='' style='width:0;height:0;border:0px solid #fff;display:none'></iframe><script>" + s.ToString() + "</script></form>";
                //;
                return s;
            }
            finally
            {
                fieldValue = pFieldValue;
            }
        }

        internal string GetHtmlContainerId(string fieldName)
        {
            var fieldValue1 = fieldValue.FormData.GetFieldValue(fieldName);
            if (fieldValue1 == null)
            {
                appPressResponse.Add(AppPressResponse.FormError(fieldValue.FormData, "Could not find Field: " + fieldName + " in Form: " + fieldValue.FormData.formDef.formName));
                return "";
            }
            return fieldValue1.GetHtmlContainerId();
        }
        internal string GetHtmlFormErrorId()
        {
            return "_FormError" + AppPress.IdSep + fieldValue.FormData.formDefId + AppPress.IdSep + fieldValue.FormData.id;
        }
        internal string GetHtmlFormErrorContainerId()
        {
            return "_FormErrorContainer" + AppPress.IdSep + fieldValue.FormData.formDefId + AppPress.IdSep + fieldValue.FormData.id;
        }
        internal string GetHtmlId(string fieldName)
        {
            var fieldValue1 = fieldValue.FormData.GetFieldValue(fieldName);
            if (fieldValue1 == null)
            {
                appPressResponse.Add(AppPressResponse.FormError(fieldValue.FormData, "Could not find Field: " + fieldName + " in Form: " + fieldValue.FormData.formDefId));
                return "";
            }
            return "AppPress" + AppPress.IdSep + (int)fieldValue1.formField.Type + AppPress.IdSep + fieldValue1._GetHtmlId();
        }

        internal string SetChecked(string fieldName)
        {
            var fieldValue1 = fieldValue.FormData.GetFieldValue(fieldName);
            if (fieldValue1 == null)
                appPressResponse.Add(AppPressResponse.FormError(fieldValue.FormData, "Could not find Field: " + fieldName + " in Form: " + fieldValue.FormData.formDefId));
            else if (fieldValue1.Value == "1")
            {
                JsStr.Append("$('#'+JQueryEscape('" + GetHtmlId(fieldValue1.formField.fieldName) + "')).attr('checked',true);\n");
            }
            return "";
        }

        internal string GetHtmlOnClick(string fieldName)
        {

            var fieldValue1 = fieldValue.FormData.GetFieldValue(fieldName);
            if (fieldValue1 == null)
            {
                throw new Exception("Could not find Field: " + fieldName + " in Form: " + fieldValue.FormData.formDef.formName);
            }
            return fieldValue1.GetHtmlOnClick(this, true);
        }
        internal string GetHtmlOnChange(string fieldName)
        {
            var f = fieldValue.FormData.GetFieldValue(fieldName);
            if (f == null)
                throw new Exception("Could not find field:" + fieldName + " in formDef:" + fieldValue.FormData.formDef.formName);
            return f.GetHtmlOnChange(this);
        }

        /// <summary>
        /// Encrypt the given string using DES algorithm.
        /// Encryption Key used is the EncryptionKey passed to InitAppPress function
        /// </summary>
        /// <param name="strText"></param>
        /// <returns></returns>
        public static string EncryptTextDES(string strText, string newEncryptionKey = null)
        {
            return Util.EncryptDES(strText, newEncryptionKey);
        }

        /// <summary>
        /// Decrypt the given encrypted string using DES algorithm.
        /// Encryption Key used is the EncryptionKey passed to InitAppPress function
        /// In Debug Mode if error in Decryption returns 9999
        /// </summary>
        /// <param name="strText"></param>
        /// <returns></returns>
        public static string DecryptTextDES(string encryptvalue, string newEncryptionKey = null)
        {
            try
            {
                return Util.DecryptDES(encryptvalue, newEncryptionKey);
            }
            catch
            {
                if (AppPress.Settings.developer)
                    return "9999";
                throw;
            }
        }
        /// <summary>
        /// Encrypt the given string using AES algorithm.
        /// Encryption Key used is the EncryptionKey passed to InitAppPress function
        /// </summary>
        /// <param name="value"></param>
        /// <param name="newEncryptionKey"></param>
        /// <returns></returns>
        public static string EncryptTextAES(string value, string newEncryptionKey = null)
        {
            if (AppPress.Settings.encryptionKey == null && newEncryptionKey == null)
                throw new Exception("Cannot EncryptTextAES as EncryptionKey for AES was not passed to InitAppPress");
            return value != null ? APCrypto.EncryptStringAES(value, newEncryptionKey) : value;
        }
        /// <summary>
        /// Decrypt the given Encrypted string using AES algorithm.
        /// Encryption Key used is the EncryptionKey passed to InitAppPress function
        /// In Debug Mode if error in Decryption returns 9999
        /// </summary>
        /// <param name="encryptvalue"></param>
        /// <param name="newEncryptionKey"></param>
        /// <returns></returns>
        public static string DecryptTextAES(string encryptvalue, string newEncryptionKey = null)
        {
            try
            {
                if (AppPress.Settings.encryptionKey == null && newEncryptionKey == null)
                    throw new Exception("Cannot DecryptTextAES as EncryptionKey for AES was not passed to InitAppPress");
                return encryptvalue != null ? APCrypto.DecryptStringAES(encryptvalue, newEncryptionKey) : encryptvalue;
            }
            catch (Exception)
            {
                if (AppPress.Settings.developer)
                    return "9999";
                else
                    throw;
            }
        }
        internal string GetHtmlOptions(string fieldName)
        {
            var fieldValue1 = fieldValue.FormData.GetFieldValue(fieldName);
            var f = fieldValue.FormData.GetFieldValue(fieldName);
            if (f == null)
                throw new Exception("Could not find field:" + fieldName + " in formDef:" + fieldValue.FormData.formDefId);
            switch (f.formField.Type)
            {
                case FormDefFieldType.Pickone:

                    string ostr = string.Empty;
                    if (f.formField.Style == FormDefFieldStyle.AutoComplete)
                        return ostr;
                    if (!f.formField.Static)
                    {
                        var options = ((PickFieldValue)f).GetOptions(this);
                        foreach (var option in options)
                        {
                            var id = option.id;
                            var display = option.value;
                            if (f.formField.Style == FormDefFieldStyle.Radio)
                            {
                                ostr += "<div class='pickoneRadio'><input type='radio' id='" + GetHtmlId(f.formField.fieldName) + AppPress.IdSep + id + "' name='" + GetHtmlId(f.formField.fieldName) + "'";
                                if (!f.Value.IsNullOrEmpty() && f.Value == id) // Value can be null from GetFieldDataAndHTML and if paging is hidden
                                    ostr += " checked ";
                                if (f.ReadOnly == FieldReadonlyType.Readonly)
                                    ostr += " disabled ";
                                ostr += "onclick='" + GetHtmlOnChange(f.formField.fieldName) + "'";
                                ostr += " Value='" + id + "'/><label style='float: right;margin-left: 4px;margin-top: 1px;' for='" + GetHtmlId(f.formField.fieldName) + AppPress.IdSep + id + "'>" + display + "</label></div>";
                            }
                            else if (f.formField.Style == FormDefFieldStyle.DropDown)
                            {
                                ostr += "<option value='" + id + "'";
                                if (!f.Value.IsNullOrEmpty() && f.Value == id) // Value can be null from GetFieldDataAndHTML and if paging is hidden
                                    ostr += " selected";
                                ostr += ">" + display + "</option>";
                            }

                        }
                    }
                    return ostr;
                default:
                    throw new Exception("Cannot GetHtmlOptions for field which is of Type: " + f.formField.Type);
            }
        }

        internal string GetHtmlDatePopup(string fieldName)
        {
            var f = fieldValue.FormData.GetFieldValue(fieldName);
            if (f == null)
                throw new Exception("Could not find field:" + fieldName + " in formDef:" + fieldValue.FormData.formDefId);
            switch (f.formField.Type)
            {
                case FormDefFieldType.DateTime:
                    var hid = "AppPress" + AppPress.IdSep + (int)f.formField.Type + AppPress.IdSep + f._GetHtmlId();
                    StringBuilder str = new StringBuilder();
                    if (f.ReadOnly == FieldReadonlyType.None)
                    {
                        var dateFormat = AppPress.Settings.JQueryDateFormat;
                        if (remoteLoginUserId != null)
                            dateFormat = remoteData.JQueryDateFormat;
                        var formFields = fieldValue.FormData.formDef.formFields;
                        var i = formFields.FindIndex(t => t == f.formField);
                        string hid1 = null, hid2 = null;
                        if (f.formField.IsDateRange != 0 && i > 0 && formFields[i - 1].IsDateRange == 1)
                        {
                            hid2 = hid;
                            var f1 = fieldValue.FormData.GetFieldValue(formFields[i - 1].id);
                            hid1 = "AppPress" + AppPress.IdSep + (int)f1.formField.Type + AppPress.IdSep + f1._GetHtmlId();
                        }

                        if (f.formField.Style != FormDefFieldStyle.Time)
                        {
                            if (f.formField.Style == FormDefFieldStyle.Month)
                                dateFormat = Settings.JQueryDateMonthFormat;

                            //str.Append(@"$('#'+JQueryEscape('" + hid + @"')).timepicker({showOn: 'focus',inline: true,changeMonth: true,timeFormat: 'HH:mm',yearRange: '-100:+5',changeYear: true");
                            str.Append(@"$('#'+JQueryEscape('" + hid + @"')).datepicker({showOn: 'focus',inline: true,changeMonth: true,dateFormat: '" + dateFormat + @"',yearRange: '1900:2050',changeYear: true");
                            if (f.formField.Style == FormDefFieldStyle.Month)
                                str.Append(",showButtonPanel: true");
                            var dateTimeFieldValue = (DateTimeFieldValue)f;
                            if (dateTimeFieldValue.MinDateTime != null)
                                str.Append(",minDate:new Date(" + dateTimeFieldValue.MinDateTime.Value.Year + "," + (dateTimeFieldValue.MinDateTime.Value.Month - 1) + "," + dateTimeFieldValue.MinDateTime.Value.Day + ")");
                            if (dateTimeFieldValue.MaxDateTime != null)
                                str.Append(",maxDate:new Date(" + dateTimeFieldValue.MaxDateTime.Value.Year + "," + (dateTimeFieldValue.MaxDateTime.Value.Month - 1) + "," + dateTimeFieldValue.MaxDateTime.Value.Day + ")");
                            if (f.formField.IsDateRange != 0 && hid1 != null)
                                str.Append(@",beforeShow: function (date, inst) {return {minDate:$.datepicker.parseDate('" + dateFormat + @"', $('#'+JQueryEscape('" + hid1 + @"')).val())}}");
                            str.Append(@"})");
                            if (f.formField.Style == FormDefFieldStyle.Month)
                                str.Append(@".focus(function() {
                                                                var thisCalendar = $(this);
		                                $('.ui-datepicker-calendar').detach();
		                                $('.ui-datepicker-close').click(function() {
                                                                    var month = $('#ui-datepicker-div .ui-datepicker-month :selected').val();
                                                                    var year = $('#ui-datepicker-div .ui-datepicker-year :selected').val();
                                                                    thisCalendar.datepicker('setDate', new Date(year, month, 1));
                                                                    thisCalendar.trigger('change')
                                                                });
                                                            });");
                            else
                                str.Append(";");
                        }
                    }
                    JsStr.Append(str);
                    return "";

                default:
                    throw new Exception("Cannot GetHtmlDatePopup for field which is of Type: " + f.formField.Type);
            }
        }
        internal int GetFieldInt(string fieldName, int defaultValue)
        {
            FieldValue f = fieldValue.FormData.GetFieldValue(fieldName);
            if (f == null)
                throw new Exception("Could not find field:" + fieldName + " in formDef:" + fieldValue.FormData.formDefId);
            if (f.Value == null)
                return defaultValue;
            return int.Parse(f.Value);
        }
        internal int GetFieldInt(string fieldName)
        {
            var f = fieldValue.FormData.GetFieldValue(fieldName);
            if (f == null)
                throw new Exception("Could not find Field: " + fieldName + " in formDef: " + fieldValue.FormData.formDefId);
            if (f.Value == null)
                throw new Exception("Could not find field value for Field: " + fieldName + " in formDef: " + fieldValue.FormData.formDefId);
            return int.Parse(f.Value);
        }
        internal decimal GetFieldDecimal(string fieldName, int decimals)
        {
            var f = fieldValue.FormData.GetFieldValue(fieldName);
            if (f == null)
                throw new Exception("Could not find Field: " + fieldName + " in formDef: " + fieldValue.FormData.formDefId);
            if (f.Value == null)
                throw new Exception("Could not find field value for Field: " + fieldName + " in formDef: " + fieldValue.FormData.formDefId);
            return Math.Round(decimal.Parse(f.Value), decimals);
        }
        internal string GetFieldString(string fieldName)
        {
            var f = fieldValue.FormData.GetFieldValue(fieldName);
            if (f == null)
                throw new Exception("Could not find Field: " + fieldName + " in formDef: " + fieldValue.FormData.formDefId);
            if (f.Value == null)
                return "";
            return f.Value;
        }
        /// <summary>
        /// Redirects to a new Tab or Window
        /// </summary>
        /// <param name="Url"></param>
        /// <param name="redirectTarget">Target of redirect. same as html a tag target. If null then redirects to same window</param>
        public void RedirectToUrl(string Url, string redirectTarget)
        {
            var clientAction = new AppPressResponse();
            clientAction.url = Url;
            clientAction.appPressResponseType = AppPressResponseType.Redirect;
            if (redirectTarget != null)
                clientAction.redirectParams = new RedirectParams { target = redirectTarget };
            appPressResponse.Add(clientAction);
        }
        /// <summary>
        /// For Internal use. Use FormDefNameClass.Redirect
        /// </summary>
        /// <param name="formDefId"></param>
        /// <param name="formId"></param>
        /// <param name="redirectParams"></param>
        public virtual void Redirect(long formDefId, string formId, RedirectParams redirectParams)
        {
            var formDef = FindFormDef(formDefId);
            if (formDef == null)
                throw new Exception("Could not Find FormDef: " + formDefId);
            formDef.Redirect(this, formId, redirectParams);
        }

        internal void RefreshContainer(FieldValue callerFieldValue)
        {
            if (callerFieldValue.formField.containerFormField == null)
            {
                // Popup called from button on a rowFormData
                var fieldValue = callerFieldValue.FormData.containerFieldValue;
                if (fieldValue != null)
                    appPressResponse.Add(AppPressResponse.RefreshField(this, fieldValue, true));

            }
            else
            {
                // Popup called from FormContainer Field. Refresh all Callers
                if (callerFieldValue != null && callerFieldValue.formField.containerFormField != null)
                {
                    var fieldValue = callerFieldValue.FormData.GetFieldValue(callerFieldValue.formField.containerFormField.fieldName);
                    appPressResponse.Add(AppPressResponse.RefreshField(this, fieldValue, true));
                }
            }

        }
        /// <summary>
        /// ???
        /// </summary>
        /// <param name="instanceId"></param>
        /// <param name="formName"></param>
        /// <param name="formDataId"></param>
        /// <param name="popupParams"></param>
        public void RemotePopup(int instanceId, string formName, string formDataId, PopupParams popupParams)
        {
            if (instanceId == AppPress.LocalInstanceId)
            {
                Popup(formName, formDataId, popupParams);
                return;
            }
            if (popupParams == null)
                popupParams = new PopupParams();
            popupParams.remoteData = new RemoteData();
            popupParams.remoteData.JQueryDateFormat = AppPress.Settings.JQueryDateFormat;
            popupParams.remoteData.NetDateFormat = AppPress.Settings.NetDateFormat;
            popupParams.remoteData.NetDateTimeFormat = AppPress.Settings.NetDateTimeFormat;

            var instance = AppPress.Settings.Instances.Find(t => t.InstanceId == instanceId);
            if (instance == null)
                throw new Exception("In RemotePopup could not find Instance Name: " + instanceId);
            int Instance = AppPress.Settings.Instances.FindIndex(t => t.InstanceId == instanceId);
            var remoteUrl = instance.InstanceBaseUrl + "?RemoteForm=&Popup=&FormName=" + formName;
            if (formDataId != null)
                remoteUrl += "&FormDataId=" + formDataId;
            if (popupParams != null)
                remoteUrl += "&PopupParams=" + HttpUtility.UrlEncode(FormDef.Serialize(popupParams, typeof(PopupParams))) + "";
            remoteUrl += "&RemoteLoginUserId=" + this.sessionData.loginUserId + "&RemoteInstanceId=" + AppPress.LocalInstanceId;
            var pairs = new NameValueCollection();
            WebClient client = new WebClient();
            byte[] response = client.UploadValues(new Uri(remoteUrl), pairs);

            var remoteA = (AppPress)FormDef.Deserialize(System.Text.Encoding.UTF8.GetString(response), AppPress.Settings.ApplicationAppPress);
            DependentFields = remoteA.DependentFields;
            appPressResponse.AddRange(remoteA.appPressResponse);
            var popupResponse = remoteA.appPressResponse.Find(t => t.appPressResponseType == AppPressResponseType.Popup);
            if (popupResponse != null)
            {
                foreach (var remoteFormDef in remoteA.remoteFormDefs)
                {
                    AppPress.formDefs.RemoveAll(t => t.id == remoteFormDef.id);
                    remoteFormDef.MasterFormName = null;
                    Util.InitializeFormDef(remoteA, remoteFormDef, remoteA.remoteFormDefs);
                    AppPress.formDefs.Add(remoteFormDef);
                }
                foreach (var secureUrl in remoteA.remoteData.SecureUrls)
                {
                    this.sessionData.AddSecureUrl(secureUrl);
                }
                pageStackCount++;
                PopupDatas[pageStackCount].InstanceId = instanceId;
                remoteA.fieldValue = null;
                remoteA.SetFormDataFieldValue();


                formDatas.AddRange(remoteA.formDatas);
                PopupDatas[pageStackCount].callerFormDefId = fieldValue.FormData.formDef.id;
                PopupDatas[pageStackCount].callerFormDataId = fieldValue.FormData.id;
                PopupDatas[pageStackCount].callerFieldDefId = fieldValue.formField.id;
                popupResponse.popupWidth = popupParams.PopupWidth;
                if (popupParams.PopupHeight != null)
                    popupResponse.popupHeight = popupParams.PopupHeight.Value.ToString();
            }
        }
        /// <summary>
        /// ???
        /// </summary>
        /// <param name="instanceId"></param>
        /// <param name="formName"></param>
        /// <param name="formDataId"></param>
        /// <param name="popupParams"></param>
        public void RemoteRedirect(int instanceId, string formName, string formDataId)
        {
            if (instanceId == AppPress.LocalInstanceId)
            {
                Redirect(AppPress.FindFormDef(formName).id, formDataId, null);
                return;
            }
            var instance = AppPress.Settings.Instances.Find(t => t.InstanceId == instanceId);
            if (instance == null)
                throw new Exception("In RemoteRedirect could not find InstanceId: " + instanceId);
            int InstanceId = AppPress.Settings.Instances.FindIndex(t => t.InstanceId == instanceId);
            var remoteUrl = instance.InstanceBaseUrl + "?RemoteForm=&Redirect=&FormName=" + formName;
            if (formDataId != null)
                remoteUrl += "&FormDataId=" + formDataId;

            var popupParams = new PopupParams();
            popupParams.remoteData = new RemoteData();
            popupParams.remoteData.JQueryDateFormat = AppPress.Settings.JQueryDateFormat;
            popupParams.remoteData.NetDateFormat = AppPress.Settings.NetDateFormat;
            popupParams.remoteData.NetDateTimeFormat = AppPress.Settings.NetDateTimeFormat;

            if (popupParams != null)
                remoteUrl += "&PopupParams=" + HttpUtility.UrlEncode(FormDef.Serialize(popupParams, typeof(PopupParams))) + "";
            remoteUrl += "&RemoteLoginUserId=" + this.sessionData.loginUserId + "&RemoteInstanceId=" + AppPress.LocalInstanceId;
            var pairs = new NameValueCollection();

            WebClient client = new WebClient();

            byte[] response = client.UploadValues(new Uri(remoteUrl), pairs);

            var remoteA = (AppPress)FormDef.Deserialize(System.Text.Encoding.UTF8.GetString(response), AppPress.Settings.ApplicationAppPress);
            appPressResponse.AddRange(remoteA.appPressResponse);
            var popupResponse = remoteA.appPressResponse.Find(t => t.appPressResponseType == AppPressResponseType.Popup);
            if (popupResponse != null)
            {
                appPressResponse.Remove(popupResponse);
                foreach (var remoteFormDef in remoteA.remoteFormDefs)
                {
                    AppPress.formDefs.RemoveAll(t => t.id == remoteFormDef.id);
                    remoteFormDef.MasterFormName = null;
                    Util.InitializeFormDef(remoteA, remoteFormDef, remoteA.remoteFormDefs);
                    AppPress.formDefs.Add(remoteFormDef);
                }

                if (pageStackCount != 0)
                    throw new Exception("PageStackCount should be 0");
                //PopupDatas[pageStackCount].InstanceId = instanceId;


                remoteA.fieldValue = null;
                remoteA.SetFormDataFieldValue();
                formDatas.AddRange(remoteA.formDatas);

                var html = popupResponse.fieldHtml;
                var ar = new AppPressResponse();
                ar.appPressResponseType = AppPressResponseType.RefreshField;

                var rootFormDatas = remoteA.formDatas.FindAll(t => t.formDef.formName == formName);
                if (rootFormDatas.Count() != 1)
                    throw new Exception("In RemotePopup Found 0 or more than 1 Forms: " + formName);

                var masterFormData = formDatas.Find(t => t.formDef.FormType == FormType.MasterForm);
                var contentFieldValue = masterFormData.fieldValues.Find(t => t.formField.fieldName == "MasterContentArea");
                rootFormDatas[0].containerFieldValue = contentFieldValue;

                ar.formDefId = masterFormData.formDef.id;
                ar.id = masterFormData.id;
                ar.fieldDefId = contentFieldValue.formField.id;

                ar.fieldHtml = Util.RemoveScripts(this, html);
                ar.JsStr = JsStr.ToString() + popupResponse.JsStr;
                appPressResponse.Add(ar);
                MasterContentAreaFormName = formName;
                MasterContentAreaInstanceId = instanceId;
            }
        }
        /// <summary>
        /// For Internal use. Use FormDefNameClass.Popup
        /// </summary>
        /// <param name="formDefId"></param>
        /// <param name="formDataId"></param>
        /// <param name="popupParams"></param>
        public void Popup(long formDefId, string formDataId, PopupParams popupParams)
        {
            var formDef = FindFormDef(formDefId);
            if (formDef == null)
                throw new Exception("Could not Find FormDef: " + formDefId);
            formDef.Popup(this, formDataId, popupParams);
        }
        public void Popup(string formName, string formDataId, PopupParams popupParams)
        {
            var formDef = FindSingleFormDef(formName);
            formDef.Popup(this, formDataId, popupParams);
        }
        internal FieldValue GetContainerFieldValue()
        {
            return fieldValue.FormData.GetFieldValue(fieldValue.formField.containerFormField.fieldName);
        }
        internal int GetRowNumber()
        {
            return ((FormContainerFieldValue)fieldValue).rowNumber;
        }
        internal string GetSelectedFormIds(string fieldName, string defValue)
        {
            var f = fieldValue.FormData.GetFieldValue(fieldName);
            if (f == null)
                throw new Exception("Could not find field:" + fieldName + " in Form: " + fieldValue.FormData.formDefId);
            switch (f.formField.Type)
            {
                case FormDefFieldType.FormContainerDynamic:
                    var r = string.Join(",", formDatas.FindAll(t => t.containerFieldValue == f && t.GetFieldInt("SelectRow") == 1).Select(t => t.id.ToString()));
                    if (r.IsNullOrEmpty())
                        r = defValue;
                    return r;
                default:
                    throw new Exception("in Form: " + fieldValue.FormData.formDefId + " GetSelectedFormIds can be called only for Field of type FormContainer");
            }
        }
        internal string GetAllSelectedFormIds(string fieldName, string defValue)
        {
            var f = fieldValue.FormData.GetFieldValue(fieldName);
            if (f == null)
                throw new Exception("Could not find field:" + fieldName + " in Form: " + fieldValue.FormData.formDefId);
            switch (f.formField.Type)
            {
                case FormDefFieldType.FormContainerDynamic:
                    var r = string.Join(",", formDatas.FindAll(t => t.containerFieldValue == f).Select(t => t.fieldValues[0].Value));
                    if (r.IsNullOrEmpty())
                        r = defValue;
                    return r;
                default:
                    throw new Exception("in Form: " + fieldValue.FormData.formDefId + " GetSelectedFormIds can be called only for Field of type FormContainer");
            }
        }
        internal string GetFieldValue(string fieldName)
        {
            var f = fieldValue.FormData.GetFieldValue(fieldName);
            if (f == null)
                return ""; // static fields are not submitted
            return f.GetFieldValue(this);
        }
        internal class HiResDateTime
        {
            private static long lastTimeStamp = DateTime.UtcNow.Ticks;
            public static long UtcNowTicks
            {
                get
                {
                    long original, newValue;
                    do
                    {
                        original = lastTimeStamp;
                        long now = DateTime.UtcNow.Ticks;
                        newValue = Math.Max(now, original + 1);
                    } while (Interlocked.CompareExchange
                                 (ref lastTimeStamp, newValue, original) != original);

                    return newValue;
                }
            }
        }
        internal string GetBottomScript()
        {
            var s = "<script type=\"text/javascript\">\nvar pageTimeStamp=" + HiResDateTime.UtcNowTicks + ";\n$(document).ready(function() {";
            s += Regex.Replace(JsStr.ToString(), @"<!--\|\w+\|-->", "", RegexOptions.Singleline) + "\nAppPressReady();SetFieldReadonly(a);\nExecuteAppPressResponse(a," + AppPress.LocalInstanceId + ");});</script>\n";
            return s;
        }

        internal string GetHtmlHeader()
        {
            var baseUrl = GetBaseUrl();
            var s =
              @"
               <script src='" + baseUrl + Path.GetFileName(Request.Url.LocalPath) + @"?getJs=&filetime=" + startTime.ToString() + @"'></script>";

            s += HeaderSignature + "\n" + // this signature is replaced with scripts generated in body generation
                "<script type=\"text/javascript\">\n" +
                    "var rootFormName = '" + fieldValue.FormData.formDefId + "';\n" +
                    "var rootFormId = " + fieldValue.FormData.id + ";\n" +
                    "var defaultInstanceId = " + Settings.Instances.Find(t => t.LocalInstance).InstanceId + ";\n";

            s += "function GetBaseUrl(instanceId) { return '" + baseUrl + AppPress.GetDefaultAspx() + "';}\n";

            foreach (var formData in formDatas)
            {
                if (formData.formDef.TableName != null && formData.formDef.FormType != FormType.ContainerRowFormGenerated && formData.formDef.GenerationType != 1)
                {
                    s += "SetWarnOnDirtyClose();\n";
                    break;
                }

            }
            s += "\nvar formId=" + fieldValue.FormData.id + ";var a = Evaluate(unescape('" + Serialize().ToEscapeString() + "'));\n</script>";
            return s;
        }
        /// <summary>
        /// Executes the script on browser. Script is sent to browser as response to the current Ajax Call. OnClick etc.
        /// </summary>
        /// <param name="JSScript">Script to execute. Do not include <Script> tag as part of script</Script></param>
        public void ExecuteJSScript(string JSScript)
        {
            appPressResponse.Add(AppPressResponse.ExecuteJSScript(JSScript));
        }
        /// <summary>
        /// Marks the Page in UI as dirty. If user tries to navigate away from the page he will be shown a warning.
        /// This is normally managed by AppPress. 
        /// </summary>
        /// <param name="dirty"></param>
        public void SetPageDirty(bool dirty)
        {
            appPressResponse.Add(AppPressResponse.SetPageDirty(dirty));
        }
        internal string Serialize()
        {
            if (DoNotSerialize)
                if (appPressResponse.Count() == 0 || appPressResponse.Find(t => t.appPressResponseType != AppPressResponseType.Redirect) != null)
                    throw new Exception(this.fieldValue.GetFieldDescription() + " has NoSubmit property. In Click logic of this should only result in Redirect as no other data is submited.");

            // Need to Serialize the Base class FormData
            var formDatas1 = new List<FormData>();
            var pFormDatas = formDatas;
            var saveDependentFields = DependentFields;
            formFields = new List<FormFieldJS>();
            var saveFieldValues = new List<List<FieldValue>>();
            if (formDatas != null)
                for (int i = 0; i < formDatas.Count(); ++i)
                {
                    var formData = formDatas[i];
                    saveFieldValues.Add(formData.fieldValues);
                    if (formData.IsDeleted && formData.IsNew)
                        continue;

                    var fieldValues = formData.SerializableFields(this);
                    if (fieldValues.Count() == 0)
                        if (formData.formDef.formFields.Find(t => t.Type == FormDefFieldType.FormContainerDynamic || t.Type == FormDefFieldType.Button) == null)
                            continue;
                    formData.fieldValues = fieldValues;
                    formDatas1.Add(formData.CovertToFormDefClass());
                }
            try
            {
                var dFields = new Dictionary<string, List<string>>();
                if (formDatas != null)
                {
                    foreach (var dependentField in DependentFields)
                    {
                        var Value = new List<string>();
                        foreach (var s in dependentField.Value)
                        {
                            var ss = s.Split(AppPress.IdSep.ToCharArray());
                            var formData = formDatas.Find(t => t.formDefId == long.Parse(ss[2]) && t.id == ss[4]);
                            if (formData != null) // Popup may create dependent field. After close of popup the field no longer exists. TDB create Dependent Field for Each PageStack
                            {
                                var fieldValue1 = formData.GetFieldValue(long.Parse(ss[3]));
                                if (fieldValue1 != null && fieldValue1.Hidden == FieldHiddenType.None)
                                    Value.Add(s);
                            }
                        }
                        if (Value.Count != 0)
                            dFields.Add(dependentField.Key, Value);
                    }
                    formDatas = formDatas1;
                }
                DependentFields = dFields;
                var pFieldValue = this.fieldValue;
                if (GetPDFParams != null)
                {

                    Dictionary<string, string> postParams = null;
                    RedirectParams redirectParams = null;
                    GetPDFParams = HttpUtility.UrlEncode(GetPDFParams.Substring(0, GetPDFParams.Length - QuerySeperator.Length));
                    if (GetPDFParams.Length > 200)
                    { // IE has a issue with filedownload with PostPrams. Let it work for single files
                        postParams = new Dictionary<string, string>();
                        postParams.Add("GetPDFParams", GetPDFParams);
                        redirectParams = new RedirectParams { postParams = postParams };
                    }
                    var clientAction = new AppPressResponse();

                    clientAction.url = GetBaseUrl() + Path.GetFileName(Request.Url.LocalPath) + "?GetPDF=&GetPDFParams=" + HttpUtility.UrlEncode(GetPDFParams) + "&t=" + DateTime.UtcNow.Ticks;
                    sessionData.AddSecureUrl(clientAction.url);

                    clientAction.redirectParams = redirectParams;
                    clientAction.appPressResponseType = AppPressResponseType.Redirect;

                    appPressResponse.Add(clientAction);
                }
                else if (GetDOCXParams != null)
                {

                    RedirectParams redirectParams = null;
                    GetDOCXParams = HttpUtility.UrlEncode(GetDOCXParams.Substring(0, GetDOCXParams.Length - QuerySeperator.Length));


                    var clientAction = new AppPressResponse();

                    clientAction.url = GetBaseUrl() + Path.GetFileName(Request.Url.LocalPath) + "?GetDOCX=&GetDOCXParams=" + HttpUtility.UrlEncode(GetDOCXParams);
                    sessionData.AddSecureUrl(clientAction.url);

                    clientAction.redirectParams = redirectParams;
                    clientAction.appPressResponseType = AppPressResponseType.Redirect;

                    appPressResponse.Add(clientAction);
                }
                try
                {
                    // move all ExecuteJSScript to End
                    var ejs = appPressResponse.FindAll(t => t != null && t.appPressResponseType == AppPressResponseType.ExecuteJSScript);
                    appPressResponse.RemoveAll(t => t == null || t.appPressResponseType == AppPressResponseType.ExecuteJSScript);
                    appPressResponse.AddRange(ejs);
                    this.fieldValue = null;
                    return FormDef.Serialize(this, AppPress.Settings.ApplicationAppPress);
                }
                finally
                {
                    this.fieldValue = pFieldValue;
                }

            }
            finally
            {
                formDatas = pFormDatas;
                if (formDatas != null)
                {
                    int i = 0;
                    foreach (var formData in formDatas)
                        formData.fieldValues = saveFieldValues[i++];
                }
                DependentFields = saveDependentFields;
            }
        }
        internal void SetFormDataFieldValue()
        {
            var pt = this;
            var formDatas1 = new List<FormData>();
            for (int i = 0; i < pt.formDatas.Count; ++i)
            {
                if (pt.formDatas[i] != null)
                {
                    var formData2 = pt.formDatas[i];
                    formData2.formDef = AppPress.FindFormDef(formData2.formDefId);
                    if (formData2.formDef == null)
                        throw new Exception("Could not Find Form: " + formData2.formDefId);
                    formData2.a = this;
                    foreach (var formField in formData2.formDef.formFields.FindAll(t => t.Type == FormDefFieldType.FormContainerDynamic || t.Type == FormDefFieldType.Button))
                    {
                        if (formData2.fieldValues.Find(t => t.fieldDefId == formField.id) == null)
                        {
                            var fieldValue = formField.NewFieldValue(formData2.formDef);
                            fieldValue.fieldDefId = formField.id;
                            fieldValue.FormData = formData2;
                            fieldValue.NotFromClient = true;
                            formData2.fieldValues.Add(fieldValue);
                        }
                    }

                    foreach (var fieldValue in formData2.fieldValues)
                    {
                        fieldValue.FormData = formData2;
                        fieldValue.formField = formData2.formDef.GetFormField(fieldValue.fieldDefId);
                        if (pt.fieldValue != null) // From Remote Popup
                            if (fieldValue.fieldDefId == pt.fieldValue.fieldDefId && pt.formDataId == formData2.id)
                                pt.fieldValue = pt.originalFieldValue = fieldValue;
                    }
                    formData2.IsSubmitted = true;
                    formDatas1.Add(formData2);
                }
            }
            pt.formDatas = formDatas1;
            if (pt.fieldValue != null && pt.fieldValue.formField == null)
            {
                // form Data not submitted from client. Recreate it
                FormDef ptFormDef = null;
                FormField ptFormField = null;
                foreach (var formDef in AppPress.formDefs)
                {
                    foreach (var formField in formDef.formFields)
                        if (formField.id == pt.fieldValue.fieldDefId)
                        {
                            ptFormDef = formDef;
                            ptFormField = formField;
                            break;
                        }
                    if (ptFormDef != null)
                        break;
                }
                var fieldValueFormData = FormData.NewFormData(pt, ptFormField.formDef.id);
                pt.formDatas.Add(fieldValueFormData);
                var className = ptFormField.GetClassName();
                pt.fieldValue = CreateFieldValue(className);
                pt.fieldValue.NotFromClient = true;
                pt.fieldValue.formField = ptFormField;
                pt.fieldValue.fieldDefId = ptFormField.id;
                pt.fieldValue.FormData = fieldValueFormData;
            }
            foreach (var formData in pt.formDatas)
                formData.SetContainerFormData(pt);
        }

        internal static FieldValue CreateFieldValue(string className)
        {
            var fieldValue = (FieldValue)Util.CreateInstance(className);
            if (fieldValue == null)
                fieldValue = new FieldValue();
            return fieldValue;
        }

        public bool InTransaction()
        {
            return inTransaction;
        }
        /// <summary>
        /// Starts a Database Transaction.
        /// </summary>
        public void BeginTrans()
        {
            if (inTransaction)
                throw new Exception("Cannot call BeginTrans while in Transaction");
            IsolationLevel isolationLevel = IsolationLevel.RepeatableRead;
            foreach (var formData in formDatas)
            {
                formData.originalId = formData.id;
            }
            site.BeginTrans(isolationLevel);
            inTransaction = true;
            lastApplicationAuditId = 0;
            var obj = site.ExecuteInt("Select Max(Id) From Application_Audit");
            if (obj != null)
                lastApplicationAuditId = (long)obj;
            ClearErrors();
        }

        /// <summary>
        /// Commits a Database Transaction
        /// </summary>
        public void CommitTrans()
        {
            if (!inTransaction)
                throw new Exception("Cannot call CommitTrans while not in Transaction");
            if (site.trans == null)
                throw new Exception("Commit Called without BeginTrans");
            if (BeforeCommit != null)
                Util.InvokeMethod(this, BeforeCommit, new object[] { this });
            site.Commit();
            inTransaction = false;
            appPressResponse.Add(AppPressResponse.SetPageNonDirty());
        }
        /// <summary>
        /// Rolls back the Database Transaction. Additionally rolls back any changes in Id of FormData
        /// </summary>
        public void RollbackTrans()
        {
            if (!inTransaction)
                throw new Exception("Cannot call RollbackTrans while not in Transaction");
            //if (!this.StopExecution && site.trans == null)
            //    throw new Exception("Rollback Called without BeginTrans");
            appPressResponse.RemoveAll(t => t.appPressResponseType == AppPressResponseType.ChangeFormDataId);
            foreach (var formData in formDatas)
            {
                for (var i = 0; i < appPressResponse.Count; ++i)
                {
                    var ar = appPressResponse[i];
                    if (ar.formDefId == formData.formDef.id)
                        if (formData.originalId != formData.id && ar.id == formData.id)
                        {
                            appPressResponse.RemoveAt(i);
                            i--;
                        }
                }
                var oid = formData.id;
                formData.id = formData.originalId;
                if (formData.IsNew)
                    foreach (var fieldValue in formData.fieldValues.FindAll(t => t.formField.Type == FormDefFieldType.FormContainerDynamic))
                        foreach (var cFormData in formDatas)
                            if (cFormData.containerFieldValue == fieldValue)
                                foreach (var containerIdFieldValue in cFormData.fieldValues.FindAll(t => t.formField.Type == FormDefFieldType.ForeignKey))
                                    if (containerIdFieldValue.Value == oid)
                                        containerIdFieldValue.Value = formData.id;
            }
            site.RollBack();
            inTransaction = false;
            //appPressResponse.RemoveAll(t => t.appPressResponseType == AppPressResponseType.RefreshField);
            if (appPressResponse.Find(t => t.appPressResponseType == AppPressResponseType.FieldError || t.appPressResponseType == AppPressResponseType.FormError) != null)
                appPressResponse.Add(AppPressResponse.AlertMessage("There are errors on the Page. Please review and correct the errors shown in red and click on Save again."));
        }
        /// <summary>
        /// Refresh the current Page in Browser. Same as user clicking on Refresh button in Browser
        /// </summary>
        public void PageRefresh()
        {
            appPressResponse.RemoveAll(t => t.appPressResponseType != AppPressResponseType.AlertMessage);
            appPressResponse.Add(AppPressResponse.SetPageNonDirty());
            appPressResponse.Add(AppPressResponse.PageRefresh());

        }
        /// <summary>
        /// Clears all Errors shown on Page.
        /// </summary>
        public void ClearErrors()
        {
            if (appPressResponse.Find(t => t.appPressResponseType == AppPressResponseType.ClearErrors) == null)
                appPressResponse.Insert(0, AppPressResponse.ClearErrors());
        }

        /// <summary>
        /// Clears Error for the field on the page
        /// </summary>
        public void ClearErrors(FieldValue f)
        {
            appPressResponse.Insert(0, AppPressResponse.ClearErrors(f));
        }
        /// <summary>
        /// Add email in queue in database.
        /// </summary>
        /// <param name="sitedb">Connection of Database</param>
        /// <param name="mailTo">multiple email to should be split by comma ','</param>
        /// <param name="fromEmail"></param>
        /// <param name="subject"></param>
        /// <param name="content"></param>
        /// <param name="cc">multiple email to should be split by comma ','</param>
        /// <param name="bcc">multiple email to should be split by comma ','</param>
        /// <param name="isHtml"></param>
        /// <param name="fileName"></param>
        /// <param name="uploadFileIds"></param>
        public static void SendEmail(DAOBasic sitedb, string mailTo, string fromEmail, string subject, string content, string cc, string bcc, bool isHtml, string fileName = null, List<Int64> uploadFileIds = null)
        {
            if (AppPress.Settings.Smtp == null)
                throw new Exception("Please provide SmtpSection in Settings in AppInit.");

            if (mailTo.IsNullOrEmpty())
                throw new Exception("Internal Error: MailTo in QueueEmail should not be blank");

            if (AppPress.Settings.useDebugEmail)
            {
                if (AppPress.Settings.DebugEmail == null)
                    throw new Exception("Settings.DebugEmail should be defined.");
                var nextLine = Environment.NewLine;
                if (isHtml)
                    nextLine = "<br/>";

                var tempcontent = "Original Mail To: " + mailTo + nextLine;
                if (cc != null)
                    tempcontent += "Original CC : " + cc + nextLine;
                if (bcc != null)
                    tempcontent += "Original BCC : " + bcc + nextLine;
                tempcontent += nextLine;

                content = tempcontent + content;
                subject = "Testing (Debug) " + subject;
                mailTo = AppPress.Settings.DebugEmail;
                cc = null;
                bcc = null;
            }

            if (!cc.IsNullOrEmpty() && cc.IndexOf('@') == -1)
                throw new Exception("Internal Error: Mail CC (" + cc + ") is invalid");

            if (!bcc.IsNullOrEmpty() && bcc.IndexOf('@') == -1)
                throw new Exception("Internal Error: Mail BCC (" + bcc + ") is invalid");

            if (mailTo.IndexOf('@') == -1)
                throw new Exception("Internal Error: Mail To (" + mailTo + ") is invalid");

            if (content == null)
                content = "";

            var qry = string.Empty;
            if (content.Length >= 20000 - 100)
                content = content.Substring(0, 20000 - 100) + "\n Remaining Message Truncated....";

            string fileIds = null;
            if (uploadFileIds != null && uploadFileIds.Count > 0)
                fileIds = string.Join(",", uploadFileIds.ToArray());

            content = sitedb.EscapeSQLString(content);
            fileName = sitedb.EscapeSQLString(fileName);

            qry = string.Format("Insert into Application_sendmails(Subject, ReplyTo, Content," + sitedb.SQLQuote + "From" + sitedb.SQLQuote + ", cc,bcc,isHtml, fileName, UploadFileIds) Values (N'{0}',N'{1}',N'{2}',N'{3}',{4},{5},{6},{7},{8});", sitedb.EscapeSQLString(subject), mailTo, sitedb.EscapeSQLString(content), sitedb.EscapeSQLString(fromEmail), cc == null ? "null" : "'" + cc + "'", bcc == null ? "null" : "'" + bcc + "'", isHtml ? 1 : 0, fileName == null ? "null" : "'" + sitedb.EscapeSQLString(fileName) + "'", fileIds == null ? "null" : "'" + fileIds + "'");
            sitedb.ExecuteNonQuery(qry);
        }
        public void SendEmail(string mailTo, string fromEmail, string subject, string content, string cc, string bcc, bool isHtml, string fileName = null, List<Int64> uploadFileIds = null)
        {
            SendEmail(site, mailTo, fromEmail, subject, content, cc, bcc, isHtml, fileName = null, uploadFileIds);
        }
        internal static void SetupAssemblies(Assembly applicationAssembly, string applicationAssemblyNamespaceName, string applicationAssemblyClassName, List<string> assemblyNames)
        {
            // Add Application Dll
            var a = AppPress.Assemblies.Find(t => t.assembly.FullName == applicationAssembly.FullName);
            if (a != null)
                a.assembly = applicationAssembly;
            else
                AppPress.Assemblies.Add(new AppPressAssembly(applicationAssemblyNamespaceName, applicationAssemblyClassName, applicationAssembly, false));

            // AppPress.dll
            var executingAssembly = Assembly.GetExecutingAssembly();
            a = AppPress.Assemblies.Find(t => t.assembly.FullName == executingAssembly.FullName);
            if (a != null)
                a.assembly = executingAssembly;
            else
                AppPress.Assemblies.Add(new AppPressAssembly("AppPressFramework", "AppPressLogic", executingAssembly, false));
            if (assemblyNames != null)
                foreach (var assemblyName in assemblyNames)
                {
                    var aPath = HttpContext.Current.Server.MapPath("~/bin/") + assemblyName.Trim();
                    Log.Writeln("Loading Assembly: " + aPath);
                    var executingAssembly1 = Assembly.LoadFrom(aPath);
                    var a1 = AppPress.Assemblies.Find(t => t.assembly.FullName == executingAssembly1.FullName);
                    if (a1 != null)
                        a1.assembly = executingAssembly1;
                    else
                    {
                        var aAssembly = new AppPressAssembly("Application", "PluginLogic", executingAssembly1, true);
                        AppPress.Assemblies.Add(aAssembly);
                    }
                }

        }
        /// <summary>
        /// Prompts the User with a Message. If user clicks Yes then execution continues with next statement. if user clicks No then execution stops.
        /// </summary>
        /// <param name="message">Message to show to the User</param>
        /// <param name="title">Title of popup window</param>
        /// <param name="popupWidth">Width of popup window</param>
        /// <param name="IsHtml">Render Message as HTML text</param>
        public void PromptClient(string message, string title = null, int popupWidth = 0, bool IsHtml = false)
        {
            if (PromptClientResult == null)
            {
                serverFunction.ExecuteClientFunctions = true;
                appPressResponse.Clear();
                var clientAction = AppPressResponse.PromptClient(this, message, title, popupWidth, !IsHtml);
                throw new AppPressException(clientAction);
            }
        }
        internal void SaveDBAudit(AuditType auditType, string tableName, string tableRowId, FormData formData, string change)
        {
            var a = this;
            var sessionData = AppPress.TryGetSessionData();
            if (sessionData != null)
            {
                var userName = sessionData.email;
                if (userName == null)
                    userName = "";

                var page = HttpUtility.ParseQueryString(HttpContext.Current.Request.UrlReferrer.Query)["Form"];
                var id = HttpUtility.ParseQueryString(HttpContext.Current.Request.UrlReferrer.Query)["id"];
                if (id != null)
                {
                    if (id == "l")
                        id = a.sessionData.loginUserId;
                    id = "'" + id + "'";
                }
                else
                    id = "NULL";
                a.ExecuteNonQuery("Insert into Application_Audit(UserName,Time,AuditType,TableName,RowId," + a.SQLQuote + @"Change" + a.SQLQuote + @",Page,TimeStamp, PageId, LoginUserId)" +
            "Values ('" + userName + "','" + DateTime.Now.ToString(DAOBasic.DBDateTimeFormat) + "'," + (int)auditType + ",'" + tableName + "','" + formData.id + "','" + a.EscapeSQLString(change) + "','" + a.EscapeSQLString(page) + "'," + a.PageTimeStamp +
                "," + id + ", '" + a.sessionData.loginUserId + "')");
            }
        }
        internal void SaveDBAudit(AuditType auditType, string tableName, string primaryKey, FormData formData, List<FieldValue> updatedFieldValues)
        {
            var a = this;
            if (formData.formDef.NonSecure)
                return;
            string description = "";

            switch (auditType)
            {
                case AuditType.DeleteRow:
                    foreach (var formField in formData.formDef.formFields)
                        if (formData.fieldValues.Find(t => t.formField.fieldName == formField.fieldName) == null)
                        {
                            var f = formField.NewFieldValue(formData.formDef);

                            f.formField = formField;
                            f.FormData = formData;
                            updatedFieldValues.Add(f);
                        }
                    goto case AuditType.UpdateRow;
                case AuditType.UpdateRow:

                    var updatedValues = new Dictionary<string, string>();
                    foreach (var fieldValue in updatedFieldValues)
                        if (fieldValue.formField.Type == FormDefFieldType.PickMultiple)
                            if (fieldValue.Title != null)
                                updatedValues.Add(fieldValue.formField.fieldName, fieldValue.Title);
                    var dr = a.ExecuteQuery("Select * From " + a.SQLQuote + tableName + a.SQLQuote + " Where " + a.SQLQuote + primaryKey + a.SQLQuote + "='" + formData.id + "'");
                    try
                    {
                        if (dr.Read()) // form May not exist in DB, if Popup Deleted the Form
                            foreach (var fieldValue in updatedFieldValues)
                                if (fieldValue.formField.Type != FormDefFieldType.ForeignKey/* && !fieldValue.formField.DoNotSaveInDB*/)
                                {
                                    var fieldName = fieldValue.formField.fieldName;
                                    try
                                    {
                                        int ord = DAOBasic.TryGetOrdinal(dr, fieldName);
                                        if (ord != -1)
                                        {
                                            var oldValue = dr.IsDBNull(ord) ? null : dr[ord].ToString();
                                            if (oldValue != null)
                                            {
                                                switch (fieldValue.formField.EncryptionType)
                                                {
                                                    case EncryptionType.AES:
                                                        oldValue = AppPress.DecryptTextAES(oldValue);
                                                        break;
                                                    case EncryptionType.DES:
                                                        oldValue = AppPress.DecryptTextDES(oldValue);
                                                        break;
                                                }

                                            }
                                            if (auditType == AuditType.DeleteRow || oldValue != fieldValue.Value)
                                                updatedValues.Add(fieldName, oldValue);
                                        }

                                    }
                                    catch (IndexOutOfRangeException)
                                    { }
                                }
                    }
                    finally
                    {
                        dr.Close();
                    }
                    foreach (var item in updatedValues)
                    {
                        var fieldValue = formData.GetFieldValue(item.Key);
                        var oldValue = item.Value;
                        try
                        {
                            // TBD: options in drop down may have changed and it will not be possible to find the old value
                            var Value = fieldValue.Value;
                            try
                            {
                                fieldValue.Value = item.Value;
                                oldValue = fieldValue.GetFormattedFieldValue(a, false);
                            }
                            finally
                            {
                                fieldValue.Value = Value;
                            }

                        }
                        catch { }
                        if (oldValue != null && oldValue.Length > 2000)
                            oldValue = oldValue.Substring(0, 2000) + "...";
                        if (auditType == AuditType.DeleteRow)
                            description += fieldValue.formField.fieldName + ": " + oldValue + AppPress.auditEndOfLine;
                        else if (fieldValue.formField.Type == FormDefFieldType.Password)
                            description += "Password has been changed. ";
                        else
                        {

                            var newValue = fieldValue.GetFormattedFieldValue(a, false);
                            {
                                if (oldValue.IsNullOrEmpty())
                                    oldValue = "n/a";
                                if (newValue.IsNullOrEmpty())
                                    newValue = "n/a";
                                if (oldValue != newValue)
                                    description += fieldValue.formField.fieldName + ": " + oldValue + " => " + newValue + AppPress.auditEndOfLine;
                            }
                        }
                    }
                    if (description.IsNullOrEmpty())
                        return;
                    break;
                case AuditType.InsertRow:
                    foreach (var fieldValue in updatedFieldValues)
                        if (!fieldValue.formField.Static && fieldValue.formField.Type != FormDefFieldType.ForeignKey && !fieldValue.formField.DoNotSaveInDB)
                        {
                            var fieldName = fieldValue.formField.fieldName;
                            var newValue = fieldValue.GetFormattedFieldValue(a, false);
                            if (newValue != null && newValue.Length > 2000)
                                newValue = newValue.Substring(0, 2000) + "...";
                            if (newValue != null && newValue.Length > 0)
                                description += fieldName + ": " + newValue + AppPress.auditEndOfLine;
                        }

                    break;
                default:
                    throw new NotImplementedException();
            }
            a.SaveDBAudit(auditType, tableName, primaryKey, formData, description);
        }
        /// <summary>
        /// Loads the Form and Compiles it against the template DOCX file and returns the merged DOCX file
        /// </summary>
        /// <param name="formDefId">FormDefId of the Form</param>
        /// <param name="formDataId">Form Data Id of the Form</param>
        /// <param name="templateDocxFileName">Template file name</param>
        /// <param name="downloadDocxFileName">File Name to Download</param>
        public void DownloadDOCX(long formDefId, string formDataId, string templateDocxFileName, string downloadDocxFileName)
        {
            if (GetDOCXParams == null)
                GetDOCXParams = "";
            GetDOCXParams += formDefId + QuerySeperator1 + formDataId + QuerySeperator1 + templateDocxFileName + QuerySeperator1 + downloadDocxFileName + QuerySeperator;
        }
        /// <summary>
        /// Load the form and return as PDF file. FO file for transformation used is 
        ///   If the Skins folder contains .fo file with file name as name of form
        ///   Internally generated FO for the form
        /// </summary>
        /// <param name="formDefId">FormDefId of the Form</param>
        /// <param name="formDataId">Form Data Id of the Form</param>
        /// <param name="pdfFileName">Name of PDF file to download</param>
        /// <param name="reportFilter">String Passed back to PDF Generation in p.ApplicationData. Used in reporting to pass report filter values to PDF generation</param>
        /// <param name="skinIdex">SkinFileName Index, 0 will be <formName>.fo 1 will be <formName>-1.fo and so on</formName></formName></param>
        public void DownloadPDF(Type formClass, string formDataId, string pdfFileName, string reportFilter, int skinIndex = 0)
        {
            if (GetPDFParams == null)
                GetPDFParams = "";
            var fieldInfo = formClass.GetField("formDefId");
            if (fieldInfo == null)
                throw new AppPressException("Could not find formDefId in Class: " + formClass.Name);
            GetPDFParams += "F" + QuerySeperator1 + (long)fieldInfo.GetValue(null) + QuerySeperator1 + formDataId + QuerySeperator1 + pdfFileName + QuerySeperator1 + skinIndex + QuerySeperator1 + reportFilter + QuerySeperator;
        }
        public byte[] HtmlToPDF(string html, string fileName, PDFPageSettings pageSettings)
        {
            var fo = html.Replace("\n", "");
            fo = fo.Replace(@"<div style=""page-break-after: always""><span style=""display:none"">&nbsp;</span></div>", @"<fo:block page-break-before=""always""></fo:block>");
            fo = fo.Replace("<p>", @"<fo:block space-after=""4mm"">").Replace("</p>", "</fo:block>");
            fo = fo.Replace(@"<p style=""text-align:center"">", @"<fo:block space-after=""4mm"" text-align=""center"">");
            fo = fo.Replace(@"<p style=""text-align:right"">", @"<fo:block space-after=""4mm"" text-align=""right"">");
            fo = fo.Replace("<strong>", @"<fo:inline font-weight=""bold"">").Replace("</strong>", "</fo:inline>");
            fo = fo.Replace(@"<table>", @"<fo:table>");
            fo = fo.Replace(@"<table border=""1"" cellpadding=""0"" cellspacing=""0"">", @"<fo:table border-color=""black"" border-style=""solid"" border-width=""1pt"">").Replace("</table>", "</fo:table>");
            fo = fo.Replace(@"<tbody>", "<fo:table-body>").Replace("</tbody>", "</fo:table-body>");
            fo = fo.Replace(@"<tr>", "<fo:table-row>").Replace("</tr>", "</fo:table-row>");
            fo = fo.Replace(@"<td>", @"<fo:table-cell><fo:block>");
            fo = Regex.Replace(fo, @"<td style=""width:([0-9]*)px"">", @"<fo:table-cell width=""$1px""><fo:block>").Replace("</td>", "</fo:block></fo:table-cell>");
            fo = Regex.Replace(fo, @"<span style=""font-size:([0-9]*)px"">", @"<fo:inline font-size=""$1px"">").Replace("</span>", "</fo:inline>");
            fo = fo.Replace("<br />", @"&#xA;");
            fo = fo.Replace("<ol>", @"<fo:list-block space-after=""4mm"">").Replace("</ol>", "</fo:list-block>");
            fo = fo.Replace("<li>", @"<fo:list-item><fo:list-item-label end-indent = ""label-end()""><fo:block>
                                    &#x2022;</fo:block></fo:list-item-label><fo:list-item-body start-indent = ""body-start()""><fo:block>
                                    ").Replace("</li>", @"</fo:block></fo:list-item-body></fo:list-item>");
            fo = fo.Replace("&nbsp;", "&#160;");
            fo = fo.Replace("&amp;", "&#0038;");
            fo = fo.Replace("&ndash;", "&#8211;");
            fo = fo.Replace("&rsquo;", "&#8217;");
            if (pageSettings.contentImages != null)
                foreach (var l in pageSettings.contentImages)
                    fo = fo.Replace(l.Key, "<fo:external-graphic src=\"url('file:///" + l.Value + "')\"/>");

            fo = @"<?xml version=""1.0"" encoding=""utf-8""?>
                                        <fo:root xmlns:fo=""http://www.w3.org/1999/XSL/Format"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
                                  <fo:layout-master-set>
                                    <fo:simple-page-master master-name=""simple""
                                                  page-height=""" + pageSettings.pageHeight + @"cm""
                                                  page-width=""" + pageSettings.pageWidth + @"cm""                                
                                                  margin-top=""" + pageSettings.topMargin + @"cm""
                                                  margin-bottom=""" + pageSettings.bottomMargin + @"cm""
                                                  margin-left=""" + pageSettings.leftMargin + @"cm""
                                                  margin-right=""" + pageSettings.rightMargin + @"cm"">
                                      <fo:region-body/>
      <fo:region-before region-name=""page-header"" extent=""4cm""/>
        <fo:region-after region-name=""page-footer"" extent=""0cm"" />
                                             </fo:simple-page-master>
                                  </fo:layout-master-set>
                                  <fo:page-sequence master-reference=""simple"">
    <fo:static-content flow-name=""page-header"">
      <fo:block margin-left=""0.4cm"" margin-right=""0.2cm"">
      </fo:block>
    </fo:static-content>

<fo:flow flow-name=""xsl-region-body"">
                                      <fo:block font-size=""10pt"" color=""black"" text-align=""left"" linefeed-treatment=""preserve"">
" + fo + @"                                
                                      </fo:block>
      <fo:block id=""last-page""></fo:block>
                                      </fo:flow>
                                  </fo:page-sequence>
                                </fo:root>";
            var pdf = Util.FOtoPDF(fo);
            if (pageSettings.contentImages != null)
                foreach (var l in pageSettings.contentImages)
                    File.Delete(l.Value);
            return pdf;
            /*
                <fo:static-content flow-name=""page-footer"">
      <fo:block text-align-last=""justify"" border-before-style=""solid"" border-before-width=""1pt"" color=""#808080"">
  " + fileName + @"
  <fo:leader leader-pattern=""space""/>
   Page <fo:page-number/>/<fo:page-number-citation ref-id=""last-page""/>
      </fo:block>
    </fo:static-content>
    */
        }
        public void DownloadPDF(string query, string pdfFileName, PDFPageSettings pageSettings)
        {
            if (GetPDFParams == null)
                GetPDFParams = "";
            var pstr = "";
            if (pageSettings != null)
                pstr = FormDef.Serialize(pageSettings, typeof(PDFPageSettings));
            GetPDFParams += "Q" + QuerySeperator1 + query + QuerySeperator1 + pdfFileName + QuerySeperator1 + pstr + QuerySeperator;
        }
        public string EscapeSQLString(string value)
        {
            return site.EscapeSQLString(value);
        }
        public static string EscapeJSString(string value)
        {
            return Microsoft.JScript.GlobalObject.escape(value);
        }
        public static string GenerateAppPressClasses(string[] jsFiles, string[] xmlFiles, string[] skinFiles, string FormDefsJSon)
        {
            try
            {
                // Assuming this function will not be called again with a second
                AppPress.newFormID = (long)-((DateTime.UtcNow.Ticks - new DateTime(DateTime.Now.Year, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).Ticks) / (10000 * 1000)) * (10000 * 1000);

                List<FormDef> formDefsParent = null;
                if (FormDefsJSon != null)
                    formDefsParent = (List<FormDef>)FormDef.Deserialize(FormDefsJSon, typeof(List<FormDef>));
                // Generate Class
                AppPress.Assemblies.Add(new AppPressAssembly("AppPressFramework", "AppPressLogic", Assembly.GetExecutingAssembly(), false));
                var a = new AppPress();
                // get unique id based on clock.
                // force wait a second so that if called twice in same second stil gives unique
                // should not be called in parallel in same project 
                // wait for time span to avoid 2 calls within timespan
                var ticksTill = (DateTime.UtcNow + +new TimeSpan(0, 0, 2)).Ticks;
                while (DateTime.UtcNow.Ticks < ticksTill)
                    ;

                long elapsedTicks = DateTime.Now.Ticks - new DateTime(2015, 3, 1).Ticks; // cannot be a day before this as appPress did not exist
                TimeSpan elapsedSpan = new TimeSpan(elapsedTicks);
                var formDefs = new List<FormDef>();
                foreach (var xmlPattern in xmlFiles)
                {
                    var xFiles = Directory.GetFiles(xmlPattern, "*.xml");
                    foreach (var xmlFile in xFiles)
                    {
                        var xmlDoc = XDocument.Load(xmlFile, LoadOptions.SetLineInfo | LoadOptions.SetBaseUri);
                        var formDefsFile = FormDef.LoadFormDefs(a, xmlDoc, false);
                        formDefs.AddRange(formDefsFile);
                    }
                }
                var s = "";
                if (jsFiles != null)
                    foreach (var jsFile in jsFiles)
                        s += Util.EscapeForCHash(System.IO.File.ReadAllText(jsFile)) + "+\n";
                var PageDesignerHtml = "";
                if (skinFiles != null)
                    if (skinFiles != null)
                        foreach (var skinFile in skinFiles)
                            PageDesignerHtml += Util.EscapeForCHash(System.IO.File.ReadAllText(skinFile)) + "+\n";
                var code = Util.GenerateClasses(a, formDefs, true, formDefsParent);
                if (!s.IsNullOrEmpty() || !PageDesignerHtml.IsNullOrEmpty())
                    code += @"
                internal class FileTexts
                    {
                    internal static string AppPressJS = " + s + @""""";
                    internal static string PageDesignerHtml = " + PageDesignerHtml + @""""";
                    }";
                return code;
            }
            catch (ReflectionTypeLoadException ex)
            {
                StringBuilder sb = new StringBuilder();
                foreach (Exception exSub in ex.LoaderExceptions)
                {
                    sb.AppendLine(exSub.Message);
                    FileNotFoundException exFileNotFound = exSub as FileNotFoundException;
                    if (exFileNotFound != null)
                    {
                        if (!string.IsNullOrEmpty(exFileNotFound.FusionLog))
                        {
                            sb.AppendLine("Fusion Log:");
                            sb.AppendLine(exFileNotFound.FusionLog);
                        }
                    }
                    sb.AppendLine();
                }
                string errorMessage = sb.ToString();
                throw new Exception(errorMessage);
            }
            catch (System.Xml.XmlException ex)
            {
                throw new Exception("File: " + ex.SourceUri + " " + ex.Message);
            }
            catch (Exception)
            {
                throw;
                //return "Error in Code Generation: " + ex.Message;
            }
        }
        /// <summary>
        /// Gets a unique integer value. It is unique since the application was started, so should not be used for persistance.
        /// </summary>
        /// <returns></returns>
        public static long GetUniqueId()
        {
            return AppPress.newFormID--;
        }
        public static string InsertSpacesBetweenCaps(string name)
        {
            if (name == null)
                return null;
            // insert space 
            // between lower case and upper case letter
            // before Upper and lower combination
            string fieldName = string.Empty;
            var fieldNameArray = name.ToCharArray();
            for (var i = 0; i < name.Length; ++i)
            {
                if (i > 0 && char.IsUpper(fieldNameArray[i]) && i < name.Length - 1 && char.IsLower(fieldNameArray[i + 1]))
                    fieldName += " ";
                fieldName += fieldNameArray[i];
                if (char.IsLower(fieldNameArray[i]) && i < name.Length - 1 && char.IsUpper(fieldNameArray[i + 1]))
                    fieldName += " ";
            }
            return fieldName.Replace("  ", " ");
        }
        internal string FormatNumber(FormField fieldDef, string value)
        {
            if (String.IsNullOrEmpty(value))
                return "";
            decimal d;
            if (!decimal.TryParse(value, out d))
                return value;
            if (fieldDef.ZeroAsBlank && d == 0.0m)
                return "";
            return (d).ToString(((fieldDef.Static || skinType == SkinType.FO) ? "N" : "F") + fieldDef.decimals, CultureInfo.CurrentCulture);
        }

        public string GetFileUploadImageUrlNonSecure(int? FileUploadId, int? width = null, int? height = null)
        {
            if (FileUploadId == null)
                return null;
            var s1 = ExecuteScalarList("Select FilePath, FileName From Application_Files Where Id=" + FileUploadId);
            if (s1 != null && s1[0] != null && s1[0].ToString().StartsWith("~"))
            {
                return AppPress.GetBaseUrl() + s1[0].ToString().Substring(1);
            }
            if (width == null && height == null)
                width = 100;
            var s = AppPress.GetBaseUrl() + AppPress.GetDefaultAspx() + "?GetFile=&Download=true&id=" + FileUploadId;
            if (width.HasValue)
                s += "&width=" + width;
            if (height.HasValue)
                s += "&height=" + height;
            return s;
        }
        /// <summary>
        /// Returns URL to show uploaded Photo. If only one of width or height is specified the image is scaled propertionally in other axis
        /// </summary>
        /// <param name="FileUploadId">Id of file uploaded</param>
        /// <param name="width">Width in Pixels of Photo</param>
        /// <param name="height">Height in Pixels of thw Photo</param>
        /// <returns></returns>
        public string GetFileUploadImageUrl(int? FileUploadId, int? width = null, int? height = null)
        {
            var s = GetFileUploadImageUrlNonSecure(FileUploadId, width, height);
            this.sessionData.AddSecureUrl(s);
            return s;
        }
        /// <summary>
        /// Returns URL to show uploaded Photo. If only one of width or height is specified the image is scaled propertionally in other axis
        /// </summary>
        /// <param name="FilePath">Path of File uploaded using DoNotSaveInDB property</param>
        /// <param name="width">Width in Pixels of Photo</param>
        /// <param name="height">Height in Pixels of thw Photo</param>
        /// <returns></returns>
        public string GetFileUploadImageUrl(string FilePath, int? width = null, int? height = null)
        {
            if (FilePath == null)
                return "";
            if (!File.Exists(FilePath)) //Means here is FileId comming.
            {
                FilePath = HttpContext.Current.Server.MapPath("~/Resources/Img/file-not-found.jpg");
            }

            if (width == null && height == null)
                width = 100;
            var s = GetFileUrl(FilePath);
            if (width.HasValue)
                s += "&width=" + width;
            if (height.HasValue)
                s += "&height=" + height;
            this.sessionData.AddSecureUrl(s);
            return s;
        }
        /// <summary>
        /// Moves File uploaded earlier into a temp file and returns its path.
        /// Used if you need the contents of upload file as a File
        /// </summary>
        /// <param name="fileId">Id of uploaded file using FileUpload Control without DoNotSaveInDB property</param>
        /// <returns></returns>
        public string GetFileUploadAsTempFilePath(int fileId)
        {
            var fileDetails = GetFile(fileId);
            var tempPath = Path.GetTempPath();
            var tempFilePath = Path.Combine(tempPath, fileDetails.FileName);
            if (File.Exists(tempFilePath))
                File.Delete(tempFilePath);
            File.WriteAllBytes(tempFilePath, fileDetails.FileBytes);
            return tempFilePath;
        }
        /// <summary>
        /// ???
        /// </summary>
        /// <param name="fileId"></param>
        /// <returns></returns>
        public static FileDetails GetFile(int fileId)
        {
            string fileName;
            string fileContentType;
            DateTime uploadTime;
            var site = new DAOBasic();
            byte[] fileBytes;
            try
            {
                fileBytes = Util.GetFile(site, null, fileId, out fileName, out fileContentType, out uploadTime);
            }
            finally
            {
                site.Close();
            }
            if (fileBytes == null)
                return null;
            FileDetails oFileDetails = new FileDetails();
            oFileDetails.FileId = fileId;
            oFileDetails.FileName = fileName;
            oFileDetails.ContentType = fileContentType;
            oFileDetails.UploadTime = uploadTime;
            oFileDetails.FileBytes = fileBytes;
            return oFileDetails;
        }
        private static Thread autoEmailSendThread = null;
        private static void StartAutoEmailThread()
        {
            if (autoEmailSendThread == null)
            {
                autoEmailSendThread = new Thread(AppPressLogic.SendEmails);
                autoEmailSendThread.Priority = ThreadPriority.Lowest;
                autoEmailSendThread.Start();
            }
        }

        /// <summary>
        /// Does a Garbage Collection on Application_Files table.
        /// This function is slow and should be called in a thread on idle time
        /// </summary>
        /// <param name="site"></param>
        /// <param name="deleteUnusedFiles"></param>
        public static void MarkFilesUnused(DAOBasic site, bool deleteUnusedFiles)
        {
            site.BeginTrans();
            try
            {
                site.ExecuteNonQuery("Update Application_Files Set Used=0");
                var q = site.GetForeignKeysQuery("application_files", "Id");
                var dr = site.ExecuteQuery(q);
                q = null;
                try
                {
                    while (dr.Read())
                    {
                        q = q == null ? "" : (q + "\nUnion All\n");
                        var tableName = dr.GetString(0);
                        var columnName = dr.GetString(1);
                        q += @"Select " + site.SQLQuote + "" + columnName + "" + site.SQLQuote + " From " + site.SQLQuote + "" + tableName + "" + site.SQLQuote + "";
                    }
                }
                finally
                {
                    dr.Close();
                }
                if (q != null)
                {
                    site.ExecuteNonQuery("Update Application_Files Set Used=1 Where id in (" + q + ")");
                    if (deleteUnusedFiles)
                        site.ExecuteNonQuery("Delete From Application_Files Where Used=0");
                }
                site.Commit();
            }
            catch (Exception)
            {
                site.RollBack();
                throw;
            }
        }

        static MethodInfo BeforeCommit = null;


        /// <summary>
        /// Initialize AppPress. This must be called in PageLoad
        /// </summary>
        /// <param name="settings">Settings to use in AppPress</param>
        public static void InitAppPress(AppPressSettings settings)
        {
            if (settings.ApplicationAppPress == null || settings.ApplicationAppPress.BaseType != typeof(AppPress))
                throw new Exception("Settings.ApplicationAppPress should point to class whose Base Type is AppPress");
            AppPress.startTime = DateTime.UtcNow.Ticks;
            if (AppPress.Settings != null)
                throw new Exception("InitAppPress Called Already");
            try
            {
                AppPress.Settings = settings;

                AppPress.SetupAssemblies(settings.applicationAssembly, settings.applicationNameSpace, settings.applicationClassName, settings.pluginAssemblyNames);

                foreach (var instance in AppPress.Settings.Instances)
                {
                    if (instance.InstanceId <= 0 || instance.InstanceId >= 99)
                        throw new Exception("In InitAppPress the InstanceId should be between 1-99");
                    if (AppPress.Settings.Instances.Find(t => t != instance && t.InstanceId == instance.InstanceId) != null)
                        throw new Exception("In InitAppPress the InstanceId should be unique");
                    //if (AppPress.Settings.Instances.Find(t => t != instance && t.InstanceBaseUrl == instance.InstanceBaseUrl) != null)
                    //    throw new Exception("In InitAppPress the InstanceBaseUrl should be unique");
                }
                var localInstances = AppPress.Settings.Instances.FindAll(t => t.LocalInstance);
                if (localInstances.Count() > 1)
                    throw new Exception("Found more than one Local Instances in AppPress Settings");
                if (localInstances.Count() == 0)
                    throw new Exception("Did not find Local Instances in AppPress Settings");
                AppPress.LocalInstanceId = localInstances[0].InstanceId;

                switch (settings.databaseType)
                {
                    case DatabaseType.MySql:
                        DAOBasic.SchemaColumnName = "TABLE_SCHEMA";
                        break;
                    case DatabaseType.SqlServer:
                        DAOBasic.SchemaColumnName = "TABLE_CATALOG";
                        break;
                }

                if (AppPress.Settings.ConnectionString == null)
                    throw new Exception("Settings.ConnectionString should be defined.");
                if (AppPress.Settings.applicationAssembly == null)
                    throw new Exception("Settings.applicationAssembly should be defined.");
                if (AppPress.Settings.applicationNameSpace == null)
                    throw new Exception("Settings.applicationNameSpace should be defined.");
                if (AppPress.Settings.applicationClassName == null)
                    throw new Exception("Settings.applicationClassName should be defined.");

                var site = new DAOBasic();
                try
                {
                    string startLimitClause = "";
                    string endLimitClause = "";
                    if (settings.databaseType == DatabaseType.SqlServer)
                        startLimitClause = " TOP 1 ";
                    if (settings.databaseType == DatabaseType.MySql)
                        endLimitClause = " LIMIT 1 ";

                    site.ExecuteScalar(@"SELECT " + startLimitClause + " Id,AuditType,TableName,RowId,UserName,Time,Page," + site.SQLQuote + "Change" + site.SQLQuote + ",TimeStamp FROM application_audit " + endLimitClause);
                    site.ExecuteScalar(@"SELECT " + startLimitClause + "  Id,AuditType FROM application_audittype " + endLimitClause);
                    site.ExecuteScalar(@"SELECT " + startLimitClause + " Id,FileName,UploadDate,FileSize,Preview,FileContent,FileType,TransactionId,Checksum,Used,EncryptionType,StorageType,FilePath,NonSecure FROM application_files " + endLimitClause);
                    site.ExecuteScalar(@"SELECT " + startLimitClause + " Id,Subject,ReplyTo,Content," + site.SQLQuote + "From" + site.SQLQuote + ",FileName,CC,Error,bcc,isHtml,TimeStamp,UploadFileIds FROM application_sendmails " + endLimitClause);
                }
                catch (Exception ex)
                {
                    throw new Exception("Database tables needed for AppPress do not exist or have invalid columns. Create or Correct the Database using Script Database\\DatabaseScript.sql\nSQL Error: " + ex.Message);
                }
                finally
                {
                    site.Close();
                }

                AppPress a = new AppPress();
                a.site = new DAOBasic();
                try
                {
                    Util._LoadFormDefs(a);
                    CheckAppPressFunctions();
                }
                finally
                {
                    a.site.Close();
                }

                if (AppPress.Settings.encryptionKey == null)
                {
                    // check if using Encryption
                    foreach (var form in AppPress.formDefs)
                        foreach (var formField in form.formFields)
                            if (formField.EncryptionType != null)
                                throw new Exception("No EncryptionKey is provided in InitAppPress. Encryption is being used for:" + formField.GetDescription());
                }

                var errorForm = AppPress.FindFormDef("ErrorForm");
                if (errorForm == null)
                    throw new Exception("Could not find Form: ErrorForm");
                var blankMasterErrorForm = AppPress.FindFormDef("BlankMasterErrorForm");
                if (blankMasterErrorForm == null)
                    throw new Exception("Could not find Form: BlankMasterErrorForm");

                AppPress.InternalMessageData.Clear();
                AppPress.InternalMessageData.Add("LAKey_RequiredMsg", new Dictionary<string, string> { { "English", "This field is required...." } });
                foreach (var key in AppPress.Settings.LocalizationData.Keys)
                    AppPress.LocalizationData.Add(key, AppPress.Settings.LocalizationData[key]);
                foreach (var key in AppPress.Settings.LocalizationLanguages)
                    AppPress.LocalizationLanguages.Add(key);
                foreach (var key in AppPress.InternalMessageData.Keys)
                    if (!AppPress.LocalizationData.ContainsKey(key))
                        AppPress.LocalizationData.Add(key, AppPress.InternalMessageData[key]);

                foreach (var instance in AppPress.Settings.Instances)
                {
                    if (AppPress.Settings.Instances.Find(t => t != instance && t.InstanceId == instance.InstanceId) != null)
                        throw new Exception("Duplicate InstanceId");
                    //if (AppPress.Settings.Instances.Find(t => t != instance && t.InstanceBaseUrl == instance.InstanceBaseUrl) != null)
                    //    throw new Exception("Duplicate InstanceUrl");
                }
                // Call PluginInit of Plugins
                foreach (var assembly in AppPress.Assemblies)
                {
                    if (assembly.className != "PluginLogic")
                        continue;
                    var types = new Type[] { AppPress.Settings.ApplicationAppPress };
                    var method = assembly.appLogicType.GetMethod("PluginInit", BindingFlags.Public | BindingFlags.Static, null, types, null);
                    if (method == null)
                        throw new AppPressException("Could not find Function public static void PluginInit(AppPress a) in namespace Application and class PluginLogic in Plugin: " + assembly.assemblyName);
                    Util.InvokeMethod(a, method, new object[] { a });
                }
                StartAutoEmailThread();
                BeforeCommit = Util.GetMethod(null, "BeforeCommit", new Type[] { AppPress.Settings.ApplicationAppPress }, false);
            }
            catch (ReflectionTypeLoadException ex)
            {
                StringBuilder sb = new StringBuilder();
                foreach (Exception exSub in ex.LoaderExceptions)
                {
                    sb.AppendLine(exSub.Message);
                    FileNotFoundException exFileNotFound = exSub as FileNotFoundException;
                    if (exFileNotFound != null)
                    {
                        if (!string.IsNullOrEmpty(exFileNotFound.FusionLog))
                        {
                            sb.AppendLine("Fusion Log:");
                            sb.AppendLine(exFileNotFound.FusionLog);
                        }
                    }
                    sb.AppendLine();
                }
                string errorMessage = sb.ToString();
                throw new Exception(errorMessage);
            }
            catch (Exception)
            {
                if (autoEmailSendThread != null)
                    autoEmailSendThread.Abort();
                autoEmailSendThread = null;
                AppPress.formDefs = null;
                Settings = null;
                throw;
            }
        }

        /// <summary>
        /// Get HTML from result of DataReader
        /// </summary>
        /// <param name="dr"></param>
        /// <param name="rows"></param>
        /// <returns></returns>
        public static string GetHtmlTableFromDataReader(IDataReader dr, out int rows, bool NumericZeroAsBlank, string header)
        {
            var html = "<table id='FormContainerTable'><tbody>";
            if (header != null)
                html += header;
            else
            {
                html += "<tr>";
                for (int i = 0; i < dr.FieldCount; i++)
                {
                    var dataType = dr.GetDataTypeName(i);
                    html += @"<th>" + dr.GetName(i) + @"</th>";
                }
                html += "</tr>";
            }
            int indexRow = 0;
            while (dr.Read())
            {
                indexRow++;
                html += "<tr>";
                for (int i = 0; i < dr.FieldCount; i++)
                {
                    var dataType = dr.GetDataTypeName(i);
                    var colName = dr.GetName(i);
                    string align = null;
                    var colText = (dr.IsDBNull(i) ? "" : dr[i].ToString());
                    switch (dataType)
                    {
                        case "INT":
                        case "DOUBLE":
                        case "DECIMAL":
                            align = "style='text-align:right'";
                            if (NumericZeroAsBlank && !colText.IsNullOrEmpty() && decimal.Parse(colText) == 0.0m)
                            {
                                colText = "";
                            }
                            break;
                    }
                    if (!colText.IsNullOrEmpty() && colText.StartsWith("LKey_"))
                        colText = GetLocalizationKeyValue(colText);
                    html += @"<td " + (align == null ? "" : @"" + align) + @">" + colText + @"</td>";
                }
                html += "</tr>";
            }
            html += "</tbody></table>";
            rows = indexRow;
            return html;
        }

        /// <summary>
        /// Gets the result of Query as HTML formated Tables
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public string GetHtmlTableFromQuery(string query, bool NumericZeroAsBlank, string header)
        {
            var dr = site.ExecuteQuery(query);
            string html = null;
            try
            {
                int rows = 0;
                html = GetHtmlTableFromDataReader(dr, out rows, NumericZeroAsBlank, header);
            }
            finally
            {
                dr.Close();
            }
            return html;
        }


        internal void AppPopup(AppPressResponse popup)
        {
            var errors = appPressResponse.FindAll(t => t.appPressResponseType == AppPressResponseType.FieldError || t.appPressResponseType == AppPressResponseType.FormError || t.appPressResponseType == AppPressResponseType.ExecuteJSScript);
            appPressResponse.RemoveAll(t => t.appPressResponseType == AppPressResponseType.FieldError || t.appPressResponseType == AppPressResponseType.FormError || t.appPressResponseType == AppPressResponseType.ExecuteJSScript);
            appPressResponse.Add(popup);
            appPressResponse.AddRange(errors);

        }

        public static SecureString ConvertToSecureString(string NonSecureString)
        {
            var secureStr = new SecureString();
            string _key = "hfdfwe8yur98w4";
            if (NonSecureString.Length > 0)
            {
                foreach (var c in NonSecureString.ToCharArray()) secureStr.AppendChar(c);
                foreach (var c in _key.ToCharArray()) secureStr.AppendChar(c);
            }
            return secureStr;
        }
        internal static string ConvertToNonSecureString(SecureString SecureString)
        {
            IntPtr unmanagedString = IntPtr.Zero;
            try
            {
                unmanagedString = Marshal.SecureStringToGlobalAllocUnicode(SecureString);
                return Marshal.PtrToStringUni(unmanagedString);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(unmanagedString);
            }
        }
        static string defaultAspx = null;
        public static string GetDefaultAspx()
        {
            if (defaultAspx != null)
                return defaultAspx;
            var fullPath = HttpContext.Current.Request.Url.AbsolutePath;
            defaultAspx = System.IO.Path.GetFileName(fullPath);
            return defaultAspx;
        }

    }
}
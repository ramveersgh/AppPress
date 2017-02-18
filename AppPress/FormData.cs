using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Globalization;
using System.Data;
using System.Reflection;
using System.Web;
using System.Text.RegularExpressions;
using System.Runtime.Serialization;
using System.IO;

namespace AppPressFramework
{
    [DataContract]
    public class FormData
    {
        internal AppPress a;
        public string NewId = null;

        /// <summary>
        /// Id of the FormData. if FormData is loaded from database then it is the Value of Id column. For new Forms AppPress assigns a unique negative number
        /// </summary>
        [DataMember]
        public string id = null;
        [DataMember]
        internal long formDefId;
        /// <summary>
        /// ??? Look at what to do with this
        /// </summary>
        [DataMember]
        public List<FieldValue> fieldValues = new List<FieldValue>();
        [DataMember]
        internal bool IsDeleted;
        [DataMember]
        internal int pageStackIndex; // basePage + popups. 0 for base Page 1 for 1st Popup and so on
        [DataMember]
        internal string cfv; // reference for ContainerFieldValue and CallerFieldValue
        [DataMember]
        internal bool fromPopupSave = false;
        /// <summary>
        /// If this Form is part of popup
        /// </summary>
        public bool IsPopup { get { return pageStackIndex > 0; } }
        /// <summary>
        /// for internal use only
        /// </summary>
        public FieldValue containerFieldValue;
        /// <summary>
        /// for internal use only
        /// </summary>
        public FieldValue callerFieldValue;

        internal FormDef formDef;
        internal string originalId = null;
        internal bool IsSubmitted = false;

        /// <summary>
        /// set this property to Error string which will be shown on UI along the field
        /// </summary>
        public string Error { set { a.appPressResponse.Add(AppPressResponse.FormError(this, value)); } }

        internal FormData()
        {
        }

        public FormData(AppPress a, long formDefId)
            : this(a, AppPress.FindFormDef(formDefId), null)
        {

        }
        public FormData(FormData formData)
        {
            var a = formData.a;
            Type t = typeof(FormData);
            foreach (FieldInfo fieldInf in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                fieldInf.SetValue(this, fieldInf.GetValue(formData));
            }
            var idx = a.formDatas.FindIndex(t1 => t1 == formData);
            if (idx != -1)
            {
                a.formDatas.RemoveAt(idx);
                a.formDatas.Insert(idx, this);
            }
            foreach (var f in this.fieldValues)
                f.FormData = this;
        }
        /// <summary>
        /// ???
        /// </summary>
        /// <param name="a"></param>
        /// <param name="dbId"></param>
        /// <param name="Title"></param>
        /// <param name="formFieldsQuery"></param>
        public FormData(AppPress a, long dbId, string Title, string formFieldsQuery)
        {
            // read fields from Query and create a formDef
            formDef = new FormDef();
            formDef.id = AppPress.GetUniqueId();
            formDef.dbId = dbId;
            id = AppPress.GetUniqueId().ToString();
            formDefId = formDef.id;
            formDef.formName = Title;
            // Find if formdata is saved earlier
            var saveId = a.ExecuteString(@"Select Id From " + a.SQLQuote + "survey.submit.data" + a.SQLQuote + " Where SurveyMasterId=" + dbId + " and EmployeeId=" + a.sessionData.loginUserId);
            if (saveId != null)
                this.id = saveId;
            // Add Save Button
            var saveFieldId = AppPress.GetUniqueId();
            formFieldsQuery = @"
                Select * From (" + formFieldsQuery + @"
                UNION ALL
                Select " + saveFieldId + @" Id, 'Submit' as FieldDescription, '' as FieldNotes, " + (int)FormDefFieldType.Button + @" as FieldType ) As A";
            var dr = a.ExecuteQuery(formFieldsQuery);
            try
            {
                while (dr.Read())
                {
                    var formField = new FormField();
                    formField.id = AppPress.GetUniqueId();
                    formField.dbId = dr.GetInt64(dr.GetOrdinal("Id"));
                    formField.fieldName = "Field" + (-formField.id);
                    formField.Type = (FormDefFieldType)dr.GetInt16(dr.GetOrdinal("FieldType"));
                    formField.Label = dr.GetString(dr.GetOrdinal("FieldDescription"));
                    if (formField.Label == null)
                        throw new Exception("FieldDescription should not be null.");
                    formField.formDef = formDef;
                    formDef.formFields.Add(formField);
                    if (formField.dbId == saveFieldId)
                    {
                        var Function = new ServerFunction(a, FunctionType.OnClick, "SaveFormInternal");
                        formField.FieldFunctions.Add(Function);
                        Function = new ServerFunction(a, FunctionType.OnClick, "RefreshContainer");
                        formField.FieldFunctions.Add(Function);
                    }
                    var fieldValue = formField.NewFieldValue(formDef);
                    fieldValue.formField = formField;
                    fieldValue.fieldDefId = formField.id;
                    fieldValue.FormData = this;
                    fieldValues.Add(fieldValue);
                }
                if (!IsNew)
                {
                    dr.Close();
                    foreach (var fieldValue in fieldValues)
                    {
                        // read value from db
                        switch (fieldValue.formField.Type)
                        {
                            case FormDefFieldType.Text:
                            case FormDefFieldType.TextArea:
                                {
                                    var o = a.ExecuteScalar("Select TextValue From " + a.SQLQuote + "survey.answer.data" + a.SQLQuote + " Where " + a.SQLQuote + "survey.submit.data.id" + a.SQLQuote + "=" + this.id + " and SurveyQuestionId=" + fieldValue.formField.dbId);
                                    if (o != null)
                                        fieldValue.Value = o.ToString();
                                }
                                break;
                            case FormDefFieldType.Pickone:
                            case FormDefFieldType.Checkbox:
                                {
                                    var o = a.ExecuteString("Select IntValue From " + a.SQLQuote + "survey.answer.data" + a.SQLQuote + " Where " + a.SQLQuote + "survey.submit.data.id" + a.SQLQuote + "=" + this.id + " and SurveyQuestionId=" + fieldValue.formField.dbId);
                                    if (o != null)
                                        fieldValue.Value = o;
                                }
                                break;
                            case FormDefFieldType.DateTime:
                                {
                                    var o = a.ExecuteDateTime("Select DateTimeValue From " + a.SQLQuote + "survey.answer.data" + a.SQLQuote + " Where " + a.SQLQuote + "survey.submit.data.id" + a.SQLQuote + "=" + this.id + " and SurveyQuestionId=" + fieldValue.formField.dbId);
                                    if (o != null)
                                        fieldValue.Value = o.Value.ToString(DAOBasic.DBDateTimeFormat);
                                    break;
                                }
                            case FormDefFieldType.Number:
                                {
                                    var o = a.ExecuteScalar("Select DecimalValue From " + a.SQLQuote + "survey.answer.data" + a.SQLQuote + " Where " + a.SQLQuote + "survey.submit.data.id" + a.SQLQuote + "=" + this.id + " and SurveyQuestionId=" + fieldValue.formField.dbId);
                                    if (o != null)
                                        fieldValue.Value = ((Decimal)o).ToString();
                                }
                                break;
                            case FormDefFieldType.PickMultiple:
                                {
                                    var o = a.ExecuteScalar("Select Id From " + a.SQLQuote + "survey.answer.data" + a.SQLQuote + " Where " + a.SQLQuote + "survey.submit.data.id" + a.SQLQuote + "=" + this.id + " and SurveyQuestionId=" + fieldValue.formField.dbId);
                                    if (o != null)
                                    {
                                        o = a.ExecuteScalar(@"
                                            Select Group_Concat(" + a.SQLQuote + @"application.formfields.pickone.options.id" + a.SQLQuote + @") 
                                            From " + a.SQLQuote + "survey.answer.data.pickmultiple" + a.SQLQuote + @" 
                                            Where " + a.SQLQuote + "survey.answer.data.id" + a.SQLQuote + "=" + o.ToString()
                                            );
                                        if (o != null)
                                            fieldValue.Value = o.ToString();
                                    }
                                    break;
                                }
                        }
                    }
                }
            }
            finally
            {
                dr.Close();
            }
            foreach (var fieldValue in fieldValues)
            {
                var formField = fieldValue.formField;
                if (formField.dbId < 0) // internal fields
                    continue;
                if (formField.Type == FormDefFieldType.Checkbox)
                    continue;
                var tableName = "application.formfields." + formField.Type;
                if (formField.Type == FormDefFieldType.PickMultiple)
                    tableName = "application.formfields." + FormDefFieldType.Pickone;
                dr = a.ExecuteQuery(@"Select * From  " + a.SQLQuote + "" + tableName + "" + a.SQLQuote + " Where SurveyQuestionsId=" + fieldValue.formField.dbId);
                try
                {
                    if (dr.Read())
                        switch (formField.Type)
                        {
                            case FormDefFieldType.Password:
                                {
                                    formField.Required = dr.GetBoolean(dr.GetOrdinal("Required"));
                                    if (!dr.IsDBNull(dr.GetOrdinal("MaxChars")))
                                        formField.MaxChars = dr.GetInt32(dr.GetOrdinal("MaxChars"));
                                    break;
                                }
                            case FormDefFieldType.Text:
                                {
                                    formField.Required = dr.GetBoolean(dr.GetOrdinal("Required"));
                                    formField.EmailValidation = dr.GetBoolean(dr.GetOrdinal("EmailValidation"));
                                    if (!dr.IsDBNull(dr.GetOrdinal("Regex")))
                                        formField.RegexValidation = dr.GetString(dr.GetOrdinal("Regex"));
                                    if (!dr.IsDBNull(dr.GetOrdinal("MaxChars")))
                                        formField.MaxChars = dr.GetInt32(dr.GetOrdinal("MaxChars"));
                                    if (!dr.IsDBNull(dr.GetOrdinal("EncryptionType")))
                                        formField.EncryptionType = (EncryptionType)dr.GetInt32(dr.GetOrdinal("EncryptionType"));
                                    break;
                                }
                            case FormDefFieldType.TextArea:
                                {
                                    formField.Required = dr.GetBoolean(dr.GetOrdinal("Required"));
                                    if (!dr.IsDBNull(dr.GetOrdinal("MaxChars")))
                                        formField.MaxChars = dr.GetInt32(dr.GetOrdinal("MaxChars"));
                                    if (!dr.IsDBNull(dr.GetOrdinal("EncryptionType")))
                                        formField.EncryptionType = (EncryptionType)dr.GetInt32(dr.GetOrdinal("EncryptionType"));
                                    break;
                                }
                            case FormDefFieldType.DateTime:
                                {
                                    formField.Required = dr.GetBoolean(dr.GetOrdinal("Required"));
                                    break;
                                }
                            case FormDefFieldType.Number:
                                {
                                    formField.Required = dr.GetBoolean(dr.GetOrdinal("Required"));
                                    formField.ZeroAsBlank = dr.GetBoolean(dr.GetOrdinal("ZeroAsBlank"));
                                    if (!dr.IsDBNull(dr.GetOrdinal("MinimumValue")))
                                        formField.MinimumValue = dr.GetDecimal(dr.GetOrdinal("MinimumValue"));
                                    if (!dr.IsDBNull(dr.GetOrdinal("MaximumValue")))
                                        formField.MaximumValue = dr.GetDecimal(dr.GetOrdinal("MaximumValue"));
                                    if (!dr.IsDBNull(dr.GetOrdinal("Decimals")))
                                    {
                                        formField.decimals = dr.GetInt32(dr.GetOrdinal("Decimals"));
                                        formField.decimalsAssigned = true;
                                    }
                                    break;
                                }
                            case FormDefFieldType.PickMultiple:
                            case FormDefFieldType.Pickone:
                                {
                                    formField.Required = dr.GetBoolean(dr.GetOrdinal("Required"));
                                    if (!dr.IsDBNull(dr.GetOrdinal("Style")))
                                        formField.Style = (FormDefFieldStyle)dr.GetInt32(dr.GetOrdinal("Style"));
                                    var optionsFunction = new ServerFunction(a, FunctionType.Options, "GetInternalOptions");
                                    formField.FieldFunctions.Add(optionsFunction);
                                    break;
                                }
                        }
                }
                finally
                {
                    dr.Close();
                }
            }
            formDef.Skins.Add(new FormSkin { skinType = SkinType.HTML, skin = formDef.GenerateSkin(a, false, null) });
            if (formDef.formName != null)
                AppPress.formDefs.Add(formDef);// ??? remove formDef after popup is closed or Reuse it
        }
        internal FormData(AppPress a, FormDef formDef, FieldValue containerFieldValue)
        {
            this.a = a;
            this.pageStackIndex = a.pageStackCount;

            this.containerFieldValue = containerFieldValue;
            this.formDefId = formDef.id;
            this.id = AppPress.GetUniqueId().ToString();
            this.formDef = formDef;
            foreach (var formField in this.formDef.formFields)
            {
                var fieldValue = formField.NewFieldValue(this.formDef);
                fieldValue.formField = formField;
                fieldValue.fieldDefId = formField.id;
                fieldValue.ReadOnly = FieldReadonlyType.None;
                fieldValue.NotFromClient = true;
                switch (formField.Type)
                {
                    case FormDefFieldType.Checkbox:
                        fieldValue.Value = "0";
                        break;
                    case FormDefFieldType.Pickone:
                    case FormDefFieldType.FileUpload:
                    case FormDefFieldType.DateTime:
                    case FormDefFieldType.Text:
                    case FormDefFieldType.TextArea:
                        fieldValue.Value = null;
                        break;
                    case FormDefFieldType.ForeignKey:
                        if (containerFieldValue != null)
                            fieldValue.Value = containerFieldValue.FormData.id;
                        break;
                }
                fieldValue.FormData = this;
                this.fieldValues.Add(fieldValue);
            }
            a.formDatas.Add(this);
        }
        internal FormData(AppPress a, long formDefId, FieldValue containerFieldValue, string Id)
            : this(a, AppPress.FindFormDef(formDefId), containerFieldValue, Id)
        {
        }
        internal FormData(AppPress a, FormDef formDef, FieldValue containerFieldValue, string Id)
        {
            var formData = a.LoadFormData(formDef.id, Id, containerFieldValue, null);
            a.CalcFormDatas(formData, null, true);
            this.fieldValues = formData.fieldValues;
            this.containerFieldValue = formData.containerFieldValue;
            this.formDef = formDef;
            this.formDefId = formData.formDefId;
            this.id = formData.id;
            this.pageStackIndex = formData.pageStackIndex;
            this.a = a;
            var i = a.formDatas.FindIndex(t => t == formData);
            a.formDatas[i] = this;
        }

        /// <summary>
        /// Hides the Fields in the Group identified by GroupName. Fields in nested FormContainers are also included. Groups are created in Form XML. <Fields Group="ABC">...</Fields>
        /// </summary>
        /// <param name="GroupName"></param>
        public void GroupHide(string GroupName)
        {
            var groupFound = false;
            for (int i = 0; i < fieldValues.Count; i++)
            {
                var fieldValue = fieldValues[i];
                if (fieldValue.formField.GroupName == GroupName)
                {
                    groupFound = true;
                    fieldValue.Hidden = FieldHiddenType.Hidden;
                    fieldValue.Refresh(a);
                }
            }
            if (!groupFound)
                throw new Exception(formDef.GetDescription() + " Cannot find field with Group Name: " + GroupName);
        }
        /// <summary>
        /// Shows the Fields in the Group identified by GroupName. Fields in nested FormContainers are also included. Groups are created in Form XML. <Fields Group="ABC">...</Fields>
        /// </summary>
        /// <param name="GroupName"></param>
        public void GroupShow(string GroupName)
        {
            var groupFound = false;
            for (int i = 0; i < fieldValues.Count; i++)
            {
                var fieldValue = fieldValues[i];
                if (fieldValue.formField.GroupName == GroupName)
                {
                    groupFound = true;
                    fieldValue.Hidden = FieldHiddenType.None;
                    fieldValue.Refresh(a);
                }
            }
            if (!groupFound)
                throw new Exception(formDef.GetDescription() + " Cannot find field with Group Name: " + GroupName);
        }
        /// <summary>
        /// Makes the Fields in the Group identified by GroupName Readonly. Fields in nested FormContainers are also included. Groups are created in Form XML. <Fields Group="ABC">...</Fields>
        /// </summary>
        /// <param name="GroupName"></param>
        public void GroupReadonly(string GroupName)
        {
            var groupFound = false;
            foreach (var fieldValue in fieldValues)
                if (fieldValue.formField.GroupName == GroupName)
                {
                    groupFound = true;
                    fieldValue.ReadOnly = FieldReadonlyType.Readonly;
                }
            if (!groupFound)
                throw new Exception(formDef.GetDescription() + " Cannot find field with Group Name: " + GroupName);
        }


        /// <summary>
        /// ???
        /// </summary>
        /// <param name="a"></param>
        /// <returns></returns>
        public string GetTableQuery(AppPress a)
        {
            var dateFormat = AppPress.Settings.SQLDateFormat;
            var q = "Select FullName(Employee.FirstName,Employee.MiddleName,Employee.LastName) Name, Employee.StaffId,";
            foreach (var fieldValue in fieldValues)
            {
                switch (fieldValue.formField.Type)
                {
                    case FormDefFieldType.Text:
                    case FormDefFieldType.TextArea:
                        q += "\ngroup_concat(case when SurveyQuestionId=" + fieldValue.formField.dbId + " then textValue else '' end SEPARATOR '') " + a.SQLQuote + "" + fieldValue.formField.Label + @"" + a.SQLQuote + ",";
                        break;
                    case FormDefFieldType.Pickone:
                        q += "\ngroup_concat(case when SurveyQuestionId=" + fieldValue.formField.dbId + " then (Select " + a.SQLQuote + "Option" + a.SQLQuote + " From " + a.SQLQuote + "application.formfields.pickone.options" + a.SQLQuote + " Where Id=intValue) else '' end SEPARATOR '') " + a.SQLQuote + "" + fieldValue.formField.Label + @"" + a.SQLQuote + ",";
                        break;
                    case FormDefFieldType.Number:
                        var c = "Round(decimalValue," + fieldValue.formField.decimals + ")";
                        q += "\ngroup_concat(case when SurveyQuestionId=" + fieldValue.formField.dbId + " then " + c + " else '' end SEPARATOR '') " + a.SQLQuote + "" + fieldValue.formField.Label + @"" + a.SQLQuote + ",";
                        break;
                    case FormDefFieldType.Checkbox:
                        q += "\ngroup_concat(case when SurveyQuestionId=" + fieldValue.formField.dbId + " then case when intValue =1 then 'yes' else 'no' end else '' end SEPARATOR '') " + a.SQLQuote + "" + fieldValue.formField.Label + @"" + a.SQLQuote + ",";
                        break;
                    case FormDefFieldType.DateTime:
                        q += "\ngroup_concat(case when SurveyQuestionId=" + fieldValue.formField.dbId + " then date_format(dateTimeValue,'" + dateFormat + "') else null end SEPARATOR '') " + a.SQLQuote + "" + fieldValue.formField.Label + @"" + a.SQLQuote + ",";
                        break;
                    case FormDefFieldType.PickMultiple:
                        q += "\ngroup_concat(case when SurveyQuestionId=" + fieldValue.formField.dbId + " then (Select Group_Concat((Select " + a.SQLQuote + "Option" + a.SQLQuote + " From " + a.SQLQuote + "application.formfields.pickone.options" + a.SQLQuote + " Where Id=" + a.SQLQuote + "application.formFields.pickone.options.id" + a.SQLQuote + ") SEPARATOR ',') From " + a.SQLQuote + "survey.answer.data.pickmultiple" + a.SQLQuote + " Where " + a.SQLQuote + "survey.answer.data.id" + a.SQLQuote + "=" + a.SQLQuote + "survey.answer.data" + a.SQLQuote + ".id) else '' end SEPARATOR '') " + a.SQLQuote + "" + fieldValue.formField.Label + @"" + a.SQLQuote + ",";
                        break;
                }
            }
            q = q.Substring(0, q.Length - 1);// remove trailing ,
            q += @"
                From " + a.SQLQuote + "survey.answer.data" + a.SQLQuote + @" 
                Left Outer Join " + a.SQLQuote + "survey.submit.data" + a.SQLQuote + " On " + a.SQLQuote + "survey.submit.data" + a.SQLQuote + ".id=" + a.SQLQuote + "survey.submit.data.id" + a.SQLQuote + @"
                Left Outer Join Employee On Employee.id=" + a.SQLQuote + "survey.submit.data" + a.SQLQuote + @".EmployeeId
                Where " + a.SQLQuote + "survey.submit.data" + a.SQLQuote + ".SurveyMasterId=" + formDef.dbId + @"
                Group By " + a.SQLQuote + "survey.submit.data.id" + a.SQLQuote + "";
            return q;
        }
        /// <summary>
        /// Get name of form definition for this formData  as defined in XML
        /// </summary>
        /// <returns>Name of Form</returns>
        public string GetFormName()
        {
            return formDef.formName;
        }
        /// <summary>
        /// Get name of Database Table as defined in XML
        /// </summary>
        /// <returns>Name of Table</returns>
        public string GetTableName()
        {
            return formDef.TableName;
        }
        /// <summary>
        /// if true the form data is not loaded from database
        /// </summary>
        public bool IsNew
        {
            get
            {
                return AppPress.IsNewId(id);
            }
        }
        /// <summary>
        /// if true the form data is not loaded from database
        /// </summary>
        public bool IsOriginalNew
        {
            get
            {
                return AppPress.IsNewId(originalId);
            }
        }
        /// <summary>
        /// Internal use only
        /// </summary>
        public FormData PopupContainer
        {
            get
            {
                if (!this.IsPopup)
                    throw new AppPressException("FormDataPopupCaller is available for Popup Form only");
                return (FormData)(this.callerFieldValue == null ? null : this.callerFieldValue.FormData.CovertToFormDefClass());
            }
        }
        /// <summary>
        /// Refresh the form in UI
        /// </summary>
        public void Refresh()
        {
            if (containerFieldValue == null)
            {
                // from Remote
                var clientAction = new AppPressResponse();
                clientAction.appPressResponseType = a.remoteLoginUserId == null ? AppPressResponseType.RefreshField : AppPressResponseType.RemoteRefreshMasterContentArea;
                var pCallReason = a.CallReason;
                var pFieldValue = a.fieldValue;
                try
                {
                    a.fieldValue = new FieldValue();
                    a.fieldValue.formField = new FormField();
                    a.fieldValue.FormData = this;
                    a.CallReason = CallReasonType.Refresh;
                    var str = formDef.GetSkin(a, false, false, null, SkinType.HTML, 0);
                    if (str == null)
                        throw new Exception("Could not find Begin or End Marker for field name: " + a.fieldValue.formField.fieldName + " in FormDef: " + a.fieldValue.FormData.formDef.formName);

                    //if (getData)
                    {
                        if (!this.IsNew)
                        {
                            var formData = FormData.InitializeFormData(a, this.formDef, this.id);
                            this.MergeFields(formData);
                            //a.fieldValue = a.fieldValue.FormData.fieldValues.Find(t => t.fieldDefId == a.fieldValue.fieldDefId);
                        }
                        a.CalcFormDatas(this, null, true);
                    }

                    clientAction.fieldHtml = Util.CompileSkin(a, str, false, SkinType.HTML, false);
                    clientAction.fieldHtml = Util.RemoveScripts(a, clientAction.fieldHtml);
                    clientAction.formDefId = formDef.id;
                    clientAction.id = this.id;
                    clientAction.JsStr = a.JsStr.ToString();

                }
                finally
                {
                    a.fieldValue = pFieldValue;
                    a.CallReason = pCallReason;
                }
                a.appPressResponse.Add(clientAction);
            }
            else
                a.appPressResponse.Add(AppPressResponse.RefreshField(a, containerFieldValue, true));

        }
        internal static FormData GetFormData(AppPress a)
        {
            foreach (var formData in a.formDatas)
                if (formData.formDefId == a.fieldValue.FormData.formDef.id && formData.id == a.fieldValue.FormData.id)
                    return formData;
            return null;
        }
        internal FieldValue GetFieldValue(string fieldName, string containerFieldName)
        {
            if (containerFieldName == null)
                return GetFieldValue(fieldName);
            List<FieldValue> f = fieldValues.FindAll(t => t != null && t.formField.fieldName == fieldName && t.formField.containerFormField.fieldName == containerFieldName);
            if (f.Count == 0)
                return null;
            if (f.Count > 1)
                throw new Exception("Found more than 1 fieldValue in GetFieldValue for FieldName: " + fieldName);
            return f[0];
        }

        /// <summary>
        /// intenal use only
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public FieldValue GetFieldValue(string fieldName)
        {
            var f = fieldValues.FindAll(t => t != null && t.formField.fieldName.Equals(fieldName, StringComparison.OrdinalIgnoreCase));
            if (f.Count == 0)
            {
                f = fieldValues.FindAll(t => t != null && fieldName.Equals(t.formField.UserControlScalarFieldName, StringComparison.OrdinalIgnoreCase));
                if (f.Count == 0)
                {
                    // if field is static then add it
                    var formField = formDef.GetFormField(fieldName);
                    if (formField != null && (formField.Static || formField.Type == FormDefFieldType.Button))
                    {
                        var fieldValue = formField.NewFieldValue(formDef);
                        fieldValue.FormData = this;
                        fieldValues.Add(fieldValue);
                        return fieldValue;
                    }
                    return null;
                }
            }
            if (f.Count > 1)
                throw new Exception("Found more than 1 fieldValue in GetFieldValue for FieldName: " + fieldName);
            return f[0];
        }
        internal FieldValue GetFieldValue(Int64 fieldId)
        {
            var fieldName = formDef.formFields.Find(t => t.id == fieldId).fieldName;
            return GetFieldValue(fieldName);
        }
        internal int GetFieldInt(string fieldName, int? defaultValue)
        {
            var f = GetFieldValue(fieldName);
            if (f == null || f.Value == null)
                if (defaultValue == null)
                    throw new Exception("Internal Error: Could not find int value in field value for Field: " + fieldName);
                else
                    return (int)defaultValue;
            return int.Parse(f.Value);
        }
        internal int? GetFieldInt(string fieldName)
        {
            var f = GetFieldValue(fieldName);
            if (f.Value == null)
                return null;
            return int.Parse(f.Value);
        }
        internal DateTime? GetFieldDateTime(string fieldName)
        {
            var f = GetFieldValue(fieldName);
            if (f == null || f.Value == null)
                return null;
            return DateTime.Parse(f.Value);
        }
        internal void SetFieldDateTime(string fieldName, DateTime? dateTime)
        {
            var f = GetFieldValue(fieldName);
            if (dateTime == null)
                f.Value = null;
            else
                f.Value = ((DateTime)dateTime).ToString(DAOBasic.DBDateTimeFormat);
        }
        internal void SetFieldInt(string fieldName, int? val)
        {
            var f = GetFieldValue(fieldName);
            if (val == null)
                f.Value = null;
            else
                f.Value = val.ToString();
        }
        internal void SetFieldDecimal(string fieldName, decimal? val)
        {
            var f = GetFieldValue(fieldName);
            if (val == null)
                f.Value = null;
            else
                f.Value = val.ToString();
        }
        internal decimal? GetFieldDecimal(string fieldName)
        {
            var f = GetFieldValue(fieldName);
            if (f.Value == null)
                return null;
            return decimal.Parse(f.Value);
        }
        internal List<int> GetFieldPickMultiple(string fieldName)
        {
            var f = GetFieldValue(fieldName);
            if (f.Value == null)
                return null;
            var values = f.Value.Split(new char[] { ',' });
            var l = new List<int>();
            foreach (var value in values)
                if (!value.IsNullOrEmpty())
                    l.Add(int.Parse(value));
            return l;
        }
        internal void SetFieldPickMultiple(string fieldName, List<int> l)
        {
            if (l == null)
                l = new List<int>();
            var f = GetFieldValue(fieldName);
            f.Value = string.Join(",", l);
        }
        internal List<FormData> GetFieldFormContainer(string fieldName)
        {
            a.CheckContainerfieldValues();

            var f = GetFieldValue(fieldName);
            if (f == null)
                return null;
            return a.formDatas.FindAll(t => t.containerFieldValue == f && !t.IsDeleted);
        }

        internal string GetFieldString(string fieldName)
        {
            var f = GetFieldValue(fieldName);
            if (f == null)
                return null;
            return f.Value;
        }
        public FieldValue SetFieldValue(string fieldName, string value)
        {
            foreach (var fieldValue in fieldValues)
                if (fieldValue.formField.fieldName.Equals(fieldName, StringComparison.OrdinalIgnoreCase))
                {
                    fieldValue.Value = value;
                    return fieldValue;
                }
            throw new Exception("Internal Error: SetFieldValue, Could not find Field Name:" + fieldName);
        }
        public FieldValue TrySetFieldValue(string fieldName, string value)
        {
            foreach (var fieldValue in fieldValues)
                if (fieldValue.formField.fieldName.Equals(fieldName, StringComparison.OrdinalIgnoreCase))
                {
                    fieldValue.Value = value;
                    return fieldValue;
                }
            return null;
        }
        internal bool _ValidateAll()
        {
            var valid = _Validate();
            foreach (var fieldValue in fieldValues.FindAll(t => t.formField.Type == FormDefFieldType.FormContainerDynamic))
                foreach (var formData in a.formDatas.FindAll(t => t.containerFieldValue == fieldValue))
                    if (!formData._ValidateAll())
                        valid = false;
            return valid;
        }
        /// <summary>
        /// Validate the Form Data for Validations like Required specified on fields in form definiton
        /// </summary>
        public void Validate()
        {
            a.ClearErrors();
            var result = _Validate();
            if (!result)
            {
                a.StopExecution = true;
                throw new AppPressException();
            }
        }

        internal bool _Validate()
        {
            bool result = true;

            for (int i = 0; i < fieldValues.Count; i++)
            {
                var fieldValue = fieldValues[i];
                result = fieldValue._Validate() & result;
                if ((i + 1) < fieldValues.Count)
                    if (fieldValue.formField.OriginalType == (int)FormDefFieldType.DateRange && fieldValue.formField.IsDateRange == 1 && fieldValues[i + 1].formField.IsDateRange == 2)
                    {
                        var dateFrom = GetFieldDateTime(fieldValue.formField.fieldName);
                        var dateTo = GetFieldDateTime(fieldValues[i + 1].formField.fieldName);
                        if (dateFrom.HasValue && dateTo.HasValue)
                            if (dateFrom.Value > dateTo.Value)
                                throw new AppPressException(fieldValue.formField.GetDisplayName(false) + " should be less than " + fieldValues[i + 1].formField.GetDisplayName(false) + ".");
                    }
            }

            return result;
        }
        internal static FormData NewFormData(AppPress a, long formDefId)
        {
            return NewFormData(a, AppPress.FindFormDef(formDefId), null);
        }
        internal static FormData NewFormData(AppPress a, FormDef formDef, FieldValue containerFieldValue)
        {
            FormData formData = null;
            if (AppPress.Assemblies.Count == 0)
                formData = new FormData(); // comes here from Code Generation
            else
            {
                //formData = (FormData)assembly.assembly.CreateInstance(assembly.namespaceName + "." + className, true, System.Reflection.BindingFlags.CreateInstance, null, new object[] { a }, null, null);
                var type = formDef.GetTopClassType();
                if (type != null) // null for User Forms
                    formData = (FormData)Activator.CreateInstance(type, new object[] { a });

            }
            if (formData == null)
            {
                // comes here for User Generated Forms like AssetType
                formData = new FormData(a, formDef, containerFieldValue);
            }
            formData.id = AppPress.GetUniqueId().ToString();
            //It will be added to p in caller.
            a.formDatas.RemoveAll(t => t == formData);
            formData.pageStackIndex = a.pageStackCount;
            formData.formDef = formDef;
            formData.formDefId = formDef.id;
            if (containerFieldValue != null)
            {
                var containerIdFormField = formDef.formFields.Find(t => t.Type == FormDefFieldType.ForeignKey);
                if (containerIdFormField != null)
                    formData.SetFieldValue(containerIdFormField.fieldName, containerFieldValue.FormData.id);
            }
            return formData;
        }
        /// <summary>
        /// internal use only
        /// </summary>
        /// <param name="formData"></param>
        /// <returns></returns>
        public FormData CovertToFormDefClass()
        {
            var type = this.formDef.GetTopClassType();
            if (type == null || this.GetType() == type)
                return this;
            return (FormData)Activator.CreateInstance(type, new object[] { this });
        }
        internal static List<FormData> ReadFormDatas(AppPress a, FieldValue fieldValueFormContainer, FormDef formDef, string query)
        {
            var formDatas = new List<FormData>();
            var dr = a.ExecuteQuery(query);
            try
            {
                int idOrd = formDef.PrimaryKey == null ? -1 : DAOBasic.TryGetOrdinal(dr, formDef.PrimaryKey);
                // throw error if the row has SelectRow field but not return Id column in domain query
                if (formDef.TableName != null && idOrd == -1 && formDef.GetFormField("SelectRow") != null)
                    throw new Exception("Form: " + formDef.formName + " has a Select Row Field, but the domain query for " + formDef.ContainerFormField.GetDescription() + " return a domain query which does not have a " + (formDef.PrimaryKey ?? "Id") + " Column");
                while (dr.Read())
                {
                    var formData = FormData.NewFormData(a, formDef, null);
                    formDatas.Add(formData);
                    if (idOrd != -1)
                        formData.id = dr.IsDBNull(idOrd) ? AppPress.GetUniqueId().ToString() : dr[idOrd].ToString();
                    foreach (var formField in formDef.formFields)
                    {
                        if (formField.Type == FormDefFieldType.FormContainerDynamic || formField.Type == FormDefFieldType.FormContainerGrid || formField.Type == FormDefFieldType.UserControlScalar || formField.Type == FormDefFieldType.MergedForm)
                            continue;
                        FieldValue fieldValue = formData.GetFieldValue(formField.fieldName);
                        if (fieldValue == null)
                        {
                            fieldValue = new FieldValue();
                            fieldValue.formField = formField;
                            fieldValue.FormData = formData;
                            formData.fieldValues.Add(fieldValue);
                        }
                        // do not assume all fields will be in query
                        var fieldName = fieldValue.formField.fieldName;
                        int ford = DAOBasic.TryGetOrdinal(dr, fieldName);
                        if (ford == -1)
                        {
                            //Load selectRow checkbox Data

                            //if (fieldValue.formField.fieldName.Equals("SelectRow", StringComparison.OrdinalIgnoreCase))
                            //{
                            //    if (fieldValue.FormData.containerFieldValue != null && fieldValue.FormData.containerFieldValue == a.fieldValue)
                            //    {
                            //        var domainfun = a.fieldValue.formField.GetFieldFunctions(FunctionType.Domain).Find(t => t.Parameters.Find(t1 => t1.Name.Equals("TableName", StringComparison.OrdinalIgnoreCase)) != null);
                            //        if (domainfun != null && a.fieldValue.FormData.id > 0)
                            //        {
                            //            var tblname = domainfun.Parameters.Find(t => t.Name.Equals("TableName", StringComparison.OrdinalIgnoreCase)).Value;
                            //            DAOBasic site1 = new DAOBasic();
                            //            IDataReader dr1 = site1.ExecuteQuery("Select fieldId from " + tblname + " where formid = " + a.fieldValue.FormData.id);
                            //            try
                            //            {
                            //                while (dr1.Read())
                            //                {
                            //                    if (dr1.GetInt64(0) == fieldValue.FormData.id)
                            //                    {
                            //                        fieldValue.Value = "1";
                            //                        break;
                            //                    }
                            //                }
                            //            }
                            //            finally
                            //            {
                            //                dr1.Close();
                            //                site1.Close();
                            //            }
                            //        }
                            //    }

                            //}
                            continue;
                        }
                        fieldValue.Value = formField.GetFieldValueFromDB(a, dr, ford, fieldValue);
                    }
                }

            }
            finally
            {
                dr.Close();
            }
            foreach (var formData in formDatas)
                foreach (var fieldValue in formData.fieldValues)
                    switch (fieldValue.formField.Type)
                    {
                        case FormDefFieldType.PickMultiple:
                            {
                                if (fieldValue.formField.SaveTableName != null)
                                {
                                    var values = a.ExecuteStringList("Select " + a.SQLQuote + fieldValue.formField.fieldName + a.SQLQuote + " From " + a.SQLQuote + fieldValue.formField.SaveTableName + a.SQLQuote + " Where " + a.SQLQuote + fieldValue.formField.SaveTableForeignKey + a.SQLQuote + "=" + formData.id);
                                    fieldValue.Value = values == null ? null : string.Join(",", values);
                                }
                                break;
                            }
                    }
            return formDatas;
        }

        internal static FormData InitializeFormData(AppPress a, FormDef formDef, string id)
        {
            var query = formDef.GetViewQuery(a);
            int intid;
            var read = !int.TryParse(id, out intid) || intid > 0;
            if (query != null && read)
            {
                query = "Select * From (" + query + ") As A Where " + a.SQLQuote + formDef.PrimaryKey + a.SQLQuote + "='" + id + "'";
                var formDatas = ReadFormDatas(a, null, formDef, query);
                if (formDatas.Count() == 0)
                    throw new Exception("Could not find Form with " + formDef.PrimaryKey + ":" + id + " in table :" + formDef.TableName);
                // check if 2 rows with same Id exist in table or view
                if (formDatas.Count() > 1)
                    throw new Exception("In Table/View: " + formDef.TableName + " Found 2 rows with " + formDef.PrimaryKey + ":" + id);
                var formData = formDatas.First();
                formData.a = a;

                foreach (var fieldValue in formData.fieldValues)
                {
                    switch (fieldValue.formField.Type)
                    {
                        case FormDefFieldType.DateTime:
                            // Disable DateTo if this is last row in Modify as it cannot be modifed
                            if (fieldValue.formField.Contiguous && fieldValue.formField.IsDateRange != 0 && fieldValue.formField.fieldName.EndsWith("To") && fieldValue.Value == null)
                                fieldValue.Hidden = FieldHiddenType.Hidden;
                            break;
                    }
                }
                return formData;
            }
            else
            {
                var formData = FormData.NewFormData(a, formDef, null);
                formData.id = id;
                foreach (var fieldValue in formData.fieldValues)
                {
                    switch (fieldValue.formField.Type)
                    {
                        case FormDefFieldType.DateTime:
                            // Disable DateTo and default to null for new row to be added
                            fieldValue.Value = null;
                            if (fieldValue.formField.Contiguous && fieldValue.formField.IsDateRange != 0 && fieldValue.formField.fieldName.EndsWith("To"))
                                fieldValue.Hidden = FieldHiddenType.Hidden;
                            break;
                    }
                }
                return formData;
            }
        }


        private static ulong GetTransactionId(IDataReader dr)
        {
            ulong transactionId = 0;
            for (int i = 0; i < dr.FieldCount; i++)
            {
                if (dr.GetName(i).Equals("TransactionId", StringComparison.OrdinalIgnoreCase))
                {
                    if (!dr.IsDBNull(i))
                        transactionId = (ulong)dr.GetInt64(i);
                    break;
                }
            }
            return transactionId;

        }

        internal static void ParseCFV(string cfv, out long containerFomDefId, out string containerid, out long containerFieldDefId, out long callerFomDefId, out string callerId, out long callerFieldDefId)
        {
            var atIndex = cfv.IndexOf("@");
            containerFomDefId = -1;
            containerid = null;
            containerFieldDefId = -1;
            callerFomDefId = -1;
            callerId = null;
            callerFieldDefId = -1;

            if (atIndex != 0)
            {
                var cfvc = cfv;
                if (atIndex != -1)
                    cfvc = cfvc.Substring(0, atIndex);
                string[] cfvs = cfvc.Split(AppPress.IdSep.ToCharArray());
                containerFomDefId = long.Parse(cfvs[0]);
                containerid = cfvs[1];
                containerFieldDefId = long.Parse(cfvs[2]);
            }
            if (atIndex != -1)
            {
                cfv = cfv.Substring(atIndex + 1);
                string[] cfvs = cfv.Split(AppPress.IdSep.ToCharArray());
                callerFomDefId = long.Parse(cfvs[0]);
                callerId = cfvs[1];
                callerFieldDefId = long.Parse(cfvs[2]);
            }
        }
        internal void SetContainerFormData(AppPress a)
        {
            if (cfv != null)
            {
                long containerFomDefId;
                string containerid;
                long containerFieldDefId;
                long callerFomDefId;
                string callerId;
                long callerFieldDefId;
                ParseCFV(cfv, out containerFomDefId, out containerid, out containerFieldDefId, out callerFomDefId, out callerId, out callerFieldDefId);
                var atIndex = cfv.IndexOf("@");
                if (containerid != null)
                {
                    var containers = a.formDatas.FindAll(t => t.formDefId == containerFomDefId && t.id == containerid);
                    if (containers.Count > 1)
                        throw new Exception("Internal Error: Found more than 1 container for Form: " + formDefId + " Id: " + id);
                    if (containers.Count == 1) // count is 0 for remote forms
                        containerFieldValue = containers[0].GetFieldValue(containerFieldDefId);
                }
                if (atIndex != -1)
                {
                    var containers = a.formDatas.FindAll(t => t.formDefId == callerFomDefId && t.id == callerId);
                    if (containers.Count > 1)
                        throw new Exception("Internal Error: Found more than 1 caller for Form: " + formDefId + " Id: " + id);
                    if (containers.Count == 1) // count is 0 for remote forms
                        callerFieldValue = containers[0].GetFieldValue(callerFieldDefId);
                    if (callerFieldValue == null)
                    {
                        // buttons and static fields are not submitted. Create is needed

                        var formField = containers[0].formDef.GetFormField(callerFieldDefId);
                        var className = formField.GetClassName();
                        callerFieldValue = AppPress.CreateFieldValue(className);
                        callerFieldValue.formField = formField;
                        callerFieldValue.fieldDefId = callerFieldDefId;
                        callerFieldValue.FormData = containers[0];
                        callerFieldValue.NotFromClient = true;
                        containers[0].fieldValues.Add(callerFieldValue);
                    }
                }
            }

        }

        internal string GetHtml(AppPress a, bool forceStatic, bool popup, string skin, SkinType skinType)
        {
            if (skin == null)
                skin = formDef.GetSkin(a, forceStatic, popup, null, skinType, 0);
#if DEBUG
            a.fieldsNotGenerated = new List<FieldValue>();
#endif
            var html = Util.CompileSkin(a, skin, false, skinType, false);
            if (AppPress.Settings.DEBUG && skinType == SkinType.HTML)
            {
                if (containerFieldValue == null || containerFieldValue.formField.GetContainerRowFormDef(a) == null)
                {
                    if (a.LinksGenerated.Find(t => t == formDef.formName) == null)
                    {
                        var baseUrl = AppPress.GetBaseUrl() + AppPress.GetDefaultAspx();
                        html += formDef.GenerateDeveloperLinks(a);
                        a.LinksGenerated.Add(formDef.formName);
                    }
                }
            }

            return html;
        }

        private string RandomString(int size, bool lowerCase)
        {
            StringBuilder builder = new StringBuilder();
            Random random = new Random();
            char ch;
            for (int i = 0; i < size; i++)
            {
                ch = Convert.ToChar(Convert.ToInt32(Math.Floor(26 * random.NextDouble() + 65)));
                builder.Append(ch);
            }
            if (lowerCase)
                return builder.ToString().ToLower();
            return builder.ToString();
        }

        internal void SetFocus(AppPress a)
        {
            if (IsNew)
                foreach (var fieldValue in fieldValues)
                {
                    if (fieldValue.formField.Static)
                        continue;
                    switch (fieldValue.formField.Type)
                    {
                        case FormDefFieldType.Text:
                        case FormDefFieldType.TextArea:
                        case FormDefFieldType.Pickone:
                        case FormDefFieldType.Password:
                        case FormDefFieldType.Number:
                            a.appPressResponse.Add(AppPressResponse.SetFocus(formDefId, id, fieldValue.formField));
                            return;
                    }
                }

        }
        internal FormData Clone()
        {
            FormData f = new FormData();
            foreach (var fieldValue in fieldValues)
            {
                var fv = (FieldValue)Activator.CreateInstance(fieldValue.GetType(), new object[] { });
                fv.formField = fieldValue.formField;
                fv.FormData = f;
                fv.Value = fieldValue.Value;
                f.fieldValues.Add(fv);
            }
            f.formDefId = formDefId;
            f.formDef = formDef;
            f.id = id;
            f.callerFieldValue = callerFieldValue;
            f.a = a;
            return f;
        }

        internal List<FieldValue> SerializableFields(AppPress a, bool forRedirect = false)
        {
            cfv = null;
            if (containerFieldValue != null)
            {
                cfv = containerFieldValue.FormData.formDefId + AppPress.IdSep + containerFieldValue.FormData.id + AppPress.IdSep + containerFieldValue.formField.id;
            }
            if (callerFieldValue != null)
            {
                if (cfv == null)
                    cfv = "";
                cfv += "@" + callerFieldValue.FormData.formDefId + AppPress.IdSep + callerFieldValue.FormData.id + AppPress.IdSep + callerFieldValue.formField.id;
            }

            // do not send static fields and formData with no non static fields to client
            var fieldValues1 = new List<FieldValue>();
            for (int i = 0; i < fieldValues.Count; i++)
            {
                var fieldValue1 = fieldValues[i];
                // need to send static fields with non default properties as those properties cannot be read from DB
                if (!fieldValue1.formField.Hidden && fieldValue1.formField.Static && !fieldValue1.formField.NonStatic/* && !fieldValue1.Modified*/ && !fieldValue1.formField.StaticSubmitValue)
                    continue;
                if (fieldValue1.formField.Type == FormDefFieldType.Button)
                    if (fieldValue1.Hidden == FieldHiddenType.None && fieldValue1.ReadOnly == FieldReadonlyType.None && !fieldValue1.formField.StaticSubmitValue) //This is done for manula HTML manipulation from browser console. Readonly field has to be disable in JS
                        continue;
                //if (fieldValue1.formField.Type == FormDefFieldType.FormContainerDynamic && !fieldValue1.ReArranged)
                //    // not needed in client side. Reconstructed on Post back to server
                //    continue;
                if (a.formFields != null)
                    if (a.formFields.Find(t => t.id == fieldValue1.formField.id) == null)
                    {
                        var formField = new FormFieldJS();
                        formField.id = fieldValue1.formField.id;
                        formField.Type = fieldValue1.formField.Type;
                        formField.Style = fieldValue1.formField.Style;
                        formField.Static = fieldValue1.formField.Static;
                        a.formFields.Add(formField);
                    }
                try
                {
                    if (Math.Abs(fieldValue1.FormData.formDef.id) % 10 == AppPress.LocalInstanceId)
                        fieldValue1.security = fieldValue1.GetSecurityKey(a);
                }
                catch (Exception)
                {
                    // error here should have reflected earlier. throw here will hide the Error Response.
                    // To rethrow the error and set Wrong Table Name for Pickone in Application Manager
                    fieldValue1.security = null;
                    throw;
                }
                fieldValues1.Add(fieldValue1);
            }
            return fieldValues1;
        }

        /// <summary>
        /// internal use only
        /// </summary>
        /// <param name="a"></param>
        /// <param name="popupParams"></param>
        public void Popup(AppPress a, PopupParams popupParams)
        {
            var formData = this;
            if (a.formDatas.Find(t => t == this) == null)
                a.formDatas.Add(this);
            if (popupParams == null || !popupParams.forRedirect)
                a.pageStackCount++;
            this.callerFieldValue = a.fieldValue;
            a.CalcFormDatas(formData, null, true);
            var popup = AppPressResponse.Popup(a, formData, popupParams);

            if (popupParams != null)
            {
                popup.popupWidth = popupParams.PopupWidth;
                popup.popupHeight = popupParams.PopupHeight == null ? "auto" : popupParams.PopupHeight.ToString();
                popup.popupPosition = popupParams.PopupPosition;
                if (popupParams.title != null)
                    popup.popupTitle = popupParams.title;
            }
            a.appPressResponse.Add(popup);
        }
        /// <summary>
        /// internal use only
        /// </summary>
        /// <param name="a"></param>
        /// <param name="redirectParams"></param>
        public void Redirect(AppPress a, RedirectParams redirectParams)
        {
            if (redirectParams == null)
                redirectParams = new RedirectParams();
            a.appPressResponse.Add(AppPressResponse.Redirect(a, formDef.id, id, redirectParams));
        }
        /// <summary>
        /// Save the Form Data. 
        /// Should be called inside a Transaction. See help for AppPress Transactions in documentation
        /// </summary>
        public void Save(AppPress a)
        {
            //if (a.site.trans == null)
            //    throw new Exception("Save should be called inside a Transaction. See help for Transactions in <a href='" + GetAppPressReferenceURL("Transactions") + "'>AppPress Documentation</a>");
            var pFieldValue = a.fieldValue;
            try
            {
                a.fieldValue = new FieldValue();
                a.fieldValue.FormData = this;
                AppPressLogic.SaveForm(a, this);
            }
            finally
            {
                a.fieldValue = pFieldValue;
            }
        }

        private string GetAppPressReferenceURL(string subject)
        {
            var u = "https://docs.google.com/document/d/1FQpF6Mef8cKMIGsYv-8dG1aOCi3zkKu-zteuWC6Zyj0/edit";
            switch (subject)
            {
                case "Transactions":
                    return u + "#bookmark=id.caoo3bq2qm8s";
            }
            return u;
        }
        /// <summary>
        /// Save the FormData containing this button. Also saves Forms contained in FormContainerFields in the form
        /// </summary>
        /// <param name="p">Universal AppPress Object</param>
        /// <param name="SuccessMessage">Alert to be shown on successfull save.</param>
        public void SaveAndUpdateUI(AppPress a, string SuccessMessage = null)
        {
            AppPressLogic.SaveForm(a, a.fieldValue.FormData);
            if (a.pageStackCount > 0)
            {
                // inside popup
                if (PopupContainer != null)
                {
                    a.ClosePopup();
                    UpdateUI(a);
                }
                else
                {
                    if (SuccessMessage != null)
                        a.AlertMessage(SuccessMessage);
                    Refresh();
                }
            }
            else
            {
                if (SuccessMessage != null)
                    a.AlertMessage(SuccessMessage);
                Refresh();
            }
        }

        /// <summary>
        /// After calling Save call this to refresh the grid from which this popup was invoked. 
        /// Normally used when Save of PopupFields in FoormContainerGrid is overridden by add in a OnClick event
        /// </summary>
        /// <param name="a"></param>
        public void UpdateUI(AppPress a)
        {
            if (a.pageStackCount > 0 && (a.pageStackCount == 1 ? a.MasterContentAreaInstanceId : a.PopupDatas[a.pageStackCount - 1].InstanceId) != AppPress.LocalInstanceId)
            {
                // refresh remote page
                var ar = new AppPressResponse();
                ar.formDefId = a.PopupDatas[a.pageStackCount].callerFormDefId;
                ar.fieldDefId = a.PopupDatas[a.pageStackCount].callerFieldDefId;
                ar.id = a.PopupDatas[a.pageStackCount].callerFormDataId;
                ar.instanceId = a.PopupDatas[a.pageStackCount - 1].InstanceId;
                ar.appPressResponseType = AppPressResponseType.RemoteRefresh;
                a.appPressResponse.Add(ar);
                return;
            }
            if (callerFieldValue == null)
                return;
            a.RefreshContainer(callerFieldValue);
        }
        /// <summary>
        /// Delete the FormData. If form was loaded from Database. deletes the form from database also
        /// </summary>
        public void Delete(AppPress a)
        {
            var deletedFormDatas = new List<FormData>();
            deletedFormDatas.Add(this);
            if (!IsNew)
                AppPressLogic.DeleteForms(a, deletedFormDatas, true);
            IsDeleted = true;
        }

        internal void MergeFields(FormData newFormData, FieldValue fieldValue1 = null)
        {
            var removedFields = fieldValues.FindAll(t => (t.ReadOnly == FieldReadonlyType.Readonly || t.formField.Static) && t.formField.Type != FormDefFieldType.FormContainerDynamic && (fieldValue1 == null || fieldValue1.fieldDefId == t.fieldDefId));
            fieldValues.RemoveAll(t => (t.ReadOnly == FieldReadonlyType.Readonly || t.formField.Static) && t.formField.Type != FormDefFieldType.FormContainerDynamic && (fieldValue1 == null || fieldValue1.fieldDefId == t.fieldDefId));
            var newFields = newFormData.fieldValues.FindAll(t => fieldValues.Find(t1 => t1.formField.fieldName == t.formField.fieldName) == null);
            foreach (var fieldValue in newFields)
            {
                var of = removedFields.Find(t => t.formField.fieldName == fieldValue.formField.fieldName);
                if (of != null)
                {
                    fieldValue.ReadOnly = of.ReadOnly;
                    fieldValue.Hidden = of.Hidden;
                }
                fieldValue.FormData = this;
            }
            fieldValues.AddRange(newFields);
        }

        internal void ChangeIDInClient(AppPress a, string oid, string newId)
        {
            foreach (var fieldValue in fieldValues)
            {
                var response = new AppPressResponse();
                response.appPressResponseType = AppPressResponseType.ChangeFormDataId;
                response.formDefId = formDefId;
                response.id = oid;
                response.Value = newId;
                response.message = ((int)fieldValue.formField.Type).ToString();
                response.fieldDefId = fieldValue.fieldDefId;
                a.appPressResponse.Add(response);
            }
        }
    }
}

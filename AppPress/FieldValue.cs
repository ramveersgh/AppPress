using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Globalization;
using System.Web;
using System.Reflection;
using System.Security.Cryptography;
using System.Runtime.Serialization;
using System.IO;


namespace AppPressFramework
{

    [DataContract]
    public class FieldValue
    {
        /// <summary>
        /// Set the label to be diplayed for the field in UI
        /// </summary>
        public string FieldLabel;
        internal bool DoNotSaveInDB = false;
        [DataMember]
        public bool Required = false;
        /// <summary>
        /// Value of the Field
        /// </summary>
        [DataMember]
        public string Value = null;
        /// <summary>
        /// ???
        /// </summary>
        [DataMember]
        public long fieldDefId;
        /// <summary>
        /// Readonly property of the field
        /// </summary>
        [DataMember]
        public FieldReadonlyType ReadOnly;
        /// <summary>
        [DataMember]
        internal string security;
        /// <summary>
        /// Hide the field. 
        /// Fields are hidden by removing the HTML of the field. On Browser no html will be seen for hidden fields
        /// If all cells of a column are hidden, the whole column is hidden.
        /// </summary>
        [DataMember]
        public FieldHiddenType Hidden;
        /// <summary>
        /// To overrite Case Style of Text and TextArea. Only valid for Case Styles
        /// </summary>
        [DataMember]
        public FormDefFieldStyle Style = FormDefFieldStyle.None;
        /// <summary>
        /// ControlStyle Property of the field
        /// </summary>
        public string ControlStyle = null;
        /// <summary>
        /// LabelStyle Property of the field
        /// </summary>
        public string LabelStyle = null;
        /// <summary>
        /// title property of the field
        /// </summary>
        public string Title = null;
        /// <summary>
        /// internal use only
        /// </summary>
        public FormData FormData; // points to formData containing this fieldValue
        public FormField formField; // points to the formField representing the fieldValue
        internal bool ReArranged = false;
        internal bool Modified = false;
        internal string MetaData = null;// for select contains comma seperated values in select input
        internal bool NotFromClient;
        public FieldValue()
        { }
        /// <summary>
        /// If the field is Static. Displayed as Text
        /// Can be used in Options query to optimize the query to return the row having the val
        /// </summary>
        public bool IsStatic { get { return formField.Static; } }
        public FieldValue(FieldValue fieldValue)
        {
            Type t = typeof(FieldValue);
            foreach (FieldInfo fieldInf in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                fieldInf.SetValue(this, fieldInf.GetValue(fieldValue));
            }
            var idx = FormData.fieldValues.FindIndex(t1 => t1.fieldDefId == fieldValue.fieldDefId);
            if (idx == -1)
                throw new Exception(fieldValue.GetFieldDescription() + " Internal Error: Could not Clone");
            FormData.fieldValues.RemoveAt(idx);
            FormData.fieldValues.Insert(idx, this);
            var a = FormData.a;
            //if (a.fieldValue == fieldValue)
            //{
            //    a.fieldValue = this;
            //}
            //foreach (var f in a.formDatas)
            //    if (f.containerFieldValue == fieldValue)
            //        f.containerFieldValue = this;

        }
        /// <summary>
        /// Error property of the field. 
        /// </summary>
        public string Error
        {
            set
            {
                var fieldError = FormData.a.appPressResponse.Find(t => t.appPressResponseType == AppPressResponseType.FieldError && t.fieldDefId == formField.id);
                if (fieldError == null)
                    FormData.a.appPressResponse.Add(AppPressResponse.FieldError(this, value));
                else
                    fieldError.message = value;
            }
            get
            {
                var ar = FormData.a.appPressResponse.Find(t => t != null && t.appPressResponseType == AppPressResponseType.FieldError && t.fieldDefId == formField.id);
                if (ar != null)
                    return ar.message;
                return null;
            }
        }
        /// <summary>
        /// Error property of the field. 
        /// </summary>
        public string Help
        {
            set
            {
                if (Hidden == FieldHiddenType.None)
                {
                    var fieldHelp = FormData.a.appPressResponse.Find(t => t.appPressResponseType == AppPressResponseType.FieldHelp && t.fieldDefId == formField.id && t.id == FormData.id);
                    var fieldHelpText = formField.Help;
                    if (value != null)
                    {
                        if (!fieldHelpText.IsNullOrEmpty())
                            fieldHelpText += "<br/>";
                        fieldHelpText += value;
                    }
                    if (fieldHelp == null)
                        FormData.a.appPressResponse.Add(AppPressResponse.FieldHelp(this, fieldHelpText));
                    else
                        fieldHelp.message = fieldHelpText;
                }
            }
            get
            {
                var ar = FormData.a.appPressResponse.Find(t => t != null && t.appPressResponseType == AppPressResponseType.FieldHelp && t.fieldDefId == formField.id);
                if (ar != null)
                    return ar.message;
                return null;
            }
        }
        public decimal? GetMinimumValue()
        {
            return formField.MinimumValue;
        }
        public decimal? GetMaximumValue()
        {
            return formField.MaximumValue;
        }
        public FormDefFieldType GetFieldType()
        {
            return formField.Type;
        }
        public string GetFieldName()
        {
            return formField.fieldName;
        }
        /// <summary>
        /// Set Focus to the field
        /// </summary>
        public void SetFocus()
        {
            FormData.a.appPressResponse.Add(AppPressResponse.SetFocus(FormData.formDefId, FormData.id, formField));
        }

        /// <summary>
        /// ToString method has been overwridden to give error. Force user to use .val
        /// </summary>
        /// <returns></returns>
        //public override string ToString()
        //{
        //    return "You are not allowed to convert AppPress Field to string. To get value of a Field use val property of the field";
        //}
        internal void SetFieldValue(string value)
        {
            FormData.a.appPressResponse.Add(AppPressResponse.SetFieldValue(FormData.formDefId, FormData.id, formField, value));
        }
        /// <summary>
        /// Do a Required Validation on the Field
        /// </summary>
        /// <param name="message">Message to show if the validation fails</param>
        public void RequiredValidation(string message = null)
        {
            if (Value == null)
            {
                if (message.IsNullOrEmpty())
                    message = AppPress.GetLocalizationKeyValue("LAKey_RequiredMsg");
                var a = FormData.a;
                a.appPressResponse.Add(AppPressResponse.FieldError(this, message));
                throw new AppPressException();
            }
        }
        internal string GetFormattedFieldValue(AppPress a, DateTime dateTime)
        {
            if (Value.IsNullOrEmpty())
                return "";
            return dateTime.ToString("d", new CultureInfo("en-gb"));
        }
        /// <summary>
        /// Get formatted field value for the field
        /// </summary>
        /// <param name="a"></param>
        /// <param name="htmlFormat">if true encodes html characters like < to &lt; for correct display as html</param>
        /// <returns></returns>
        public string GetFormattedFieldValue(AppPress a, bool htmlFormat = true, string DateFormat = null)
        {
            if (Value == null && formField.Static && !formField.StaticSubmitValue && !(a.CallReason == CallReasonType.PageLoad || a.CallReason == CallReasonType.Refresh))
                throw new Exception("Form: " + formField.formDef.formName + " Field: " + formField.fieldName + " is of type Static. To use its value outside of Init, Add attribute &lt;Static SubmitValue=\"true\"/&gt to the Field in XML");
            switch (formField.Type)
            {
                case FormDefFieldType.DateTime:
                    {
                        var dateTimeFieldValue = (DateTimeFieldValue)this;
                        if (Value.IsNullOrEmpty())
                            return "";
                        if (formField.Style == FormDefFieldStyle.Time && dateTimeFieldValue.BaseDate != null)
                        {
                            //if (dateTimeFieldValue.BaseDateTime == null)
                            //    throw new Exception("Form: " + formField.formDef.formName + " Field: " + formField.formDef.formName + ". Cannot have BaseDateTime null for DateTime field of Style: Time");
                            var dateTime = DateTime.Parse(Value);
                            TimeSpan t = dateTime - dateTimeFieldValue.BaseDate.Value.Date;
                            // if day if not same as BaseDateTime return +1 or -1
                            var s = "";
                            var dayDiff = (dateTime.DayOfYear + dateTime.Year * 366) - (dateTimeFieldValue.BaseDate.Value.DayOfYear + dateTimeFieldValue.BaseDate.Value.Year * 366);
                            if (dayDiff != 0)
                                s += (Math.Sign(dayDiff) < 0 ? "-" : "+") + Math.Abs(dayDiff) + " ";
                            s += t.Hours.ToString("D2");
                            if (formField.TimeFormat == TimeFormat.Minutes || formField.TimeFormat == TimeFormat.Seconds)
                                s += ":" + t.Minutes.ToString("D2");
                            if (formField.TimeFormat == TimeFormat.Seconds)
                                s += ":" + t.Seconds.ToString("D2");
                            return s;
                            //return t.ToString("HH:mm");
                        }

                        var dateFormat = DateFormat ?? formField.DateFormat;
                        if (dateFormat.IsNullOrEmpty())
                        {
                            dateFormat = AppPress.Settings.NetDateFormat;
                            if (a.remoteLoginUserId != null)
                                dateFormat = a.remoteData.NetDateFormat;

                            if (formField.Style == FormDefFieldStyle.Date)
                            {
                                dateFormat = AppPress.Settings.NetDateTimeFormat;
                                if (a.remoteLoginUserId != null)
                                    dateFormat = a.remoteData.NetDateTimeFormat;
                                if (dateFormat == null)
                                    dateFormat = AppPress.Settings.NetDateFormat + " HH:mm";
                            }
                            else if (formField.Style == FormDefFieldStyle.Month)
                                dateFormat = AppPress.Settings.NetDateMonthFormat;
                        }

                        return DateTime.Parse(Value).ToString(dateFormat, System.Globalization.CultureInfo.InvariantCulture);

                    }
                case FormDefFieldType.Pickone:
                    {
                        //if (Value == null || Value.Trim().Length == 0)
                        //    return "";
                        var s = ((PickFieldValue)this).GetOption(a, Value).value;
                        if (a.skinType == SkinType.FO)
                            s = s.Replace("&", "&amp;").Replace("<", "&lt;");
                        return s;
                    }
                case FormDefFieldType.PickMultiple:
                    {
                        if (Value == null || Value.Trim().Length == 0)
                            return "";
                        var s = "";
                        var vs = Value.Split(new char[] { ',' });
                        foreach (var s1 in vs)
                        {
                            var s2 = ((PickFieldValue)this).GetOption(a, s1).value;
                            if (a.skinType == SkinType.FO)
                                s2 = s2.Replace("&", "&amp;").Replace("<", "&lt;");
                            if (!s.IsNullOrEmpty())
                                s += " | ";
                            s += s2;
                        }
                        return s;
                    }
                case FormDefFieldType.Checkbox:
                    {
                        if (formField.Static || a.skinType == SkinType.FO)
                        {
                            return ((PickFieldValue)this).GetOption(a, Value).value;
                        }
                        return Value == "1" ? "checked" : "";
                    }
                case FormDefFieldType.Number:
                    {
                        if (Value != null && decimal.Parse(Value) == AppPress.DecimalMultiple)
                            return AppPress.MultipleString;
                        return a.FormatNumber(formField, Value);
                    }
                case FormDefFieldType.Password:
                    return Value;
                case FormDefFieldType.TextArea:
                case FormDefFieldType.Text:
                case FormDefFieldType.HTML:
                    {
                        var tempValue = Value;
                        // to break long words in PDF
                        if (tempValue == null)
                            return "";
                        if (tempValue.StartsWith("LKey_"))
                            tempValue = AppPress.GetLocalizationKeyValue(tempValue);
                        if (formField.NoEncode)
                            return tempValue;
                        if (a.skinType == SkinType.FO && formField.Type != FormDefFieldType.HTML)
                        {
                            if (tempValue.IndexOf(' ') == -1)
                            {
                                var ca = Value.ToCharArray();
                                var sb = new StringBuilder();
                                for (int i = 0; i < ca.Length; ++i)
                                {
                                    sb.Append(ca[i]);
                                    sb.Append('\u200B');
                                }
                                tempValue = sb.ToString();
                            }
                        }
                        if (a.skinType == SkinType.FO)
                        {
                            if (formField.Type != FormDefFieldType.HTML)
                                tempValue = tempValue.Replace("&", "&amp;").Replace("<", "&lt;");
                            if (formField.Type == FormDefFieldType.HTML || formField.Static)
                                tempValue = tempValue.Replace("\r\n", "<fo:block/>").Replace("\n", "<fo:block/>").Replace("\r", "<fo:block/>");
                        }
                        else if (htmlFormat)
                        {
                            tempValue = HttpUtility.HtmlAttributeEncode(tempValue);
                            if (formField.Type == FormDefFieldType.HTML || formField.Static)
                                tempValue = tempValue.Replace("\n", "<br/>");
                        }
                        return tempValue;
                    }
            }
            return Value;
        }
        public int GetDecimals()
        {
            return formField.decimals;
        }
        /// <summary>
        /// Refresh the field in UI
        /// </summary>
        public void Refresh(AppPress a, bool getData = true)
        {
            if (a.CallReason != CallReasonType.PageLoad)
            {
                // Remove Refresh of same field
                a.appPressResponse.RemoveAll(t => t.appPressResponseType == AppPressResponseType.RefreshField &&
                    t.formDefId == FormData.formDefId &&
                    t.id == FormData.id &&
                    t.fieldDefId == formField.id
                    );
                a.appPressResponse.Add(AppPressResponse.RefreshField(FormData.a, this, getData));
            }
        }
        /// <summary>
        /// Validate the field.
        /// </summary>
        public void Validate()
        {
            if (!_Validate())
                throw new AppPressException();
        }
        /// <summary>
        /// Internal use only
        /// </summary>
        /// <returns></returns>
        public bool GetFieldBool()
        {
            if (FormData != null)
                FormData.a.AddDependentField(this);
            return Value != "0";
        }
        /// <summary>
        /// Internal use only
        /// </summary>
        /// <returns></returns>
        public void SetFieldBool(bool value)
        {
            if (FormData != null)
                FormData.a.AddReverseDependentField(this);
            Value = value ? "1" : "0";
            //if (FormData != null)
            //    if (this.Hidden == FieldHiddenType.None && !this.formField.Hidden)
            //        if (FormData.a.CallReason != CallReasonType.Refresh && this.Hidden == FieldHiddenType.None && !this.formField.Hidden)
            //        {
            //            // readonly fields on UI should have same value as the field
            //            this.Refresh(FormData.a, false);
            //        }

        }
        /// <summary>
        /// Internal use only
        /// </summary>
        /// <returns></returns>
        public int? GetFieldInt()
        {
            if (FormData != null)
                FormData.a.AddDependentField(this);
            if (Value == null)
                return null;
            return int.Parse(Value);
        }
        /// <summary>
        /// Internal use only
        /// </summary>
        /// <returns></returns>
        public void SetFieldInt(int? value)
        {
            if (FormData != null)
                FormData.a.AddReverseDependentField(this);
            Value = value == null ? null : value.ToString();
            //if (FormData != null)
            //    if (this.Hidden == FieldHiddenType.None && !this.formField.Hidden)
            //        if (FormData.a.CallReason != CallReasonType.Refresh && this.Hidden == FieldHiddenType.None && !this.formField.Hidden)
            //        {
            //            // readonly fields on UI should have same value as the field
            //            this.Refresh(FormData.a, false);
            //        }

        }
        /// <summary>
        /// Internal use only
        /// </summary>
        /// <returns></returns>
        public decimal? GetFieldDecimal()
        {
            if (FormData != null)
                FormData.a.AddDependentField(this);
            if (Value == null)
                return null;
            if (Value == AppPress.MultipleString)
                return AppPress.DecimalMultiple;
            return decimal.Parse(Value);
        }
        /// <summary>
        /// Internal use only
        /// </summary>
        /// <returns></returns>
        public void SetFieldDecimal(decimal? value)
        {
            if (FormData != null)
                FormData.a.AddReverseDependentField(this);
            Value = value == null ? null : value.ToString();
            //if (FormData != null)
            //    if (FormData.a.CallReason != CallReasonType.Refresh && this.Hidden == FieldHiddenType.None && !this.formField.Hidden)
            //        if (this.ReadOnly == FieldReadonlyType.Readonly || this.formField.Static)
            //        {
            //            // readonly fields on UI should have same value as the field
            //            this.Refresh(FormData.a, false);
            //        }

        }

        public string GetGroupName()
        {
            return formField.GroupName;
        }

        /// <summary>
        /// Internal use only
        /// </summary>
        /// <returns></returns>
        public DateTime? GetFieldDateTime()
        {
            if (FormData != null)
                FormData.a.AddDependentField(this);
            if (Value == null)
                return null;
            return DateTime.Parse(Value);
        }
        /// <summary>
        /// Internal use only
        /// </summary>
        /// <returns></returns>
        public void SetFieldDateTime(DateTime? value)
        {
            if (FormData != null)
                FormData.a.AddReverseDependentField(this);
            if (value == null)
                Value = null;
            else
                Value = value.Value.ToString(DAOBasic.DBDateTimeFormat);
            //if (FormData != null)
            //    if (FormData.a.CallReason != CallReasonType.Refresh && this.Hidden == FieldHiddenType.None && !this.formField.Hidden)
            //        if (this.ReadOnly == FieldReadonlyType.Readonly || this.formField.Static)
            //        {
            //            // readonly fields on UI should have same value as the field
            //            this.Refresh(FormData.a, false);
            //        }

        }
        /// <summary>
        /// Internal use only
        /// </summary>
        /// <returns></returns>
        public string GetFieldString()
        {
            if (FormData != null)
                FormData.a.AddDependentField(this);
            return Value;
        }
        /// <summary>
        /// Internal use only
        /// </summary>
        /// <returns></returns>
        public void SetFieldString(string value)
        {
            if (FormData != null)
                FormData.a.AddReverseDependentField(this);
            Value = value;
            //if (FormData != null)
            //    if (FormData.a.CallReason != CallReasonType.Refresh && this.Hidden == FieldHiddenType.None && !this.formField.Hidden)
            //        if (this.ReadOnly == FieldReadonlyType.Readonly || this.formField.Static)
            //        {
            //            // readonly fields on UI should have same value as the field
            //            this.Refresh(FormData.a, false);
            //        }
        }
        /// <summary>
        /// Internal use only
        /// </summary>
        /// <returns></returns>
        public List<int> GetFieldPickMultiple()
        {
            if (FormData != null)
                FormData.a.AddDependentField(this);
            return FormData.GetFieldPickMultiple(formField.fieldName);
        }
        /// <summary>
        /// Internal use only
        /// </summary>
        /// <returns></returns>
        public void SetFieldPickMultiple(List<int> value)
        {
            if (FormData != null)
                FormData.a.AddReverseDependentField(this);
            FormData.SetFieldPickMultiple(formField.fieldName, value);
            if (FormData != null)
                if (this.Hidden == FieldHiddenType.None && !this.formField.Hidden)
                    if (this.ReadOnly == FieldReadonlyType.Readonly || this.formField.Static)
                    {
                        // readonly fields on UI should have same value as the field
                        this.Refresh(FormData.a, false);
                    }

        }

        internal FormContainerFieldValue GetFormContainer()
        {
            var containerFieldName = formField.containerFormField.fieldName;
            return (FormContainerFieldValue)FormData.GetFieldValue(containerFieldName);
        }
        internal string _GetHtmlId()
        {
            return FormData.formDefId + AppPress.IdSep + formField.id + AppPress.IdSep + FormData.id;
        }
        /// <summary>
        /// ???
        /// </summary>
        /// <returns></returns>
        public string GetHtmlId()
        {
            return "AppPress" + AppPress.IdSep + (int)formField.Type + AppPress.IdSep + FormData.formDefId + AppPress.IdSep + formField.id + AppPress.IdSep + FormData.id;
        }
        /// <summary>
        /// ???
        /// </summary>
        /// <returns></returns>
        public string GetHtmlColumnId()
        {
            return "AppPress" + AppPress.IdSep + (int)formField.Type + AppPress.IdSep + FormData.formDefId + AppPress.IdSep + formField.id;
        }

        internal string GetHtmlErrorId()
        {
            return "error" + AppPress.IdSep + FormData.formDef.id + AppPress.IdSep + formField.id + AppPress.IdSep + FormData.id;
        }
        internal string GetHtmlHelpId()
        {
            return "help" + AppPress.IdSep + FormData.formDef.id + AppPress.IdSep + formField.id + AppPress.IdSep + FormData.id;
        }
        public string GetHtmlContainerId()
        {
            return "fieldContainer" + AppPress.IdSep + FormData.formDef.id + AppPress.IdSep + formField.id + AppPress.IdSep + FormData.id;
        }

        internal string GetSkin(AppPress a, out bool outer)
        {
            // Test cases in Empire
            // NewEmployee, AppraisalMaster->ManageEmployee->Add, DivisionManagement->Add SubDivisionHead, EmployeeProfile->FinancialDetails
            // go up till we find Form containing this field as part of skin
            var formDef = FormData.formDef;
            var upFieldValues = new List<FieldValue>();
            var containerFieldValue = FormData.containerFieldValue;
            while (containerFieldValue != null && FormData.callerFieldValue == null)
            {
                if (containerFieldValue.formField.rowFormDef == null)
                    break;
                upFieldValues.Add(containerFieldValue);
                formDef = containerFieldValue.FormData.formDef;
                containerFieldValue = containerFieldValue.FormData.containerFieldValue;
                if (containerFieldValue == null)
                    break;
            }
            var skin = formDef.GetSkin(a, false, false, null, SkinType.HTML, 0);
            string fieldSkin;
            int startIndex;
            // go down till we get skin for formField.formDef
            for (int i = upFieldValues.Count() - 1; i >= 0; --i)
            {
                skin = upFieldValues[i].formField.RemoveBetweenMarkers(a, skin, 0, "Header", out startIndex, out fieldSkin, out outer);
                upFieldValues[i].formField.RemoveBetweenMarkers(a, skin, 0, "", out startIndex, out fieldSkin, out outer);
                if (fieldSkin != null) // sometimes do not get upField Markers
                    skin = fieldSkin;
            }
            //if (FormData.containerFieldValue != null && (FormData.containerFieldValue.formField.Type == FormDefFieldType.FormContainerGrid || FormData.containerFieldValue.formField.Type == FormDefFieldType.FormContainerDynamic))
            if (FormData.containerFieldValue != null && FormData.containerFieldValue.formField.rowFormDef != null)
            {
                FormData.containerFieldValue.formField.RemoveBetweenMarkers(a, skin, 0, "Row", out startIndex, out fieldSkin, out outer);
                skin = fieldSkin;
            }
            formDef = FormData.formDef;
            if (formField.containerFormField != null)
            {
                // For FormContainer Fields get the skin out containing field and loook in that
                var cFieldValue = FormData.GetFieldValue(formField.containerFormField.id);
                bool outer1;
                skin = cFieldValue.GetSkin(a, out outer1);
            }

            // remove markers for all other fields first as field with same name may be nested in other fields
            foreach (var f in formDef.formFields)
                if (f.id != formField.id && (f.containerFormField == null || f.containerFormField.id != formField.id))
                {
                    //StartIndex = 1 to ignore top level marker name same as f.fieldName. Correction was done for SubDivionHead refresh in DivisionManagement.
                    skin = f.RemoveBetweenMarkers(a, skin, 1, "", out startIndex, out fieldSkin, out outer);
                }
            formField.RemoveBetweenMarkers(a, skin, 0, "", out startIndex, out skin, out outer);
            return skin;
        }
        internal string GetSecurityKey(AppPress a)
        {
            if (FormData == null || formField.Type == FormDefFieldType.FormContainerDynamic || formField.Type == FormDefFieldType.Button || formField.Static)
                return null;

            SHA1 sha = new SHA1CryptoServiceProvider();
            // This is one implementation of the abstract class SHA1.

            string s = "";
            if (formField.Type == FormDefFieldType.Pickone)
            {
                if (Value != null && Value.Trim().Length > 0) //Ram: Blank ID should be treated as null
                {
                    var optionsFunction = formField.FieldFunctions.Find(t => t.ServerFunctionType == FunctionType.Options);
                    if (optionsFunction != null)
                    {
                        var option = ((PickFieldValue)this).GetOption(a, Value);
                        if (option == null)
                            // option should be valid
                            throw new Exception("Security Error");
                    }
                }
            }
            s += _GetHtmlId();

            if (ReadOnly != FieldReadonlyType.None)
            {
                var temps = GetFormattedFieldValue(a, true, "dd/MM/yyyy");
                if (!temps.IsNullOrEmpty())
                    s += temps.Trim();
            }
            if (Hidden != FieldHiddenType.None)
                s += "NonH";
            if (AppPress.Settings.developer)
                return s;

            var ba = sha.ComputeHash(Encoding.ASCII.GetBytes(s));
            return string.Concat(ba.Select(b => b.ToString("X2")).ToArray());
        }

        internal string GetHtmlOnClick(AppPress a, bool replaceThis = false)
        {
            switch (formField.Type)
            {
                case FormDefFieldType.PickMultiple:
                case FormDefFieldType.Checkbox:
                    return GetHtmlOnChange(a);
                case FormDefFieldType.Button:
                case FormDefFieldType.Pickone:
                case FormDefFieldType.FileUpload:
                    if (formField.FieldFunctions == null)
                        throw new Exception("Could not find OnClick function in Field: " + formField.fieldName + " in Form: " + formField.formDef.formName);
                    string s = string.Empty;
                    if (formField.Type == FormDefFieldType.Checkbox)
                        if (!formField.DoNotSaveInDB)
                            s += "SetDirty();";
                    s += GetOnClickJSScript(replaceThis);
                    return s;
                default:
                    throw new Exception("Cannot Get AppPressOnClick for " + GetFieldDescription() + " which is of Type: " + formField.Type);
            }
        }

        internal bool InvokeOn(AppPress a, string functionName, bool invoke)
        {
            var fieldTypes = new List<Type>();
            var className = formField.GetClassName();
            Type fieldType1 = Util.GetType(className);
            if (fieldType1 != null)
                fieldTypes.Add(fieldType1);
            className = formField.GetClassName(FormData.formDef);
            fieldType1 = Util.GetType(className);
            if (fieldType1 != null)
                if (fieldTypes.Find(t => t == fieldType1) == null)
                    fieldTypes.Add(fieldType1);
            bool methodInvoked = false;
            foreach (var fieldType in fieldTypes)
            {
                for (int i = AppPress.Assemblies.Count() - 1; i >= 0; --i)
                {
                    var assembly = AppPress.Assemblies[i];
                    var method = assembly.appLogicType.GetMethod(functionName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static, null, new Type[] { AppPress.Settings.ApplicationAppPress, fieldType }, null);
                    if (method != null)
                        if (invoke)
                            try
                            {
                                if (a.fieldValue.FormData.IsDeleted)
                                    throw new Exception("Cannot call OnClick for a Deleted FormData. One of the causes for this can be because the Popup containing the FormData is already Closed.");
                                a.serverFunction = new ServerFunction();
                                Object o = a.fieldValue;
                                if (o.GetType() != fieldType)
                                {
                                    o = Activator.CreateInstance(fieldType, new object[] { this });
                                }
                                Util.InvokeMethod(a, method, new object[] { a, o });
                                methodInvoked = true;
                            }
                            finally
                            {
                                a.serverFunction = null;
                            }
                        else
                            return true;
                }
            }
            return methodInvoked;
        }


        internal bool _Validate()
        {
            var a = FormData.a;
            var pFieldValue = a.fieldValue;
            var pFormData = a.fieldValue.FormData;
            var result = true;
            try
            {
                a.fieldValue = this;
                a.fieldValue.FormData = FormData;
                int clientActionCount = a.appPressResponse.Count;
                if ((formField.Required || this.Required) && !formField.fieldName.StartsWith("Sortable"))
                    AppPressLogic.RequiredValidation(a);
                if (formField.EmailValidation)
                    AppPressLogic.EmailValidation(a);
                if (formField.RegexValidation != null)
                    AppPressLogic.RegexValidation(a, formField.RegexValidation);
                if (formField.MinimumValue != null || formField.MaximumValue != null)
                    AppPressLogic.RangeValidation(a);
                if (a.appPressResponse.Count > clientActionCount)
                    result = false;
            }
            finally
            {
                a.fieldValue = pFieldValue;
                a.fieldValue.FormData = pFormData;
            }
            // intrinsic validation with fieldType
            switch (formField.Type)
            {
                case FormDefFieldType.FormContainerDynamic:
                    {
                        foreach (var fd in a.formDatas)
                            if (fd.containerFieldValue == this)
                                foreach (var f in fd.fieldValues)
                                    if (!f._Validate())
                                        result = false;
                        break;
                    }
                case FormDefFieldType.Number:
                    decimal d = 0m;
                    if (!String.IsNullOrEmpty(Value) && !decimal.TryParse(Value, out d))
                    {
                        result = false;
                        a.appPressResponse.Add(AppPressResponse.FieldError(this, "This Number is not valid"));
                    }
                    else
                    {
                        var numberFieldValue = (NumberFieldValue)this;
                        if (numberFieldValue.MinNumber != null && d < numberFieldValue.MinNumber)
                            a.appPressResponse.Add(AppPressResponse.FieldError(this, "Number should be more or equal to " + numberFieldValue.MinNumber));
                        if (numberFieldValue.MaxNumber != null && d > numberFieldValue.MaxNumber)
                            a.appPressResponse.Add(AppPressResponse.FieldError(this, "Number should be less or equal to " + numberFieldValue.MaxNumber));
                    }
                    break;
                case FormDefFieldType.DateTime:
                    if (Value != null)
                    {
                        var dateTimeFieldValue = (DateTimeFieldValue)this;
                        if (dateTimeFieldValue.MinDateTime != null && DateTime.Parse(Value) < dateTimeFieldValue.MinDateTime)
                            a.appPressResponse.Add(AppPressResponse.FieldError(this, "Date should be more or equal to " + dateTimeFieldValue.MinDateTime.Value.ToString(AppPress.Settings.NetDateFormat)));
                        if (dateTimeFieldValue.MaxDateTime != null && DateTime.Parse(Value) > dateTimeFieldValue.MaxDateTime)
                            a.appPressResponse.Add(AppPressResponse.FieldError(this, "Date should be less than " + dateTimeFieldValue.MaxDateTime.Value.ToString(AppPress.Settings.NetDateFormat)));
                    }
                    break;
            }
            return result;
        }

        internal string ReplaceAppPress(AppPress a, string fieldSkin)
        {
            var htmlId = GetHtmlId();
            fieldSkin = fieldSkin.Replace("AppPressId", htmlId).
                Replace("AppPressErrorId", GetHtmlErrorId()).
                Replace("AppPressHelpId", GetHtmlHelpId()).
                Replace("AppPressContainerId", GetHtmlContainerId()).
                Replace("AppPressColumnId", GetHtmlColumnId()).
                Replace("AppPressFieldName", formField.fieldName);
            var fieldHelpText = formField.Help;
            //if (Help != null)
            //{
            //    if (fieldHelpText != null)
            //        fieldHelpText += "<br/>";
            //    fieldHelpText += Help;
            //}
            fieldSkin = fieldSkin.Replace("AppPressHelpText", fieldHelpText);
            int idx = fieldSkin.IndexOf("AppPressOnClick");
            if (idx != -1)
                try
                {
                    fieldSkin = fieldSkin.Replace("AppPressOnClick", GetHtmlOnClick(a));
                }
                catch (Exception ex)
                {
                    int linestart = idx;
                    for (; linestart >= 0; --linestart)
                        if (fieldSkin[linestart] == '\r' || fieldSkin[linestart] == '\n')
                            break;
                    int lineEnd = idx;
                    for (; lineEnd < fieldSkin.Length; ++lineEnd)
                        if (fieldSkin[lineEnd] == '\r' || fieldSkin[lineEnd] == '\n')
                            break;
                    throw new Exception(ex.Message + "\nLine: \n" + fieldSkin.Substring(linestart, lineEnd - linestart));
                }
            if (fieldSkin.IndexOf("AppPressOnChange") != -1)
                fieldSkin = fieldSkin.Replace("AppPressOnChange", GetHtmlOnChange(a));
            if (fieldSkin.IndexOf("AppPressDisplayName") != -1)
                fieldSkin = fieldSkin.Replace("AppPressDisplayName", GetLabel(a));
            if (fieldSkin.IndexOf("AppPressFieldName") != -1)
                fieldSkin = fieldSkin.Replace("AppPressFieldName", formField.fieldName);

            if (fieldSkin.IndexOf("AppPressMaxLength") != -1)
            {
                var maxLength = "5000000";
                if (this.formField.Type == FormDefFieldType.Number)
                    maxLength = (16 + this.formField.decimals).ToString();
                else if (this.formField.MaxChars != null)
                    maxLength = this.formField.MaxChars.ToString();
                fieldSkin = fieldSkin.Replace("AppPressMaxLength", maxLength);
            }

            var controlStyle = "";
            if (this.ControlStyle != null)
            {
                var c = this.ControlStyle.Trim();
                if (!c.EndsWith(";"))
                    c += ";";
                controlStyle += c;
            }
            if (this.formField.ControlStyle != null)
            {
                var c = this.formField.ControlStyle.Trim();
                if (!c.EndsWith(";"))
                    c += ";";
                controlStyle += c;
            }
            fieldSkin = fieldSkin.Replace("AppPressControlStyle", controlStyle);
            var partStyle = "";
            if (this.formField.PartStyle != null)
            {
                var c = this.formField.PartStyle.Trim();
                if (!c.EndsWith(";"))
                    c += ";";
                partStyle += c;
            }
            fieldSkin = fieldSkin.Replace("AppPressPartStyle", partStyle);
            var containerStyle = "";
            if (this.formField.ContainerStyle != null)
            {
                var c = this.formField.ContainerStyle.Trim();
                if (!c.EndsWith(";"))
                    c += ";";
                containerStyle += c;
            }
            fieldSkin = fieldSkin.Replace("AppPressContainerStyle", containerStyle);

            var labelStyle = "";
            if (this.LabelStyle != null)
            {
                var c = this.LabelStyle.Trim();
                if (!c.EndsWith(";"))
                    c += ";";
                labelStyle += c;
            }
            if (this.formField.LabelStyle != null)
            {
                var c = this.formField.LabelStyle.Trim();
                if (!c.EndsWith(";"))
                    c += ";";
                labelStyle += c;
            }
            fieldSkin = fieldSkin.Replace("AppPressLabelStyle", labelStyle);

            if (this.formField.Type == FormDefFieldType.FileUpload)
            {
                int i; string outStr;
                if (this.ReadOnly != FieldReadonlyType.None)
                {
                    fieldSkin = FormField.RemoveBetweenMarkers(a, fieldSkin, 0, "<!--|AppPress.FileUpload.DeletePartBegin|-->", "<!--|AppPress.FileUpload.DeletePartEnd|-->", out i, out outStr, null);
                    fieldSkin = FormField.RemoveBetweenMarkers(a, fieldSkin, 0, "<!--|AppPress.FileUpload.UploadPartBegin|-->", "<!--|AppPress.FileUpload.UploadPartEnd|-->", out i, out outStr, null);
                }
                if (this.Value == null)
                {
                    fieldSkin = FormField.RemoveBetweenMarkers(a, fieldSkin, 0, "<!--|AppPress.FileUpload.FileNamePartBegin|-->", "<!--|AppPress.FileUpload.FileNamePartEnd|-->", out i, out outStr, null);
                    fieldSkin = FormField.RemoveBetweenMarkers(a, fieldSkin, 0, "<!--|AppPress.FileUpload.DeletePartBegin|-->", "<!--|AppPress.FileUpload.DeletePartEnd|-->", out i, out outStr, null);
                }
                else
                {
                    if (fieldSkin.IndexOf("AppPressFileUploadDownloadUrl") != -1)
                        if (this.formField.DoNotSaveInDB && !this.formField.Static)
                            fieldSkin = fieldSkin.Replace("AppPressFileUploadDownloadUrl", a.GetFileUrl(this.Value));
                        else
                            fieldSkin = fieldSkin.Replace("AppPressFileUploadDownloadUrl", a.GetFileUrl(int.Parse(this.Value)));
                    if (fieldSkin.IndexOf("AppPressFileUploadFileName") != -1)
                    {
                        if (this.formField.DoNotSaveInDB && !this.formField.Static)
                            fieldSkin = fieldSkin.Replace("AppPressFileUploadFileName", Path.GetFileName(this.Value));
                        else if (this.Value != null)
                        {
                            try
                            {
                                var fileDetail = AppPress.GetFile(int.Parse(this.Value));
                                fieldSkin = fieldSkin.Replace("AppPressFileUploadFileName", fileDetail == null ? "" : fileDetail.FileName);
                            }
                            catch
                            {
#if DEBUG
#else
                                throw;
#endif
                            }
                        }
                    }
                    if (fieldSkin.IndexOf("AppPressFileUploadDeleteUrl") != -1)
                        fieldSkin = fieldSkin.Replace("AppPressFileUploadDeleteUrl", "DeleteFile('" + htmlId + "'," + AppPress.LocalInstanceId + ")");
                }
            }

            return fieldSkin;
        }

        internal string CompileSkin(AppPress a, string skin, bool header)
        {
            var fieldValue = this;
            var formField = fieldValue.formField;
            if (formField.Type != FormDefFieldType.FormContainerDynamic && formField.Type != FormDefFieldType.ForeignKey)
            {
                // remove all formContainer skins
                var formContainers = new List<string>();
                var formContainerKeys = new List<string>();
                if (formField.formDef != null) // in some unknown case this code is causing problem after some use of Site
                    foreach (var f in fieldValue.FormData.formDef.formFields)
                        if (f != null && f.Type == FormDefFieldType.FormContainerDynamic)
                        {
                            string fieldUnique, fieldSkin, beginMarker, endMarker;
                            bool outer;
                            skin = f.ExtractBetweenMarkers(a, skin, 0, "", out fieldUnique, out fieldSkin, out outer, out beginMarker, out endMarker);
                            if (fieldSkin != null)
                                if (!outer)
                                    skin = skin.Replace(fieldUnique, beginMarker + fieldSkin + endMarker);
                                else
                                {
                                    formContainers.Add(beginMarker + fieldSkin + endMarker);
                                    formContainerKeys.Add(fieldUnique);
                                }
                        }


                bool found = false;
                bool appPressIdFound = formField.Static || formField.Type == FormDefFieldType.FileUpload || a.skinType != SkinType.HTML;
                while (true) // multiple begin End Markers for a Field. Eg Date Range
                {
                    string fieldSkin, fieldUnique, beginMarker, endMarker;
                    bool outer;
                    var formName = FormData.formDef.formName;
                    skin = formField.ExtractBetweenMarkers(a, skin, 0, "", out fieldUnique, out fieldSkin, out outer, formName, out beginMarker, out endMarker);
                    if (fieldSkin == null)
                    {
                        //if (!found && formField.Type != FormDefFieldType.Extension && formField.Type != FormDefFieldType.UserControl && formField.Type != FormDefFieldType.UserControlScalar && !formField.Hidden)
                        //    throw new Exception(formField.GetFieldDescription() + " Could not find Begin Marker " + HttpUtility.HtmlEncode(beginMarker) + "\n This can be due to following\n\tThe markers are missing.\n\tThe markers are not in correct position. \nYou can also check by looking at markers in AppPress generated skin from dev links. For more information look at Working with Skins ???");
                        if (!found)
                        {
#if DEBUG
                            if (!header && fieldValue.Hidden == FieldHiddenType.None && !fieldValue.formField.Hidden && a.fieldsNotGenerated != null && a.fieldsNotGenerated.Find(t => t == fieldValue) == null)
                            {
                                bool hidden = false;
                                if (fieldValue.FormData.containerFieldValue != null && fieldValue.FormData.containerFieldValue.formField.OriginalType == (int)FormDefFieldType.FormContainerGrid)
                                {
                                    var HiddenColumns = ((FormContainerGridFieldValue)fieldValue.FormData.containerFieldValue).HiddenColumns;
                                    if (HiddenColumns != null)
                                        hidden = HiddenColumns.Find(t => t == fieldValue.formField.fieldName) != null;
                                    if (!hidden)
                                        a.fieldsNotGenerated.Add(fieldValue);
                                }
                            }
#endif
                            appPressIdFound = true;
                        }
                        break;
                    }
                    if (!FormData.formDef.Pivot && fieldValue.Hidden == FieldHiddenType.Hidden)
                    {
                        var containerFieldValue = fieldValue.FormData.containerFieldValue;
                        while (containerFieldValue != null)
                            if (containerFieldValue.Hidden == FieldHiddenType.Hidden)
                                break;
                            else
                                containerFieldValue = containerFieldValue.FormData.containerFieldValue;
                        if (containerFieldValue != null)
                            continue; // found a hidden container
                        int hiddenMarkerBeginIndex = fieldSkin.IndexOf("<!--|AppPress.HiddenBegin|-->");
                        if (hiddenMarkerBeginIndex != -1)
                        {
                            int hiddenMarkerEndIndex = fieldSkin.IndexOf("<!--|AppPress.HiddenEnd|-->");
                            if (hiddenMarkerEndIndex == -1)
                                throw new Exception(GetFieldDescription() + " Cound not find Marker HiddenEnd");
                            fieldSkin = fieldSkin.Substring(0, hiddenMarkerBeginIndex) + fieldSkin.Substring(hiddenMarkerEndIndex + "<!--|AppPress.HiddenEnd|-->".Length);
                        }
                        else
                        {
                            if (a.skinType == SkinType.FO)
                                skin = skin.Replace(fieldUnique, "");
                            else
                            {
                                string tag = null;
                                if (tag == null && fieldValue.formField.containerFormField == null && fieldValue.FormData.containerFieldValue != null && fieldValue.FormData.containerFieldValue.formField.OriginalType == (int)FormDefFieldType.FormContainerGrid && !FormData.formDef.Pivot)
                                    tag = "td";
                                if (tag == null)
                                    tag = "span";
                                var hiddenMessage = "<" + tag + " id='" + fieldValue.GetHtmlContainerId() + "' ";
                                if (fieldValue.formField.OriginalType == (int)FormDefFieldType.DateRange)
                                    hiddenMessage += "style='display:table-cell'";
                                hiddenMessage += "></" + tag + ">";
                                skin = skin.Replace(fieldUnique, hiddenMessage);
                            }
                            continue;
                        }
                    }
                    found = true;
                    if (!outer || appPressIdFound || fieldSkin.IndexOf("AppPressId") != -1)
                        appPressIdFound = true;
                    string partUnique;
                    fieldSkin = fieldValue.ReplaceAppPress(a, fieldSkin);
                    var htmlId = fieldValue.GetHtmlId();
                    if (!formField.Static)
                        if (formField.Type == FormDefFieldType.PickMultiple ||
                            (formField.Type == FormDefFieldType.Pickone &&
                                (formField.Style == FormDefFieldStyle.Radio || formField.Style == FormDefFieldStyle.DropDown || formField.Style == FormDefFieldStyle.AutoComplete)))
                        {
                            string partSkin;
                            fieldSkin = FormField.ExtractBetweenMarkers(a, fieldSkin, 0, "<!--|AppPress.PartBegin|-->", "<!--|AppPress.PartEnd|-->", out partUnique, out partSkin, formField.formDef.formName);
                            if (partSkin != null)
                            //throw new Exception(HttpUtility.HtmlEncode(formField.GetFieldDescription() + " In Skin Could not find <!--|AppPress.PartBegin|-->  <!--|AppPress.PartEnd|--> to generate options"));
                            {
                                string[] values = fieldValue.Value == null ? null : fieldValue.Value.Split(new char[] { ',' });
                                ((PickFieldValue)fieldValue).GetOptions(a);
                                if (formField.Static)
                                {
                                    if (values != null)
                                        foreach (var value in values)
                                        {
                                            var partCompiled = partSkin.Replace("AppPressValue", ((PickFieldValue)fieldValue).options.Find(t => t.id == value).value, StringComparison.OrdinalIgnoreCase);
                                            fieldSkin = fieldSkin.Replace(partUnique, partCompiled + partUnique);
                                        }
                                }
                                else
                                {
                                    if (formField.optionsCache != null && formField.Type == FormDefFieldType.Pickone && formField.Style == FormDefFieldStyle.DropDown)
                                    {
                                        bool ignoreNull = this.Value != null && this.formField.Required;
                                        var sdata = AppPress.TryGetSessionData();
                                        var CurrentLanguage = "English";
                                        if (sdata != null && sdata.CurrentLanguage != null)
                                            CurrentLanguage = sdata.CurrentLanguage;
                                        a.JsStr.Append("FillOptionsFromCache('" + formField.id + "','" + fieldValue.GetHtmlId() + "','" + CurrentLanguage + "'," + (ignoreNull ? "true" : "false") + ");");
                                    }
                                    else
                                    {
                                        var options = ((PickFieldValue)fieldValue).options;
                                        if (options != null)
                                        {
                                            if (formField.Type == FormDefFieldType.PickMultiple && formField.Style == FormDefFieldStyle.DropDown && values != null)
                                                a.JsStr.Append("$(JQueryEscape('#" + GetHtmlId() + "')).val(['" + string.Join("','", values) + "']);");

                                            foreach (var option in options)
                                            {
                                                var appPressValue = option.id;
                                                if (formField.Type == FormDefFieldType.PickMultiple && formField.Style == FormDefFieldStyle.Checkboxes && values != null)
                                                    foreach (var value in values)
                                                        if (value == appPressValue)
                                                        {
                                                            appPressValue += @""" checked=""checked";
                                                            break;
                                                        }
                                                if (formField.Type == FormDefFieldType.Pickone && formField.Style == FormDefFieldStyle.DropDown && option.disabled)
                                                    fieldSkin = fieldSkin.Replace(partUnique, "<option disabled>" + option.value + "</option>" + partUnique);
                                                else
                                                {
                                                    var partCompiled = partSkin.
                                                      Replace("AppPressValue", appPressValue, StringComparison.OrdinalIgnoreCase).
                                                      Replace("AppPressLabel", option.value);
                                                    fieldSkin = fieldSkin.Replace(partUnique, partCompiled + partUnique);
                                                }
                                            }
                                        }
                                    }
                                }
                                fieldSkin = fieldSkin.Replace(partUnique, "");
                            }
                            if (formField.Type == FormDefFieldType.PickMultiple)
                            {
                                a.JsStr.Append(@"_AppPress_UpdatePickMultipleOptions(""" + htmlId + @""");");
                                a.JsStr.Append(@"AppPress_AddPickMultipleToPopupDialogs(""" + htmlId + @"""," + FormData.pageStackIndex + ");");
                            }
                        }
                    fieldSkin = fieldSkin.Replace("AppPressValue", GetFieldValue(a));
                    skin = skin.Replace(fieldUnique, fieldSkin);
                    if (!formField.Static)
                    {

                        if (formField.Type == FormDefFieldType.DateTime)
                            a.JsStr.Append(a.GetHtmlDatePopup(formField.fieldName));

                        if (formField.Type == FormDefFieldType.Checkbox)
                            if (Value == "1")
                                a.JsStr.Append("$('#'+JQueryEscape('" + GetHtmlId() + "')).attr('checked',true);\n");

                        if (formField.Type == FormDefFieldType.FileUpload)
                            a.JsStr.Append(a.GetFileUploadScript(formField.fieldName));

                        if (formField.Type == FormDefFieldType.Number)
                        {
                            var regexString = "^\\\\+?\\\\-?\\\\d+$";
                            if (formField.decimals > 0)
                                regexString = "^\\\\+?\\\\-?\\\\d*(?:\\\\.\\\\d+)?$";
                            a.JsStr.Append(@"
                                FieldValidationData['" + htmlId + @"'] = new Object();
                                FieldValidationData['" + htmlId + @"'].type = 'RegEx';
                                FieldValidationData['" + htmlId + @"'].errorHtmlId = '" + GetHtmlErrorId() + @"';
                                FieldValidationData['" + htmlId + @"'].checkString = '" + regexString + @"';
                                FieldValidationData['" + htmlId + @"'].errorMessage = 'This Number is not valid';
                                $(JQueryEscape('#" + htmlId + @"')).blur(
                                    function(){
                                        return FieldValidate('" + htmlId + @"');
                                    });");
                        }
                        if (formField.Type == FormDefFieldType.TextArea)
                        {
                            var style = formField.Style;
                            if (this.Style != FormDefFieldStyle.None)
                                style = this.Style;
                            switch (style)
                            {
                                case FormDefFieldStyle.UpperCase:
                                case FormDefFieldStyle.TitleCase:
                                case FormDefFieldStyle.LowerCase:
                                    {
                                        a.JsStr.Append(@"
                                $(JQueryEscape('#" + htmlId + @"')).blur(
                                    function(){
                                        this.value = to" + formField.Style + @"(this.value);
                                    });");
                                        break;
                                    }
                                case FormDefFieldStyle.RichTextCKEditorFull:
                                    a.JsStr.Append(@"CKEDITOR.replace('" + GetHtmlId() + @"', {
                                        width: '100%',
                                        height:400,
                                        skin : 'bootstrapck',
                                        title : false
                                    } );");
                                    break;
                                case FormDefFieldStyle.RichTextCKEditorStandard:
                                    a.JsStr.Append(@"CKEDITOR.replace('" + GetHtmlId() + @"', {
                                        width: '100%',
                                        height:400,
                                    skin : 'bootstrapck',
                                    title : false,
                                    toolbarGroups : [
		                                    { name: 'clipboard',   groups: [ 'clipboard', 'undo' ] },
		                                    { name: 'editing',     groups: [ 'find', 'selection', 'spellchecker' ] },
		                                    { name: 'links' },
		                                    { name: 'insert' },
		                                    { name: 'forms' },
		                                    { name: 'tools' },
		                                    { name: 'document',	   groups: [ 'mode', 'document', 'doctools' ] },
		                                    { name: 'others' },
		                                    '/',
		                                    { name: 'basicstyles', groups: [ 'basicstyles', 'cleanup' ] },
		                                    { name: 'paragraph',   groups: [ 'list', 'indent', 'blocks', 'align', 'bidi' ] },
		                                    { name: 'styles' },
		                                    { name: 'colors' },
		                                    { name: 'about' }
	                                    ],

	                                    // Remove some buttons provided by the standard plugins, which are
	                                    // not needed in the Standard(s) toolbar.
	                                    removeButtons : 'Underline,Subscript,Superscript',

	                                    // Set the most common block elements.
	                                    format_tags : 'p;h1;h2;h3;pre',

	                                    // Simplify the dialog windows.
	                                    removeDialogTabs : 'image:advanced;link:advanced'                                
                                                                } );");
                                    break;
                                case FormDefFieldStyle.RichTextCKEditorBasic:
                                    a.JsStr.Append(@"CKEDITOR.replace('" + GetHtmlId() + @"', {
                                        width: '100%',
                                        height:400,
                                    skin : 'bootstrapck',
                                    title : false,
                                    // Plugins used by basic preset.
                                    plugins: 'about,basicstyles,clipboard,floatingspace,list,indentlist,enterkey,entities,link,toolbar,undo,wysiwygarea',

                                    // The toolbar groups arrangement, optimized for a single toolbar row.
                                    toolbarGroups: [
                                        { name: 'document',    groups: [ 'mode', 'document', 'doctools' ] },
                                        { name: 'clipboard',   groups: [ 'clipboard', 'undo' ] },
                                        { name: 'editing',     groups: [ 'find', 'selection', 'spellchecker' ] },
                                        { name: 'forms' },
                                        { name: 'basicstyles', groups: [ 'basicstyles', 'cleanup' ] },
                                        { name: 'paragraph',   groups: [ 'list', 'indent', 'blocks', 'align', 'bidi' ] },
                                        { name: 'links' },
                                        { name: 'insert' },
                                        { name: 'styles' },
                                        { name: 'colors' },
                                        { name: 'tools' },
                                        { name: 'others' },
                                        { name: 'about' }
                                    ],

                                    // The default plugins included in the basic setup define some buttons that
                                    // are not needed in a basic editor. They are removed here.
                                    removeButtons: 'Cut,Copy,Paste,Undo,Redo,Anchor,Underline,Strike,Subscript,Superscript',

                                    // Dialog windows are also simplified.
                                    removeDialogTabs: 'link:advanced'
                                                                } );");

                                    break;
                            }
                        }
                        if (formField.Type == FormDefFieldType.DateTime)
                        {
                            if (formField.Style == FormDefFieldStyle.Date)
                            {
                                var dateFormat = AppPress.Settings.JQueryDateFormat;
                                if (a.remoteLoginUserId != null)
                                    dateFormat = a.remoteData.JQueryDateFormat;
                                a.JsStr.Append(@"
                                FieldValidationData['" + htmlId + @"'] = new Object();
                                FieldValidationData['" + htmlId + @"'].type = 'Date';
                                FieldValidationData['" + htmlId + @"'].errorHtmlId = '" + GetHtmlErrorId() + @"';
                                FieldValidationData['" + htmlId + @"'].checkString = '" + dateFormat + @"';
                                FieldValidationData['" + htmlId + @"'].errorMessage = 'This date is not valid';
                                $(JQueryEscape('#" + htmlId + @"')).change(
                                    function(){
                                        return FieldValidate('" + htmlId + @"');
                                    });");
                            }
                            else if (formField.Style == FormDefFieldStyle.Time)
                            {
                                // TBD: Use Regex for Time
                            }
                        }
                        if (formField.Type == FormDefFieldType.Text)
                        {
                            var style = formField.Style;
                            if (this.Style != FormDefFieldStyle.None)
                                style = this.Style;
                            if (formField.EmailValidation && style == FormDefFieldStyle.None)
                                style = FormDefFieldStyle.LowerCase;
                            if (style == FormDefFieldStyle.UpperCase || style == FormDefFieldStyle.TitleCase || style == FormDefFieldStyle.LowerCase)
                            {
                                a.JsStr.Append(@"
                                $(JQueryEscape('#" + htmlId + @"')).blur(
                                    function(){
                                        this.value = to" + style + @"(this.value);
                                    });");
                            }
                            if (formField.EmailValidation)
                            {
                                var regexString = "^\\\\s*([\\\\w-]+(?:\\\\.[\\\\w-]+)*)@((?:[\\\\w-]+\\\\.)*\\\\w[\\\\w-]{0,66})\\\\.([a-zA-Z]{2,6}(?:\\\\.[a-zA-Z]{2})?)\\\\s*$";
                                a.JsStr.Append(@"
                                FieldValidationData['" + htmlId + @"'] = new Object();
                                FieldValidationData['" + htmlId + @"'].type = 'RegEx';
                                FieldValidationData['" + htmlId + @"'].errorHtmlId = '" + GetHtmlErrorId() + @"';
                                FieldValidationData['" + htmlId + @"'].checkString = '" + regexString + @"';
                                FieldValidationData['" + htmlId + @"'].errorMessage = 'This is not a valid email';
                                $(JQueryEscape('#" + htmlId + @"')).blur(
                                    function(){
                                        this.value = $.trim(this.value);
                                        return FieldValidate('" + htmlId + @"');
                                    });");
                            }
                            else if (formField.RegexValidation != null)
                            {
                                a.JsStr.Append(@"
                                FieldValidationData['" + htmlId + @"'] = new Object();
                                FieldValidationData['" + htmlId + @"'].type = 'RegEx';
                                FieldValidationData['" + htmlId + @"'].errorHtmlId = '" + GetHtmlErrorId() + @"';
                                FieldValidationData['" + htmlId + @"'].checkString = '" + formField.RegexValidation.Replace("'", "\\'") + @"';
                                FieldValidationData['" + htmlId + @"'].errorMessage = 'This field contains invalid characters';
                                $(JQueryEscape('#" + htmlId + @"')).blur(
                                    function(){
                                        return FieldValidate('" + htmlId + @"');
                                    });");
                            }
                        }
                        if (formField.Type == FormDefFieldType.Text || formField.Type == FormDefFieldType.TextArea)
                            if (formField.MinChars != null)
                            {
                                a.JsStr.Append(@"
                                FieldValidationData['" + htmlId + @"'] = new Object();
                                FieldValidationData['" + htmlId + @"'].type = 'MinChars';
                                FieldValidationData['" + htmlId + @"'].errorHtmlId = '" + GetHtmlErrorId() + @"';
                                FieldValidationData['" + htmlId + @"'].MinChars = " + formField.MinChars + @";
                                FieldValidationData['" + htmlId + @"'].errorMessage = 'This field must contain minimum " + formField.MinChars + @" Characters';
                                $(JQueryEscape('#" + htmlId + @"')).blur(
                                    function(){
                                        return FieldValidate('" + htmlId + @"');
                                    });");
                            }
                        if (formField.Type == FormDefFieldType.Number)
                        {
                            if (formField.MinimumValue != null || formField.MaximumValue != null)
                            {
                                var errorMessage = "";
                                if (formField.MinimumValue == null)
                                    errorMessage = "Number should be less than " + formField.MaximumValue;
                                else if (formField.MaximumValue == null)
                                    errorMessage = "Number should be more than " + formField.MinimumValue;
                                else
                                    errorMessage = "Number should be between " + formField.MinimumValue + " and " + formField.MaximumValue;
                                a.JsStr.Append(@"
                                FieldValidationData['" + htmlId + @"'] = new Object();
                                FieldValidationData['" + htmlId + @"'].type = 'MinimumValue';
                                FieldValidationData['" + htmlId + @"'].errorHtmlId = '" + GetHtmlErrorId() + @"';
                                FieldValidationData['" + htmlId + @"'].MinimumValue = " + (formField.MinimumValue == null ? "null" : "'" + formField.MinimumValue + @"'") + @";
                                FieldValidationData['" + htmlId + @"'].MaximumValue = " + (formField.MaximumValue == null ? "null" : "'" + formField.MaximumValue + @"'") + @";
                                FieldValidationData['" + htmlId + @"'].errorMessage = '" + errorMessage + @"'
                                $(JQueryEscape('#" + htmlId + @"')).blur(
                                    function(){
                                        return FieldValidate('" + htmlId + @"');
                                    });");
                            }
                        }
                        if (formField.Type == FormDefFieldType.PickMultiple)
                        {
                            if (formField.Style == FormDefFieldStyle.Checkboxes)
                                if (fieldValue.ReadOnly == FieldReadonlyType.Readonly)
                                    a.JsStr.Append("$('[id^=" + htmlId.Replace(AppPress.IdSep, "\\\\" + AppPress.IdSep) + "]').attr('disabled',true);");
                        }
                        if (formField.Type == FormDefFieldType.Pickone)
                            if (formField.Style == FormDefFieldStyle.DropDown)
                                a.JsStr.Append("$(JQueryEscape('#" + htmlId + "')).val('" + fieldValue.Value + "');");
                            else if (formField.Style == FormDefFieldStyle.Radio)
                            {
                                a.JsStr.Append("$(JQueryEscape('#" + htmlId + AppPress.IdSep + fieldValue.Value + "')).prop('checked','checked');");
                                if (fieldValue.ReadOnly == FieldReadonlyType.Readonly)
                                    a.JsStr.Append("$('[id^=" + htmlId.Replace(AppPress.IdSep, "\\\\" + AppPress.IdSep) + "]').attr('disabled',true);");
                            }
                            else if (formField.Style == FormDefFieldStyle.AutoComplete)
                                a.JsStr.Append("AppPressAutoComplete('" + htmlId + "','" + a.GetHtmlOnChange(formField.fieldName) + "', " + AppPress.LocalInstanceId + ");");
                            else if (formField.Style == FormDefFieldStyle.ImageRotation)
                            {
                                a.JsStr.Append("AppPressRotationImages['" + htmlId + "']=new Array();");
                                var options = ((PickFieldValue)this).GetOptions(a);
                                var defaultImage = "";
                                var optionIndex = 0;
                                foreach (var option in options)
                                {
                                    a.JsStr.Append("var optionObj = new Object();optionObj.id =" + (option.id == null ? "null" : "'" + HttpUtility.JavaScriptStringEncode(option.id) + "'") + ";");
                                    a.JsStr.Append("optionObj.value = '" + HttpUtility.JavaScriptStringEncode(option.value) + "';");
                                    a.JsStr.Append("AppPressRotationImages['" + htmlId + "'][" + optionIndex + "]=optionObj;");
                                    if (option.id == Value)
                                        defaultImage = option.value;
                                    optionIndex++;
                                }
                                a.JsStr.Append("$(JQueryEscape('#Image_" + htmlId + "')).addClass('" + HttpUtility.JavaScriptStringEncode(defaultImage) + "');");
                            }
                    }
                    if (fieldValue.Title != null)
                        a.JsStr.Append("$(JQueryEscape('#fieldContainer" + AppPress.IdSep + fieldValue._GetHtmlId() + "')).prop('title','" + fieldValue.Title + "');");
                }
                //if (!appPressIdFound)
                //    throw new Exception("In Form: " + formData.formDef.formName + " Field: " + formField.fieldName + " is defined as Static but the Skin does not use AppPressId");

                // insert back formContainers skins
                for (int i = 0; i < formContainers.Count(); ++i)
                    skin = skin.Replace(formContainerKeys[i], formContainers[i]);
            }

            return skin;

        }

        internal string GetFieldDescription()
        {
            // for Error Display
            var s = "Form: " + FormData.formDef.formName;
            s += " Field: " + formField.fieldName;
            if (FormData != null)
                s += ", FormDataId: " + FormData.id;
            return s;
        }

        internal string GetHtmlOnChange(AppPress a)
        {
            if (formField.Static && !formField.NonStatic)
                throw new Exception(formField.GetDescription() + " Cannot get OnChange for Static Field");
            switch (formField.Type)
            {
                case FormDefFieldType.Pickone:
                case FormDefFieldType.PickMultiple:
                case FormDefFieldType.Text:
                case FormDefFieldType.TextArea:
                case FormDefFieldType.DateTime:
                case FormDefFieldType.Number:
                case FormDefFieldType.Checkbox:
                case FormDefFieldType.Password:
                    {
                        if (formField.Type == FormDefFieldType.Checkbox && formField.fieldName == "SelectRow")
                        {
                            return "UnSelectRestSelectRow(this)";
                        }
                        bool always = formField.FieldFunctions.Find(t => t.ServerFunctionType == FunctionType.OnChange) != null;
                        if (!always)
                        {
                            always = InvokeOn(a, "OnChange", false);
                        }
                        var str = "OnChange(this," + AppPress.LocalInstanceId + "," + (always ? "true" : "false") + ");";
                        if (!formField.DoNotSaveInDB)
                            str += "SetDirty();";


                        var needAjaxCall = formField.FieldFunctions.Find(t => t.ServerFunctionType == FunctionType.OnChange) != null;
                        if (!needAjaxCall)
                            needAjaxCall = InvokeOn(a, "OnChange", false);
                        if (needAjaxCall)
                        {
                            if (formField.Shortcut != null && a.JsStr != null)
                                a.JsStr.Append("shortcut.add('" + formField.Shortcut + "',function (){HandleFocusShortcut($(\"#\"+JQueryEscape('" + GetHtmlId() + "')))})\n");
                        }



                        return str;
                    }
                default:
                    throw new Exception(formField.GetDescription() + " Cannot Get AppPressOnChange");
            }

        }
        /// <summary>
        /// ???
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public string GetSetValueJSScript(string value)
        {
            return " SetFieldValue(" + FormData.formDefId + @", " + FormData.id + @", " + formField.id + @", " + value + @"); ";
        }
        /// <summary>
        /// Returns script to perform a click on the field
        /// If you want to perform a click on HTML element or from visualization graph, crete a hidden button and add this function to onclick tag of element
        /// </summary>
        /// <returns></returns>
        public string GetOnClickJSScript(bool replaceThis = false)
        {
            var s = "";
            var a = FormData.a;
            if (formField.Type != FormDefFieldType.Button)
                throw new Exception(GetFieldDescription() + " GetOnClickJSScript can be called only for type Button.");
            var ajaxStr = "";
            var thisStr = "this";
            if (replaceThis)
            {
                ajaxStr = "var DialogButtonThis = new Object(); DialogButtonThis.id=\"" + GetHtmlId() + "\";";
                thisStr = "DialogButtonThis";
            }
            ajaxStr += "AjaxFunctionCall(\"OnClick\",\"" + AppPress.LocalInstanceId + "\",\"" + fieldDefId + "\"," + (formField.NoSubmit ? "true" : "false") + "," + thisStr + ");";
            if (formField.Shortcut != null && a.JsStr != null)
                a.JsStr.Append("shortcut.add('" + formField.Shortcut + "',function (){HandleShortcut($(\"#\"+JQueryEscape('" + GetHtmlId() + "')))})\n");
            s += ajaxStr;
            return s;
        }

        public string GetLabel(AppPress a)
        {
            if (formField.fieldName == "SelectRow")
                return "";
            //if (formField.Type == FormDefFieldType.Button && Value != null)
            //    return Value;
            var suffix = "";
            if (a.skinType != SkinType.FO && formField != null && (formField.Required || this.Required) && ReadOnly == FieldReadonlyType.None)
                suffix = "<span style='color:Red'>*</span>";
            if (FieldLabel != null) // fieldValue1 is coming null for Column Headers
                return FieldLabel + suffix;
            var tempfieldName = AppPress.GetLocalizationKeyValue(formField.Label ?? formField.fieldName, false);
            if (tempfieldName != null)
                return tempfieldName + suffix;
            var fieldName = AppPress.InsertSpacesBetweenCaps(formField.fieldName);
            if (formField.Label != null)
            {
                var label = formField.Label;
                if (formField.Type == FormDefFieldType.FormContainerDynamic && formField.OriginalType == (int)FormDefFieldType.FormContainerGrid)
                {
                    label = label.Replace("%Label%", fieldName + suffix, StringComparison.OrdinalIgnoreCase);
                    label = label.Replace("%Count%", ((FormContainerFieldValue)this).GetContainedFormDatas(a).Count.ToString(), StringComparison.OrdinalIgnoreCase);
                }
                return label + suffix;
            }
            return fieldName + suffix;
        }

        internal string GetFieldValue(AppPress a)
        {
            var pFieldValue = a.fieldValue;
            try
            {
                a.fieldValue = this;
                return GetFormattedFieldValue(a, formField.Type != FormDefFieldType.HTML);
            }
            finally
            {
                a.fieldValue = pFieldValue;
            }
        }

        public void ErrorException(string error)
        {
            Error = error;
            throw new AppPressException();
        }
        /// <summary>
        /// ??? internal use only
        /// </summary>
        /// <returns></returns>
        public long? GetDBId()
        {
            return formField.dbId == 0 ? (long?)null : formField.dbId;
        }
    }
    [DataContract]
    public class DateTimeFieldValue : FieldValue
    {
        [DataMember]
        public DateTime? BaseDate = new DateTime(2000, 1, 1); // will use the field as Time entry
        [DataMember]
        public DateTime? MinDateTime = null;
        [DataMember]
        public DateTime? MaxDateTime = null;

        public DateTimeFieldValue() { }
        public DateTimeFieldValue(DateTimeFieldValue fieldValue)
            : base(fieldValue)
        {
            this.BaseDate = fieldValue.BaseDate;
            this.MinDateTime = fieldValue.MinDateTime;
            this.MaxDateTime = fieldValue.MaxDateTime;
        }
    }
    [DataContract]
    public class FileUploadFieldValue : FieldValue
    {
        public FileUploadFieldValue() { }
        public FileUploadFieldValue(FileUploadFieldValue fieldValue)
            : base(fieldValue)
        {
        }
        private string AmazonS3BucketName = null;
        /// <summary>
        /// Sets the Bucket name where uploaded files will be stored, StorageType should be AmazonS3.
        /// </summary>
        /// <param name="bucketName"></param>
        public void SetAmazonS3BucketName(string bucketName)
        {
            if (formField.FileUploadStorage != FileUploadStorageType.AmazonS3)
                throw new Exception("Function: SetAmazonS3BucketName, " + this.GetFieldDescription() + " Should have StorageType AmazonS3.");
            this.AmazonS3BucketName = bucketName;
        }
        /// <summary>
        /// </summary>
        /// <returns>AmazonS3 Bucket Name</returns>
        public string GetAmazonS3BucketName()
        {
            return this.AmazonS3BucketName;
        }
    }
    [DataContract]
    public class NumberFieldValue : FieldValue
    {
        public NumberFieldValue() { }
        public NumberFieldValue(NumberFieldValue fieldValue) : base(fieldValue) { }
        [DataMember]
        public decimal? MinNumber = null;
        [DataMember]
        public decimal? MaxNumber = null;

        public decimal? Number
        {
            get
            {
                return Value == null ? (decimal?)null : decimal.Parse(Value);
            }
            set
            {
                if (value == null)
                    Value = null;
                else
                    Value = value.ToString();
            }
        }
    }
    [DataContract]
    public class PickFieldValue : FieldValue
    {
        /// <summary>
        /// Display for Multiple Pickone
        /// </summary>
        public const string MultiplePickone = "Multiple";
        /// <summary>
        /// Value to for Multiple Pickone
        /// </summary>
        public const string MultiplePickoneValue = "AppPressMultiple";

        internal List<Option> options = null;
        public string autoCompleteTerm;
        public PickFieldValue() { }
        public PickFieldValue(PickFieldValue fieldValue)
            : base(fieldValue)
        {
            this.autoCompleteTerm = fieldValue.autoCompleteTerm;
            this.options = fieldValue.options;
        }
        internal List<Option> GetOptions(AppPress a, string id)
        {
            if (formField.optionsCache != null)
                return CloneOptions();
            var pFieldValue = a.fieldValue;
            var pServerFunction = a.serverFunction;
            options = null;
            var checkboxOptionsError = "";
            try
            {
                a.fieldValue = this;
                var optionsFunction = formField.FieldFunctions.Find(t => t.ServerFunctionType == FunctionType.Options);
                Type t1 = null;
                switch (formField.formDef.FormType)
                {
                    case FormType.UserControlScalarForm:
                        t1 = Util.GetType(a, formField.formDef, formField.formDef.formFields[0].id); // will have only one field
                        break;
                    case FormType.MergedForm:
                        t1 = Util.GetType(a, formField.formDef, formField.id);
                        break;
                    default:
                        t1 = Util.GetType(a, FormData.formDef, formField.id);
                        break;
                }
                if (t1 == null && optionsFunction == null)
                    throw new Exception(formField.GetDescription() + " Could not find Type for the field in namespace ApplicationClasses. Check if TT has been built.");
                a.AddToDependentFields = true;
                a.sourceField = this;
                if (t1 != null)
                {
                    var method = Util.GetMethod(a, "Options", new Type[] { AppPress.Settings.ApplicationAppPress, t1 });
                    if (method != null)
                    {
                        if (optionsFunction != null)
                            throw new Exception("Cannot have Calc and Options Function defined for Field: " + formField.fieldName + " in Form: " + formField.formDef.formName);
                        // set autocomplete term only for field in focus
                        if (formField.Type == FormDefFieldType.Pickone && pFieldValue != null && pFieldValue.fieldDefId == formField.id)
                        {
                            var pickoneFieldValue = (PickFieldValue)this;
                            pickoneFieldValue.autoCompleteTerm = a.autoCompleteTerm;
                        }
                        Object o = this;
                        if (o.GetType() != t1)
                        {
                            o = Activator.CreateInstance(t1, new object[] { this });
                            a.sourceField = (FieldValue)o;
                        }
                        var obj = Util.InvokeMethod(a, method, new object[] { a, o });
                        if (obj == null)
                            return new List<Option>();
                        if (obj.GetType() == typeof(List<Option>))
                        {
                            checkboxOptionsError = "Options with Parameter " + t1.ToString();
                            return (List<Option>)obj;
                        }
                        if (obj.GetType() != typeof(string))
                            throw new Exception("Options with Parameter " + t1.ToString() + " should return string or List<Option>");
                        checkboxOptionsError = "Options with Parameter " + t1.ToString();
                        options = AppPressLogic.GetOptionsFromQuery(a, this, (string)obj);
                    }
                }
                string tableName = null;
                if (options == null)
                {
                    if (formField.Type == FormDefFieldType.PickMultiple)
                        tableName = formField.SaveTableName;
                    else
                        tableName = FormData.formDef.TableName;
                    checkboxOptionsError = "Table: " + tableName;
                    if (optionsFunction != null)
                    {
                        a.serverFunction = optionsFunction;
                        if (a.functionCall == "AutoCompleteOptions")
                            ((PickFieldValue)a.fieldValue).autoCompleteTerm = a.autoCompleteTerm;
                        var obj = Util.InvokeMethod(a, optionsFunction.method, new object[] { a, a.fieldValue });
                        if (obj.GetType() == typeof(string))
                            options = AppPressLogic.GetOptionsFromQuery(a, this, (string)obj);
                        else
                            options = (List<Option>)obj;
                        if (a.fieldValue.formField.Style == FormDefFieldStyle.DropDown)
                        {
                            if (a.fieldValue.Value == null || (!a.fieldValue.formField.Required && !a.fieldValue.Required))
                                options.Insert(0, new Option { id = null, value = "" });
                        }
                    }
                    else if (formField.Type == FormDefFieldType.Checkbox)
                    {
                        options = new List<Option>();
                        options.Add(new Option { id = "0", value = "No" });
                        options.Add(new Option { id = "1", value = "Yes" });
                    }
                    else if (!formField.RowFilter) // this will be filled from Domain Function
                    {
                        var message = "";
                        if (formField.Type == FormDefFieldType.PickMultiple)
                        {
                            if (formField.SaveTableName != null)
                                message += "Cannot Find Foreign Key for Column: " + formField.fieldName + " in Table: " + formField.SaveTableName + " to generate options for " + formField.Type + "<br/>";
                        }
                        else if (tableName != null)
                            message += "Cannot Find Foreign Key for Column: " + formField.fieldName + " in Table: " + tableName + " to generate options for " + formField.Type + "<br/>";
                        message += "Could not find Options function" + FormData.formDef._GenerateCode(0, formField, "Options");
                        this.Error = "<a onclick=\"AlertMessage('" + HttpUtility.HtmlAttributeEncode(message.Replace("'", "\\'").Replace("\"", "\\\"").Replace(Environment.NewLine, "<br/>")) + "',850)\"<span style='color:red'>X</span></a>";
                        options = new List<Option>();
                    }
                    else
                        options = new List<Option>();
                }

            }
            finally
            {
                a.AddToDependentFields = false;
                a.serverFunction = pServerFunction;
                a.fieldValue = pFieldValue;
            }
            if (formField.Type == FormDefFieldType.Checkbox)
            {
                if (options.Count() != 2)
                    throw new Exception(formField.GetDescription() + " Options for Checkbox should have 2 elements. Source: " + checkboxOptionsError);
                if (options.Find(t => t.id == "0") == null)
                    throw new Exception(formField.GetDescription() + " Options for Checkbox should have a element with Id 0. Source: " + checkboxOptionsError);
                if (options.Find(t => t.id == "1") == null)
                    throw new Exception(formField.GetDescription() + " Options for Checkbox should have a element with Id 1. Source: " + checkboxOptionsError);
            }
            return options;
        }

        private List<Option> CloneOptions()
        {
            var opts = new List<Option>();
            foreach (var option in formField.optionsCache)
            {
                var opt = new Option { id = option.id, value = option.value };
                if (!opt.value.IsNullOrEmpty() && opt.value.StartsWith("LKey"))
                {
                    opt.value = AppPress.GetLocalizationKeyValue(opt.value);
                }
                opts.Add(opt);
            }
            if (Value != null && formField.Required)
                opts.RemoveAll(t => t.id == null); // for required field if options is selected then no need for blnk option
            return opts;
        }

        /// <summary>
        /// Get the Option Associated with the Pick field
        /// </summary>
        /// <param name="a"></param>
        /// <param name="id">Id of the option</param>
        /// <returns></returns>
        public Option GetOption(AppPress a, string id)
        {
            Option option = null;
            if (options != null && formField.Style != FormDefFieldStyle.AutoComplete)
                option = options.Find(t => t.id == id);
            if (option == null)
            {
                var ioptions = GetOptions(a, id);
                option = ioptions.Find(t => t.id == id);
            }
            if (option == null)
                if (Error != null || id == null)
                    return new Option { id = id, value = "" };
                else
                    throw new Exception(GetFieldDescription() + " Could not find option with id:" + id);
            return option;
        }
        /// <summary>
        /// returns option for Field Value
        /// </summary>
        /// <param name="a"></param>
        /// <returns></returns>
        public Option GetOption(AppPress a)
        {
            AppPress.CheckSubmitIfStaticUsage(this);
            return GetOption(a, Value);
        }
        public string GetSelectedOptionValue(AppPress a)
        {
            if (formField.Type != FormDefFieldType.Pickone)
                throw new Exception("GetSelectedOptionValue can only be called for field of type Pickone");
            var options = GetOption(a, Value);
            if (options != null)
                return options.value;
            return string.Empty;
        }
        public Dictionary<string, string> GetUserControlParameters()
        {
            return formField.UserControlParameters;
        }

        public List<Option> GetOptions(AppPress a)
        {
            if (formField.RowFilter)
                return options;
            options = GetOptions(a, null);
            if (options.Count == 1 && this.Value == null && (this.Required || this.formField.Required))
                this.Value = options[0].id;
            return options;
        }
    }
    [DataContract]
    public class ButtonFieldValue : FieldValue
    {
        public ButtonFieldValue() { }
        public ButtonFieldValue(ButtonFieldValue fieldValue)
            : base(fieldValue)
        {
        }

    }

    [DataContract]
    public class FormContainerDynamicFieldValue : FormContainerFieldValue
    {
        /// <summary>
        /// Used to bind Form from Url to MasterContentArea in MasterForm
        /// </summary>
        /// <param name="a"></param>
        /// <returns></returns>
        public List<FormData> GetMasterContainer(AppPress a)
        {
            if (a.MasterContentAreaFormName == null)
                throw new Exception("In GetMasterContainer, Request must have formName");

            string id = null;
            var s = a.Request["s"];
            if (s != null)
            {
                s = Util.DecryptDES(s);
                var s1 = s.Split(new string[] { AppPress.IdSep }, StringSplitOptions.None);
                id = s1[1];
            }
            else
                id = a.PageURL["id"];
            if (id == null)
                id = AppPress.GetUniqueId().ToString();

            if (id == "l")
                id = a.sessionData.loginUserId;
            var formDef = AppPress.FindFormDef(a.MasterContentAreaFormName);
            if (formDef.FormType == FormType.MasterForm)
                throw new Exception("Cannot Redirect to Master Form:" + a.MasterContentAreaFormName);
            var formDatas = new List<FormData>();

            var formData = FormData.InitializeFormData(a, formDef, id);
            formData.containerFieldValue = a.fieldValue;
            formDatas.Add(formData);
            return formDatas;
        }

        /// <summary>
        /// Use in FormContainerDynamic Control to dynamically bind a single Form to FormContainer control. Some use cases are
        /// - Embed a form inside another form
        /// - Embed a form depending on another pickone field
        /// If the field has another type of form bound to it deletes them
        /// If FormName is bound to a table returns forms bound to parent form with foreign Key field in formName
        /// If no form is bound returns a new form of type formName
        /// </summary>
        /// <param name="a"></param>
        /// <param name="FormClass">Type of the Form to bind to. e.g. typeof(UserClass)</param>
        /// <returns></returns>
        public List<FormData> BindSingleForm(AppPress a, Type FormClass)
        {
            var fieldInfo = FormClass.GetField("formDefId");
            if (fieldInfo == null)
                throw new AppPressException("Could not find formDefId in Class: " + FormClass.Name);
            return BindSingleForm(a, (long)fieldInfo.GetValue(null));
        }

        internal List<FormData> BindSingleForm(AppPress a, long? formDefId)
        {
            var formDatas = new List<FormData>();
            FormDef subFormDef = null;
            if (formDefId != null)
            {
                subFormDef = AppPress.FindFormDef(formDefId.Value);
                if (subFormDef == null)
                    throw new Exception(a.fieldValue.GetFieldDescription() + ". Function BindSingleForm. Could not find Form with FormDefId: " + formDefId);
                if (formField.OriginalType != (int)FormDefFieldType.EmbeddedForm)
                {
                    var containerIdFormField = subFormDef.formFields.Find(t => t.Type == FormDefFieldType.ForeignKey);
                    if (subFormDef.TableName != null && containerIdFormField == null && a.fieldValue.FormData.formDef.TableName != null)
                        throw new Exception("Could not Find field of Type ForeignKey in Form: " + subFormDef.formName);
                }
                var subFormDefId = subFormDef.id;
                if (formDatas.Count == 0)
                    formDatas = AppPressLogic.GetContainerRowForms(a, subFormDefId);
                if (formDatas.Count == 0)
                {
                    var formData = FormData.NewFormData(a, subFormDef, a.fieldValue);
                    formDatas.Add(formData);
                }
            }
            foreach (var formData in a.formDatas.FindAll(t => t.containerFieldValue == a.fieldValue && (subFormDef == null || t.formDef.id != subFormDef.id)))
                Util.ApplyOnChildFormDatas(a.formDatas, formData, t => t.IsDeleted = true);

            return formDatas;
        }
    }
    [DataContract]
    public class FormContainerGridFieldValue : FormContainerFieldValue
    {
        [DataMember]
        public List<string> HiddenColumns = null;

        [DataMember]
        public bool allowMultiSelect = true;
        /// <summary>
        /// Add a New Form to the grid
        /// </summary>
        /// <param name="a"></param>
        public void AddNewForm(AppPress a)
        {
            AppPressLogic.AddNewForm(a, (long?)null, this);
        }
        /// <summary>
        /// Add formData in Parameter to the grid
        /// </summary>
        /// <param name="a"></param>
        /// <param name="formData">FormData to add</param>
        public void AddNewForm(AppPress a, FormData formData)
        {
            AppPressLogic.AddNewForm(a, formData, this);
        }
        /// <summary>
        /// Gets the only selected form. If no form is selected returns null. If more than one selected then returns null.
        /// </summary>
        /// <returns></returns>

        public FormData TryGetSingleSelection()
        {
            var selectedFormDatas = AppPressLogic.GetSelectedFormDatas(FormData.a, this);
            if (selectedFormDatas.Count() != 1)
                return null;
            return selectedFormDatas.First();
        }
        /// <summary>
        /// For Grid allowing Multiple Row Selection. use this to disable or Enable MultiSelection at Runtime
        /// Setting to false will hide the MultSelect CheckBox, Setting to true will show the multi select checkbox
        /// If set to false and the gris has already more than one row selected, all selection will be deselected
        /// </summary>
        /// <param name="allow"></param>
        public void AllowMultiSelect(bool allow)
        {
            allowMultiSelect = allow;
            var formDef = GetRowFormDef();
            var fv = formDef.formFields.Find(t => t.fieldName == "SelectRow");
            if (fv != null)
            {
                FormData.a.ExecuteJSScript("$(JQueryEscape('[id^=SelectAll_AppPress:5:" + formDef.id + ":" + fv.id + "]')).prop('disabled'," + (allowMultiSelect ? "false" : "true") + ");");
                if (!allow && GetSelection().Count > 1)
                    FormData.a.ExecuteJSScript("UnSelectRestSelectRow($(JQueryEscape('[id^=SelectAll_AppPress:5:" + formDef.id + ":" + fv.id + "]'))[0]);");
            }
        }
        public void HideColumn(string fieldName)
        {
            if (HiddenColumns == null)
                HiddenColumns = new List<string>();
            if (HiddenColumns.Find(t => t == fieldName) == null)
                HiddenColumns.Add(fieldName);
        }
        /// <summary>
        /// Shows all columns of a Grid. Should be called in Init where Columns are being Hidden
        /// </summary>
        public void ShowAllColumns()
        {
            if (HiddenColumns != null)
                HiddenColumns.Clear();
        }
        public void ShowColumn(string fieldName)
        {
            if (HiddenColumns != null)
                HiddenColumns.RemoveAll(t => t == fieldName);
        }
        /// <summary>
        /// Gets the only selected form. If no form or more than 1 selected then throws Error
        /// </summary>
        /// <returns></returns>
        public FormData GetSingleSelection()
        {
            var selectedFormDatas = AppPressLogic.GetSelectedFormDatas(FormData.a, this);
            if (selectedFormDatas.Count() != 1)
            {
                throw new AppPressException("Please select any one row by clicking check-box on the left column in required row to execute this step.");
            }
            return selectedFormDatas.First();
        }
        /// <summary>
        /// return List<FormData> selected by the user
        /// </summary>
        /// <returns></returns>
        public List<FormData> GetSelection()
        {
            return AppPressLogic.GetSelectedFormDatas(FormData.a, this);
        }
        /// <summary>
        /// Deletes the Selected forms
        /// </summary>
        public void DeleteSelectedSubForms()
        {
            var a = FormData.a;
            AppPressLogic.DeleteSelectedSubForms(a, null, true);
        }
    }
    [DataContract]
    public class FormContainerFieldValue : FieldValue
    {
        /// <summary>
        /// used Internally
        /// </summary>
        public FormContainerFieldValue() { }
        internal int rowNumber = 0;
        /// <summary>
        /// used Internally
        /// </summary>
        /// <returns></returns>
        public List<FormData> GetFieldFormContainer()
        {
            //formData.a.AddDependentField(this);
            return FormData.GetFieldFormContainer(formField.fieldName);
        }
        /// <summary>
        /// used Internally
        /// </summary>
        /// <param name="value"></param>
        public void SetFieldFormContainer(List<FormData> value)
        {
            //formData.a.AddReverseDependentField(this);
            SetContainedFormDatas(value);
        }
        internal void SetContainedFormDatas(IEnumerable<FormData> newFormDatas)
        {
            var a = FormData.a;
            var dfds = new List<FormData>();
            // set the filters
            if (formField.rowFormDef != null)
            {
                var sortableFields = formField.formDef.formFields.FindAll(t => t.IsSortingControl());
                foreach (var sortableField in sortableFields)
                {
                    var sortableFieldValue = (PickFieldValue)FormData.fieldValues.Find(t => t.formField.fieldName == sortableField.fieldName);
                    if (sortableFieldValue != null && sortableFieldValue.Value != null)
                    {
                        var fieldName = sortableFieldValue.formField.fieldName.Substring("Sortable".Length);
                        var nFormDatas = (List<FormData>)newFormDatas;
                        nFormDatas.Sort(delegate (FormData x, FormData y)
    {
        var xf = x.GetFieldValue(fieldName);
        var yf = y.GetFieldValue(fieldName);
        int order = int.Parse(sortableFieldValue.Value) == 2 ? -1 : 1;
        if (xf.formField.Type == FormDefFieldType.Number)
        {
            if (xf.Value == null)
                return -1 * order;
            if (yf.Value == null)
                return 1 * order;
            if (xf.Value == yf.Value)
                return 0;
            if (decimal.Parse(xf.Value) < decimal.Parse(yf.Value))
                return -1 * order;
            return 1 * order;
        }
        return (xf.GetFormattedFieldValue(a, false, DAOBasic.DBDateTimeFormat)).CompareTo(yf.GetFormattedFieldValue(a, false, DAOBasic.DBDateTimeFormat)) * order;
    });
                        break;
                    }
                }
                foreach (var f in formField.rowFormDef.formFields)
                {
                    var columnField = formField.formDef.formFields.Find(t => t.fieldName == "RowFilter" + f.fieldName);
                    if (columnField != null)
                    {
                        var filterFieldValue = (PickFieldValue)FormData.fieldValues.Find(t => t.formField.fieldName == columnField.fieldName);
                        filterFieldValue.options = new List<Option>();
                        foreach (var fData in newFormDatas)
                        {
                            var cValue = fData.GetFieldValue(f.fieldName);
                            if (cValue != null)
                            {
                                var val = cValue.Value ?? "AppPressBlank";
                                if (filterFieldValue.options.Find(t => t.id == val) == null)
                                    filterFieldValue.options.Add(new Option { id = val, value = cValue.GetFormattedFieldValue(a) });
                            }
                        }
                        var v = filterFieldValue.Value;
                        a.AddDependentField(filterFieldValue, this);
                        if (v != null)
                        {
                            // removed from selection if option is removed due to modify or delete in grid
                            var vs = v.Split(new string[] { "," }, StringSplitOptions.None).ToList();
                            vs.RemoveAll(t1 => filterFieldValue.options.Find(t => t.id == t1) == null);
                            filterFieldValue.Value = string.Join(",", vs.ToArray());
                            if (filterFieldValue.Value.IsNullOrEmpty())
                                filterFieldValue.Value = null;
                            // Filter the records
                            foreach (var fData in newFormDatas)
                            {
                                var cValue = fData.GetFieldValue(f.fieldName);
                                if (cValue != null)
                                    if (vs.Find(t => t == (cValue.Value ?? "AppPressBlank")) == null)
                                        dfds.Add(fData);
                            }
                        }
                    }
                }
            }
            foreach (var newFormData in newFormDatas)
            {
                newFormData.containerFieldValue = this;
                newFormData.pageStackIndex = FormData.pageStackIndex;
                var containerIdFieldValue = newFormData.fieldValues.Find(t => t.formField.Type == FormDefFieldType.ForeignKey);
                if (containerIdFieldValue != null && containerIdFieldValue.Value == null)
                    containerIdFieldValue.Value = FormData.id;
                newFormData.a = a;
                newFormData.pageStackIndex = FormData.pageStackIndex;
                if (newFormData.id != null)
                    foreach (var f in newFormDatas)
                        if (f != newFormData && f.formDef == newFormData.formDef && f.id == newFormData.id)
                            throw new Exception("In Form Container Grid: " + formField.fieldName + " Cannot have 2 members with same FormName: " + f.formDef.formName + " and same id: " + f.id + " ");
            }
            var pLen = a.formDatas.Count();
            var addedFormDatas = new List<FormData>();
            foreach (var newFormData in newFormDatas)
            {
                var formDataUsed = newFormData;
                var foundIndex = a.formDatas.FindIndex(t1 => t1.formDefId == newFormData.formDefId && t1.id == newFormData.id && t1.containerFieldValue == newFormData.containerFieldValue);
                if (foundIndex != -1 && foundIndex < pLen)
                {
                    if (a.formDatas[foundIndex] != newFormData)
                    {
                        if (a.formDatas[foundIndex].IsDeleted)
                            a.formDatas[foundIndex].IsDeleted = false;
                        if (a.formDatas[foundIndex].IsSubmitted)
                        {
                            // add missing fields
                            // if Static Field with RetailValue is changed from Code, the value should be set from DB
                            a.formDatas[foundIndex].MergeFields(newFormData);
                            a.formDatas[foundIndex].IsSubmitted = false;
                        }
                        // keep the sorting order same as newFormDatas
                        var pFormData = a.formDatas[foundIndex];
                        a.formDatas.RemoveAt(foundIndex);
                        addedFormDatas.Add(pFormData);
                        formDataUsed = pFormData;
                        var idx = dfds.FindIndex(t => t == newFormData);
                        if (idx != -1)
                            dfds[idx] = pFormData;
                    }
                    else
                    {
                        a.formDatas.RemoveAt(foundIndex);
                        addedFormDatas.Add(newFormData);
                        formDataUsed = null;
                    }
                }
                else
                    addedFormDatas.Add(newFormData);
                if (this.ReadOnly == FieldReadonlyType.Readonly && formDataUsed != null)
                    foreach (var fieldValue in formDataUsed.fieldValues)
                        if (fieldValue.formField.Type != FormDefFieldType.Checkbox || fieldValue.formField.fieldName != "SelectRow")
                            if (fieldValue.formField.Type != FormDefFieldType.Button)
                                fieldValue.ReadOnly = this.ReadOnly;
            }

            a.formDatas.AddRange(addedFormDatas);
            var deletedFormDatas = a.formDatas.FindAll(t => t.containerFieldValue == this && !t.IsDeleted && !t.fromPopupSave && newFormDatas.FirstOrDefault(t1 => t1.formDefId == t.formDefId && t1.id == t.id) == null);
            foreach (var deletedFormData in deletedFormDatas)
                Util.ApplyOnChildFormDatas(a.formDatas, deletedFormData, t => dfds.Add(t));
            a.formDatas.RemoveAll(t => dfds.Find(t1 => t1 == t) != null);
            a.CheckContainerfieldValues();
        }
        public List<FormData> GetContainedFormDatas(AppPress a)
        {
            return a.formDatas.FindAll(t => t.containerFieldValue == this);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns>FormDef of Row in Grid</returns>
        public FormDef GetRowFormDef()
        {
            return formField.rowFormDef;
        }
        /// <summary>
        /// read FormDatas for the FormContainer using the uery
        /// </summary>
        /// <param name="a"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        public List<FormData> ReadFormDatas(AppPress a, string query)
        {
            var rowFromDef = formField.GetContainerRowFormDef(a);
            if (rowFromDef == null)
                throw new Exception("ReadFormDatas can be called only for Field of type FormContainerGrid");
            return FormData.ReadFormDatas(a, this, rowFromDef, query);
        }
        /// <summary>
        /// Read FormDatas of Type given from a query
        /// </summary>
        /// <param name="a"></param>
        /// <param name="formClass">Class of form to read</param>
        /// <param name="query"></param>
        /// <returns></returns>
        public List<FormData> ReadFormDatas(AppPress a, Type formClass, string query)
        {
            var fieldInfo = formClass.GetField("formDefId");
            if (fieldInfo == null)
                throw new AppPressException("Could not find formDefId in Class: " + formClass.Name);
            var rowFromDef = AppPress.FindFormDef((long)fieldInfo.GetValue(null));
            if (rowFromDef == null)
                throw new Exception("Could not find Form Definition for Form with Name: " + formClass.Name);
            return FormData.ReadFormDatas(a, this, rowFromDef, query);
        }
        public string GetCSV(AppPress a)
        {
            a.CalcFormDatas(FormData, this, true);
            var formDatas = this.GetContainedFormDatas(a);
            var csv = "";
            var csvh = "";
            var oldCallReason = a.CallReason;
            try
            {
                a.CallReason = CallReasonType.PageLoad;
                foreach (var formData in formDatas)
                {
                    var csvl = "";
                    foreach (var fieldValue in formData.fieldValues)
                    {
                        if (fieldValue.formField.Type == FormDefFieldType.Button)
                            continue;
                        if (fieldValue.formField.fieldName == "SelectRow")
                            continue;
                        var hiddenCoulumns = ((FormContainerGridFieldValue)this).HiddenColumns;
                        if (hiddenCoulumns != null && hiddenCoulumns.Find(t => t == fieldValue.formField.fieldName) != null)
                            continue;
                        if (csv.Length == 0)
                            csvh += ",\"" + fieldValue.GetLabel(a).Replace("\"", "\"\"") + "\"";
                        csvl += ",\"" + fieldValue.GetFormattedFieldValue(a, false, null).Replace("\"", "\"\"") + "\"";
                    }
                    if (csvl.Length > 0)
                        csvl = csvl.Substring(1);
                    csv += csvl + "\n";
                }
            }
            finally
            {
                a.CallReason = oldCallReason;
            }
            if (csvh.Length > 0)
                csvh = csvh.Substring(1);
            return csvh + "\n" + csv;
        }
    }

}

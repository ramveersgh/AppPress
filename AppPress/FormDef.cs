using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Globalization;
using System.Collections.ObjectModel;
using System.Collections;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Xml;


namespace AppPressFramework
{
    [DataContract]
    public class MergeIntoForm
    {
        [DataMember]
        public string FormName;
        [DataMember]
        public string BeforeFieldName = null;
    }
    [DataContract]
    public class FormDef
    {
        [DataMember]
        public string formName;
        [DataMember]
        public FormType FormType;
        [DataMember]
        public string TableName;

        [DataMember]
        public List<FormField> formFields = new List<FormField>();
        [DataMember]
        public long id;
        [DataMember]
        internal string MasterFormName;
        [DataMember]
        internal bool NonSecure = false;
        [DataMember]
        internal bool DoNoSaveInDB = false;
        [DataMember]
        internal FormField ContainerFormField = null;
        [DataMember]
        internal string CSSClass = null;
        internal int functionId;
        [DataMember]
        public List<MergeIntoForm> MergeIntoForms = new List<MergeIntoForm>();
        /// <summary>
        /// ???
        /// </summary>
        [DataMember]
        public bool Pivot;
        [DataMember]
        internal int GenerationType = 0; // 0: Not Generated, 1: Row Generated, 2: Popup Generated
        [DataMember]
        internal string PrimaryKey;
        [DataMember]
        internal long extensionFormDefId = 0; // formDefId which is extended by this FormDef

        [DataMember]
        internal List<FormSkin> Skins = new List<FormSkin>();
        internal long dbId; // id for formDef loaded for DB. Like Survey
        // Assembly where the form is defined
        internal AppPressAssembly assembly = null;

        internal List<MethodCache> InitMethods = null;
        internal List<MethodCache> BeforeSaveMethods = null;
        internal List<MethodCache> AfterSaveMethods = null;
        internal static List<Type> knownTypes = null;
        internal Type ClassType;

        internal FormDef Clone()
        {
            var formDef = (FormDef)MemberwiseClone();
            formDef.assembly = assembly;
            formDef.id = AppPress.GetUniqueId();
            var formFields = formDef.formFields;
            formDef.formFields = new List<FormField>();
            for (int i = 0; i < formFields.Count(); ++i)
            {
                var formField = formFields[i].Clone();
                formField.id = AppPress.GetUniqueId();
                formField.formDef = formDef;
                formDef.formFields.Add(formField);
            }
            for (int i = 0; i < formFields.Count(); ++i)
                if (formFields[i].containerFormField != null)
                    formDef.formFields[i].containerFormField = formDef.GetFormField(formFields[i].containerFormField.fieldName);
            return formDef;
        }
        internal FormDef()
        {
        }
        /// <summary>
        /// ???
        /// </summary>
        /// <param name="formType"></param>
        /// <param name="formName"></param>
        /// <param name="formFields"></param>
        public FormDef(FormType formType, string formName, List<FormField> formFields)
        {
            FormType = FormType.MergedForm;
            this.formName = formName;
            this.FormType = formType;
            this.formFields.AddRange(formFields);
        }
        internal FormField GetFormField(string fieldName)
        {
            return formFields.Find(t => t.fieldName.Equals(fieldName, StringComparison.OrdinalIgnoreCase));
        }
        internal FormField GetFormField(long id)
        {
            return formFields.Find(t => t.id == id);
        }
        private static List<FormField> LoadFields(AppPress a, FormDef formDef, XElement fieldEls, List<FormDef> formDefs, bool keepOriginal, string containerNode)
        {
            var formFields = new List<FormField>();
            foreach (XElement fieldEl in fieldEls.Elements())
            {
                var formField = new FormField();
                formField.id = AppPress.GetUniqueId();
                formFields.Add(formField);
                formField.formDef = formDef;
                FormDef popupFormDef = null;
                FormDef rowFormDef = null;
                if (fieldEl.Name.LocalName == "SelectRow")
                {
                    if (containerNode != "RowFields")
                        throw new XMLParsingException(fieldEl, "SelectRow Field can only be used in RowFields");
                    formField.fieldName = fieldEl.Name.LocalName;
                    formField.Type = FormDefFieldType.Checkbox;
                    formField.DoNotSaveInDB = true;
                }
                else
                {
                    try
                    {
                        formField.Type = (FormDefFieldType)Enum.Parse(typeof(FormDefFieldType), fieldEl.Name.LocalName);
                        //if (formField.Type == FormDefFieldType.MergedForm)
                        //    throw new XMLParsingException(fieldEl, "FieldType: " + fieldEl.Name.LocalName + " is not allowed Field: " + formField.fieldName + " in Form: " + formDef.formName + " ");
                    }
                    catch (Exception)
                    {
                        throw new XMLParsingException(fieldEl, "FieldType: " + fieldEl.Name.LocalName + " is not found for  Field: " + formField.fieldName + " in Form: " + formDef.formName + " ");
                    }

                    switch (formField.Type)
                    {
                        case FormDefFieldType.FormContainerDynamic:
                            formField.Style = FormDefFieldStyle.None;
                            break;
                        case FormDefFieldType.Pickone:
                            formField.Style = FormDefFieldStyle.DropDown;
                            break;
                        case FormDefFieldType.Button:
                            formField.Style = FormDefFieldStyle.Button;
                            formField.DoNotSaveInDB = true;
                            break;
                        case FormDefFieldType.FileUpload:
                            formField.UploadDetail = new UploadFileDetail();
                            break;
                        case FormDefFieldType.HTML:
                            formField.DoNotSaveInDB = true;
                            formField.Static = true;
                            break;
                    }

                    foreach (var fieldAttribute in fieldEl.Attributes())
                    {
                        switch (fieldAttribute.Name.LocalName)
                        {
                            case "Name":
                                formField.fieldName = fieldAttribute.Value;
                                if (!System.Text.RegularExpressions.Regex.IsMatch(formField.fieldName, @"^[a-zA-Z0-9_-_]+$"))
                                    throw new XMLParsingException(fieldEl, "In FormDef: " + formDef.formName + " Field: " + formField.fieldName + " should be alpha numeric with first letter not as numeric.");
                                if (formFields.Find(t => t != formField && t.fieldName == formField.fieldName) != null)
                                    //  check if field.fieldName is unique in formDef. Look at fields before this
                                    throw new XMLParsingException(fieldEl, "Duplicate Field: " + formField.fieldName + " in Form: " + formDef.formName + " ");
                                break;
                            case "Type":
                                if (formField.Type != FormDefFieldType.UserControlScalar)
                                    goto default;
                                formField.ExtensionFormName = fieldAttribute.Value;
                                break;
                            case "Width":
                                if (containerNode != "RowFields")
                                    throw new XMLParsingException(fieldEl, "FormDef:" + formDef.formName + " Field: " + formField.fieldName + " Attribute Width is allowed only for RowFields.");
                                if (formField.Type == FormDefFieldType.ForeignKey)
                                    goto default;
                                formField.width = fieldAttribute.Value;
                                break;
                            default:
                                throw new XMLParsingException(fieldEl, "In FormDef:" + formDef.formName + " Field: " + fieldEl.Name.LocalName + " has a invalid Attribute:" + fieldAttribute.Name);
                        }
                    }
                }
                if (formField.fieldName == null)
                    throw new XMLParsingException(fieldEl, "In Form:" + formDef.formName + " Field: " + formField.fieldName + " should have Name attribute.");
                if (formField.Type == 0)
                    throw new XMLParsingException(fieldEl, "Missing Type in Form:" + formDef.formName + " Field: " + formField.fieldName);
                if (formField.Type != FormDefFieldType.Button && formField.fieldName.IndexOf(' ') != -1)
                    throw new XMLParsingException(fieldEl, "In Form:" + formDef.formName + " Field: " + formField.fieldName + " should not have space.");
                int popupWidth = 0;
                int popupHeight = 0;
                string popupPosition = null;
                foreach (var fieldSubEl in fieldEl.Elements())
                {
                    switch (fieldSubEl.Name.LocalName)
                    {
                        case "FormName":
                            if (formField.Type != FormDefFieldType.Redirect && formField.Type != FormDefFieldType.EmbeddedForm)
                                throw new XMLParsingException(fieldSubEl, "FormDef:" + formDef.formName + " Field: " + fieldSubEl.Name.LocalName + " has Invalid Node:" + fieldSubEl.Name.LocalName + ". This is allowed only for DateTime field type.");
                            formField.FormNameProperty = fieldSubEl.Value;
                            break;
                        case "Style":
                            foreach (var fieldAttribute in fieldSubEl.Attributes())
                            {
                                switch (fieldAttribute.Name.LocalName)
                                {
                                    case "TimeFormat":
                                        try
                                        {
                                            formField.TimeFormat = (TimeFormat)Enum.Parse(typeof(TimeFormat), fieldAttribute.Value);
                                        }
                                        catch (Exception)
                                        {
                                            throw new XMLParsingException(fieldEl, "TimeFormat: " + fieldAttribute.Value + " is not found for Field: " + formField.fieldName + " in Form: " + formDef.formName + " ");
                                        }
                                        break;
                                    default:
                                        throw new XMLParsingException(fieldSubEl, "FormDef:" + formDef.formName + " Field: " + fieldSubEl.Name.LocalName + " has Invalid Attribute:" + fieldAttribute.Name);
                                }
                            }
                            {

                                try
                                {
                                    formField.Style = (FormDefFieldStyle)Enum.Parse(typeof(FormDefFieldStyle), fieldSubEl.Value);
                                }
                                catch (Exception)
                                {
                                    throw new XMLParsingException(fieldEl, "Style: " + fieldSubEl.Value + " is not found for Field: " + formField.fieldName + " in Form: " + formDef.formName + " ");
                                }
                                switch (formField.Type)
                                {
                                    case FormDefFieldType.FormContainerDynamic:
                                        if (formField.Style != FormDefFieldStyle.None)
                                            throw new XMLParsingException(fieldSubEl, "Invalid Style in Form:" + formDef.formName + " Field: " + formField.fieldName);
                                        break;
                                    case FormDefFieldType.Button:
                                        if (formField.Style != FormDefFieldStyle.Button && formField.Style != FormDefFieldStyle.Link)
                                            throw new XMLParsingException(fieldSubEl, "Invalid Style in Form:" + formDef.formName + " Field: " + formField.fieldName);
                                        break;
                                    case FormDefFieldType.Text:
                                        if (formField.Style != FormDefFieldStyle.UpperCase && formField.Style != FormDefFieldStyle.LowerCase && formField.Style != FormDefFieldStyle.TitleCase)
                                            throw new XMLParsingException(fieldSubEl, "Invalid Style in Form:" + formDef.formName + " Field: " + formField.fieldName);
                                        break;
                                    case FormDefFieldType.TextArea:
                                        if (formField.Style != FormDefFieldStyle.RichTextCKEditorFull && formField.Style != FormDefFieldStyle.RichTextCKEditorStandard && formField.Style != FormDefFieldStyle.RichTextCKEditorBasic)
                                            if (formField.Style != FormDefFieldStyle.UpperCase && formField.Style != FormDefFieldStyle.LowerCase && formField.Style != FormDefFieldStyle.TitleCase)
                                                throw new XMLParsingException(fieldSubEl, "Invalid Style in Form:" + formDef.formName + " Field: " + formField.fieldName);
                                        break;
                                    case FormDefFieldType.Pickone:
                                        if (formField.Style == FormDefFieldStyle.None)
                                            formField.Style = FormDefFieldStyle.DropDown;
                                        if (formField.Style != FormDefFieldStyle.DropDown && formField.Style != FormDefFieldStyle.Radio && formField.Style != FormDefFieldStyle.AutoComplete && !formField.Static)
                                            throw new XMLParsingException(fieldSubEl, "Invalid Style in Form:" + formDef.formName + " Field: " + formField.fieldName);
                                        break;
                                    case FormDefFieldType.PickMultiple:
                                        if (formField.Style != FormDefFieldStyle.Checkboxes && formField.Style != FormDefFieldStyle.DropDown && !formField.Static)
                                            throw new XMLParsingException(fieldSubEl, "Invalid Style in Form:" + formDef.formName + " Field: " + formField.fieldName);
                                        break;
                                    case FormDefFieldType.DateTime:
                                    case FormDefFieldType.DateRange:
                                        if (formField.Style != FormDefFieldStyle.Date && formField.Style != FormDefFieldStyle.Time && formField.Style != FormDefFieldStyle.Month)
                                            throw new XMLParsingException(fieldSubEl, "Invalid Style in Form:" + formDef.formName + " Field: " + formField.fieldName);
                                        break;
                                    default:
                                        if (formField.Type != FormDefFieldType.MergedForm && formField.Type != FormDefFieldType.UserControlScalar)
                                            if (formField.Style != FormDefFieldStyle.None && !formField.Static)
                                                throw new XMLParsingException(fieldSubEl, "Invalid Style in Form:" + formDef.formName + " Field: " + formField.fieldName);
                                        break;
                                }
                            }
                            break;
                        case "Label":
                            formField.Label = fieldSubEl.Value;
                            break;
                        case "PopupTitle":
                            formField.PopupTitle = fieldSubEl.Value;
                            break;
                        case "Contiguous":
                            if (formField.Type != FormDefFieldType.DateRange)
                                throw new XMLParsingException(fieldSubEl, "FormDef:" + formDef.formName + " Field: " + fieldSubEl.Name.LocalName + " has Invalid Node:" + fieldSubEl.Name.LocalName + ". This is allowed only for DateTime field type.");
                            formField.Contiguous = true;
                            break;
                        case "NonOverlapping":
                            if (formField.Type != FormDefFieldType.DateRange)
                                throw new XMLParsingException(fieldSubEl, "FormDef:" + formDef.formName + " Field: " + fieldSubEl.Name.LocalName + " has Invalid Node:" + fieldSubEl.Name.LocalName + ". This is allowed only for DateTime field type.");
                            formField.NonOverlapping = true;
                            break;
                        case "DateFromRequired":
                            if (formField.Type != FormDefFieldType.DateRange)
                                throw new XMLParsingException(fieldSubEl, "FormDef:" + formDef.formName + " Field: " + fieldSubEl.Name.LocalName + " has Invalid Node:" + fieldSubEl.Name.LocalName + ". This is allowed only for DateTime field type.");
                            formField.DateFromRequired = true;
                            break;
                        case "DateToRequired":
                            if (formField.Type != FormDefFieldType.DateRange)
                                throw new XMLParsingException(fieldSubEl, "FormDef:" + formDef.formName + " Field: " + fieldSubEl.Name + " has Invalid Node:" + fieldSubEl.Name + ". This is allowed only for DateTime field type.");
                            formField.DateToRequired = true;
                            break;
                        case "MinimumValue":
                            if (formField.Type != FormDefFieldType.Number)
                                throw new XMLParsingException(fieldSubEl, "FormDef:" + formDef.formName + " Field: " + fieldSubEl.Name + " has Invalid Node:" + fieldSubEl.Name + ". This is allowed only for Number field type.");
                            formField.MinimumValue = decimal.Parse(fieldSubEl.Value);
                            break;
                        case "MaximumValue":
                            if (formField.Type != FormDefFieldType.Number)
                                throw new XMLParsingException(fieldSubEl, "FormDef:" + formDef.formName + " Field: " + fieldSubEl.Name + " has Invalid Node:" + fieldSubEl.Name + ". This is allowed only for Number field type.");
                            formField.MaximumValue = decimal.Parse(fieldSubEl.Value);
                            break;
                        case "Domain":
                            formField.FieldFunctions.AddRange(formDef.GetServerFunctionCalls(a, formField, fieldEl, FunctionType.Domain));
                            foreach (XAttribute fieldSubElAttribute in fieldSubEl.Attributes())
                            {
                                switch (fieldSubElAttribute.Name.LocalName)
                                {
                                    default:
                                        throw new XMLParsingException(fieldSubEl, "FormDef:" + formDef.formName + " Field: " + fieldSubEl.Name + " has Invalid Attribute:" + fieldSubElAttribute.Name.LocalName);
                                }
                            }
                            break;
                        case "FileFilters":
                            if (formField.Type != FormDefFieldType.FileUpload)
                                goto default;
                            formField.UploadDetail.FileFilters = fieldSubEl.Value;
                            break;

                        case "MaxFileSizeInMb":
                            if (formField.Type != FormDefFieldType.FileUpload)
                                goto default;
                            formField.UploadDetail.MaxFileSizeInMb = decimal.Parse(fieldSubEl.Value);
                            break;
                        case "ButtonText":
                            if (formField.Type != FormDefFieldType.FileUpload)
                                goto default;
                            formField.UploadDetail.ButtonText = fieldSubEl.Value;
                            break;

                        case "MinChars":
                            if (formField.Type != FormDefFieldType.Text && formField.Type != FormDefFieldType.TextArea && formField.Type != FormDefFieldType.Password)
                                goto default;
                            formField.MinChars = Int32.Parse(fieldSubEl.Value);
                            foreach (XAttribute fieldSubElAttribute in fieldSubEl.Attributes())
                            {
                                switch (fieldSubElAttribute.Name.LocalName)
                                {
                                    default:
                                        throw new XMLParsingException(fieldSubEl, "FormDef:" + formDef.formName + " Field: " + fieldSubEl.Name + " has Invalid Attribute:" + fieldSubElAttribute.Name);
                                }
                            }
                            break;
                        case "MaxChars":
                            if (formField.Type != FormDefFieldType.Text && formField.Type != FormDefFieldType.TextArea && formField.Type != FormDefFieldType.Password)
                                goto default;
                            formField.MaxChars = Int32.Parse(fieldSubEl.Value);
                            foreach (XAttribute fieldSubElAttribute in fieldSubEl.Attributes())
                            {
                                switch (fieldSubElAttribute.Name.LocalName)
                                {
                                    default:
                                        throw new XMLParsingException(fieldSubEl, "FormDef:" + formDef.formName + " Field: " + fieldSubEl.Name + " has Invalid Attribute:" + fieldSubElAttribute.Name);
                                }
                            }
                            break;
                        case "Decimals":
                            if (formField.Type != FormDefFieldType.Number)
                                goto default;
                            formField.decimals = Int32.Parse(fieldSubEl.Value);
                            formField.decimalsAssigned = true;
                            foreach (XAttribute fieldSubElAttribute in fieldSubEl.Attributes())
                            {
                                switch (fieldSubElAttribute.Name.LocalName)
                                {
                                    default:
                                        throw new XMLParsingException(fieldSubEl, "FormDef:" + formDef.formName + " Field: " + fieldSubEl.Name + " has Invalid Attribute:" + fieldSubElAttribute.Name);
                                }
                            }
                            break;
                        case "AutoUpload":
                            formField.AutoUpload = true;
                            if (formField.Type != FormDefFieldType.FileUpload && formField.Type != FormDefFieldType.MultiFileUpload)
                                goto default;
                            foreach (XAttribute fieldSubElAttribute in fieldSubEl.Attributes())
                            {
                                switch (fieldSubElAttribute.Name.LocalName)
                                {
                                    default:
                                        throw new XMLParsingException(fieldSubEl, "FormDef:" + formDef.formName + " Field: " + fieldSubEl.Name + " has Invalid Attribute:" + fieldSubElAttribute.Name.LocalName);
                                }
                            }
                            foreach (var el in fieldSubEl.Elements())
                            {
                                switch (el.Name.LocalName)
                                {
                                    default:
                                        throw new XMLParsingException(fieldSubEl, "FormDef:" + formDef.formName + " Field: " + fieldSubEl.Name + " has Invalid Child node:" + el.Name);
                                }
                            }
                            break;
                        case "SaveOnUpload":
                            formField.SaveOnUpload = true;
                            if (formField.Type != FormDefFieldType.FileUpload)
                                goto default;
                            foreach (XAttribute fieldSubElAttribute in fieldSubEl.Attributes())
                            {
                                switch (fieldSubElAttribute.Name.LocalName)
                                {
                                    default:
                                        throw new XMLParsingException(fieldSubEl, "FormDef:" + formDef.formName + " Field: " + fieldSubEl.Name + " has Invalid Attribute:" + fieldSubElAttribute.Name.LocalName);
                                }
                            }
                            foreach (var el in fieldSubEl.Elements())
                            {
                                switch (el.Name.LocalName)
                                {
                                    default:
                                        throw new XMLParsingException(fieldSubEl, "FormDef:" + formDef.formName + " Field: " + fieldSubEl.Name + " has Invalid Child node:" + el.Name);
                                }
                            }
                            break;
                        case "Accept":
                            if (formField.Type != FormDefFieldType.FileUpload && formField.Type != FormDefFieldType.MultiFileUpload)
                                goto default;
                            formField.Accept = fieldSubEl.Value;
                            foreach (XAttribute fieldSubElAttribute in fieldSubEl.Attributes())
                            {
                                switch (fieldSubElAttribute.Name.LocalName)
                                {
                                    case "ValidateOnServer":
                                        formField.ValidateOnServer = true;
                                        break;
                                    default:
                                        throw new XMLParsingException(fieldSubEl, "FormDef:" + formDef.formName + " Field: " + fieldSubEl.Name + " has Invalid Attribute:" + fieldSubElAttribute.Name.LocalName);
                                }
                            }
                            break;
                        case "Storage":
                            if (formField.Type != FormDefFieldType.FileUpload && formField.Type != FormDefFieldType.MultiFileUpload)
                                goto default;
                            foreach (XAttribute fieldSubElAttribute in fieldSubEl.Attributes())
                            {
                                switch (fieldSubElAttribute.Name.LocalName)
                                {
                                    case "Type":
                                        formField.FileUploadStorage = (FileUploadStorageType)Enum.Parse(typeof(FileUploadStorageType), fieldSubElAttribute.Value);
                                        break;
                                    default:
                                        throw new XMLParsingException(fieldSubEl, "FormDef:" + formDef.formName + " Field: " + fieldSubEl.Name + " has Invalid Attribute:" + fieldSubElAttribute.Name.LocalName);
                                }
                            }
                            foreach (var el in fieldSubEl.Elements())
                            {
                                switch (el.Name.LocalName)
                                {
                                    case "Directory":
                                        formField.FileUploadDirectory = el.Value;
                                        break;
                                    default:
                                        throw new XMLParsingException(fieldSubEl, "FormDef:" + formDef.formName + " Field: " + fieldSubEl.Name + " has Invalid Child node:" + el.Name);
                                }
                            }
                            if (formField.FileUploadStorage == FileUploadStorageType.Directory && formField.FileUploadDirectory == null)
                                throw new XMLParsingException(fieldSubEl, "FormDef:" + formDef.formName + " Field: " + fieldSubEl.Name + "Node: Storage should have a child node Directory having full path of directory where uploaded files will be saved");
                            break;
                        case "ZeroAsBlank":
                            if (formField.Type != FormDefFieldType.Number)
                                goto default;
                            formField.ZeroAsBlank = true;
                            foreach (XAttribute fieldSubElAttribute in fieldSubEl.Attributes())
                            {
                                switch (fieldSubElAttribute.Name.LocalName)
                                {
                                    default:
                                        throw new XMLParsingException(fieldSubEl, "FormDef:" + formDef.formName + " Field: " + fieldSubEl.Name + " has Invalid Attribute:" + fieldSubElAttribute.Name);
                                }
                            }
                            break;
                        case "DoNotSaveInDB":
                            formField.DoNotSaveInDB = true;
                            foreach (XAttribute fieldSubElAttribute in fieldSubEl.Attributes())
                            {
                                switch (fieldSubElAttribute.Name.LocalName)
                                {
                                    default:
                                        throw new XMLParsingException(fieldSubEl, "FormDef:" + formDef.formName + " Field: " + fieldSubEl.Name + " has Invalid Attribute:" + fieldSubElAttribute.Name);
                                }
                            }
                            break;
                        case "AllowMultiSelect":
                            formField.AllowMultiSelect = true;
                            if (formField.fieldName != "SelectRow")
                                throw new XMLParsingException(fieldSubEl, "FormDef:" + formDef.formName + " Field: " + fieldSubEl.Name + " has Invalid Node: AllowMultiSelect  (Allowed only with FieldName SelectRow)");

                            foreach (XAttribute fieldSubElAttribute in fieldSubEl.Attributes())
                            {
                                switch (fieldSubElAttribute.Name.LocalName)
                                {
                                    default:
                                        throw new XMLParsingException(fieldSubEl, "FormDef:" + formDef.formName + " Field: " + fieldSubEl.Name + " has Invalid Attribute:" + fieldSubElAttribute.Name);
                                }
                            }
                            break;
                        case "NonSecure":
                            if (formField.Type != FormDefFieldType.FileUpload && formField.Type != FormDefFieldType.MultiFileUpload)
                                goto default;
                            formField.NonSecure = true;
                            break;
                        case "NoEncode":
                            if (formField.Type != FormDefFieldType.Text && formField.Type != FormDefFieldType.TextArea)
                                goto default;
                            formField.NoEncode = true;
                            break;
                        case "Placeholder":
                            if (formField.Type != FormDefFieldType.Text && formField.Type != FormDefFieldType.Pickone && formField.Type != FormDefFieldType.Number)
                                goto default;
                            formField.Placeholder = fieldSubEl.Value;
                            break;
                        case "Static":
                            formField.Static = true;
                            if (containerNode == "RowFields")
                                throw new XMLParsingException(fieldSubEl, "FormDef:" + formDef.formName + " Field: " + fieldSubEl.Name + " Static is not required in RowFields.");
                            break;
                        case "Parameter":
                            if (formField.Type != FormDefFieldType.UserControlScalar)
                                goto default;
                            foreach (XAttribute fieldSubElAttribute in fieldSubEl.Attributes())
                            {
                                switch (fieldSubElAttribute.Name.LocalName)
                                {
                                    case "Name":
                                        formField.UserControlParameters[fieldSubElAttribute.Value] = fieldSubEl.Value;
                                        break;
                                    default:
                                        throw new XMLParsingException(fieldSubEl, "FormDef:" + formDef.formName + " Field: " + fieldSubEl.Name + " has Invalid Attribute:" + fieldSubElAttribute.Name);
                                }
                            }
                            formField.StaticSubmitValue = true;
                            break;
                        case "SubmitIfStatic":
                            formField.StaticSubmitValue = true;
                            break;
                        case "NonStatic":
                            if (containerNode != "RowFields")
                                throw new XMLParsingException(fieldSubEl, "FormDef:" + formDef.formName + " Field: " + fieldSubEl.Name + " NonStatic is Allowed only in RowFields.");
                            formField.NonStatic = true;
                            foreach (XAttribute fieldSubElAttribute in fieldSubEl.Attributes())
                            {
                                switch (fieldSubElAttribute.Name.LocalName)
                                {
                                    default:
                                        throw new XMLParsingException(fieldSubEl, "FormDef:" + formDef.formName + " Field: " + fieldSubEl.Name + " has Invalid Attribute:" + fieldSubElAttribute.Name);
                                }
                            }
                            break;
                        case "Hidden":
                            if (formField.Type == FormDefFieldType.FormContainerDynamic || formField.Type == FormDefFieldType.FormContainerGrid || formField.Type == FormDefFieldType.MergedForm || formField.Type == FormDefFieldType.MultiFileUpload || formField.Type == FormDefFieldType.Redirect || formField.Type == FormDefFieldType.UserControlScalar)
                                goto default;
                            formField.Hidden = true;
                            foreach (XAttribute fieldSubElAttribute in fieldSubEl.Attributes())
                            {
                                switch (fieldSubElAttribute.Name.LocalName)
                                {
                                    default:
                                        throw new XMLParsingException(fieldSubEl, "FormDef:" + formDef.formName + " Field: " + fieldSubEl.Name + " has Invalid Attribute:" + fieldSubElAttribute.Name);
                                }
                            }
                            break;
                        case "Encryption":
                            foreach (XAttribute fieldSubElAttribute in fieldSubEl.Attributes())
                            {
                                switch (fieldSubElAttribute.Name.LocalName)
                                {
                                    case "Type":
                                        formField.EncryptionType = (EncryptionType)Enum.Parse(typeof(EncryptionType), fieldSubElAttribute.Value);
                                        break;
                                    default:
                                        throw new XMLParsingException(fieldSubEl, "FormDef:" + formDef.formName + " Field: " + fieldSubEl.Name + " has Invalid Attribute:" + fieldSubElAttribute.Name);
                                }
                            }
                            break;
                        case "MaxFileSizeInKB":
                            formField.MaxFileSizeInKB = int.Parse(fieldSubEl.Value);
                            break;
                        case "Shortcut":
                            formField.Shortcut = fieldSubEl.Value;
                            break;
                        case "TableName":
                            if (formField.Type != FormDefFieldType.PickMultiple && formField.Type != FormDefFieldType.Pickone && formField.Type != FormDefFieldType.MultiFileUpload && formField.Type != FormDefFieldType.FormContainerGrid)
                                goto default;
                            formField.TableName = fieldSubEl.Value.Trim();
                            if (formField.TableName.IsNullOrEmpty())
                                throw new XMLParsingException(fieldSubEl, "FormDef:" + formDef.formName + " TableName should not be blank");
                            break;
                        case "SaveTableName":
                            if (formField.Type != FormDefFieldType.PickMultiple && formField.Type != FormDefFieldType.MultiFileUpload && formField.Type != FormDefFieldType.FormContainerGrid)
                                goto default;
                            formField.SaveTableName = fieldSubEl.Value.Trim();
                            if (formField.SaveTableName.IsNullOrEmpty())
                                throw new XMLParsingException(fieldSubEl, "FormDef:" + formDef.formName + " TableName should not be blank");
                            break;
                        case "SaveTableForeignKey":
                            if (formField.Type != FormDefFieldType.PickMultiple && formField.Type != FormDefFieldType.MultiFileUpload)
                                goto default;
                            formField.SaveTableForeignKey = fieldSubEl.Value;
                            if (!System.Text.RegularExpressions.Regex.IsMatch(formField.SaveTableForeignKey, @"^[a-zA-Z0-9_-_]+$"))
                                throw new XMLParsingException(fieldEl, "In FormDef: " + formDef.formName + " Field: " + formField.fieldName + " Property SaveTableForeignKey should be alpha numeric with first letter not as numeric.");
                            break;
                        case "ShowSelectAll":
                            if (formField.Type != FormDefFieldType.PickMultiple)
                                goto default;
                            formField.ShowSelectAll = true;
                            break;
                        case "Required":
                            formField.Required = true;
                            break;
                        case "Help":
                            formField.Help = fieldSubEl.Value;
                            break;
                        case "NoSubmit":
                            if (formField.Type != FormDefFieldType.Button)
                                goto default;
                            formField.NoSubmit = true;
                            break;
                        case "CSSClass":
                            formField.CSSClass = fieldSubEl.Value;
                            break;
                        case "Filter":
                            if (containerNode != "RowFields")
                                throw new XMLParsingException(fieldSubEl, " Filter property allowed only for RowFields");
                            formField.RowFilter = true;
                            break;
                        case "Sortable":
                            if (containerNode != "RowFields")
                                throw new XMLParsingException(fieldSubEl, " Sortable property allowed only for RowFields");
                            formField.Sortable = true;
                            break;
                        case "LabelStyle":
                            formField.LabelStyle = fieldSubEl.Value;
                            break;
                        case "ControlStyle":
                            formField.ControlStyle = fieldSubEl.Value;
                            break;
                        case "PartStyle":
                            formField.PartStyle = fieldSubEl.Value;
                            break;
                        case "ContainerStyle":
                            formField.ContainerStyle = fieldSubEl.Value;
                            break;
                        case "DateFormat":
                            if (formField.Type == FormDefFieldType.DateTime || formField.Type == FormDefFieldType.DateRange)
                                formField.DateFormat = fieldSubEl.Value;
                            break;
                        case "EmailValidation":
                            if (formField.Type != FormDefFieldType.Text)
                                goto default;
                            formField.EmailValidation = true;
                            break;
                        case "RegexValidation":
                            if (formField.Type != FormDefFieldType.Text && formField.Type != FormDefFieldType.Number && formField.Type != FormDefFieldType.Password)
                                goto default;
                            formField.RegexValidation = fieldSubEl.Value;
                            break;
                        case "PopupFields":
                            {
                                if (formField.Type != FormDefFieldType.FormContainerGrid)
                                    throw new XMLParsingException(fieldSubEl, "PopupFields is allowed only for FieldType: FormContainerGrid");
                                var popupFormName = formField.fieldName + "Popup";
                                popupFormDef = new FormDef();
                                popupFormDef.FormType = FormType.ContainerRowFormGenerated;
                                popupFormDef.id = AppPress.GetUniqueId();
                                popupFormDef.formName = popupFormName;
                                popupFormDef.ContainerFormField = formField;
                                popupFormDef.GenerationType = 2;
                                formDefs.Add(popupFormDef);
                                formField.popupFields = LoadFields(a, popupFormDef, fieldSubEl, formDefs, keepOriginal, fieldSubEl.Name.LocalName);
                                string groupName = null;
                                foreach (XAttribute fieldSubElAttribute in fieldSubEl.Attributes())
                                {
                                    switch (fieldSubElAttribute.Name.LocalName)
                                    {
                                        case "PopupWidth":
                                            popupWidth = int.Parse(fieldSubElAttribute.Value);
                                            break;
                                        case "PopupHeight":
                                            popupHeight = int.Parse(fieldSubElAttribute.Value);
                                            break;
                                        case "PopupPosition":
                                            popupPosition = fieldSubElAttribute.Value;
                                            break;
                                        case "Group":
                                            groupName = fieldSubElAttribute.Value;
                                            break;
                                        default:
                                            throw new XMLParsingException(fieldSubEl, "FormDef:" + formDef.formName + " Field: " + fieldSubEl.Name + " has Invalid Attribute:" + fieldSubElAttribute.Name);
                                    }
                                }
                                if (groupName != null)
                                    foreach (var field in formField.popupFields)
                                        field.GroupName = groupName;
                                break;
                            }
                        case "ContainerFields":
                            {
                                if (formField.Type != FormDefFieldType.FormContainerGrid && formField.Type != FormDefFieldType.FormContainerDynamic)
                                    throw new XMLParsingException(fieldSubEl, "ContainerFields is allowed only for FieldTypes: FormContainerGrid, FormContainerDynamic");

                                var fields = LoadFields(a, formDef, fieldSubEl, formDefs, keepOriginal, fieldSubEl.Name.LocalName);
                                foreach (var field in fields)
                                {
                                    // save temporarily the containerFieldName
                                    field.containerFormField = new FormField();
                                    field.containerFormField.fieldName = formField.fieldName;
                                }
                                formFields.AddRange(fields);
                                break;
                            }
                        case "RowFields":
                            {
                                if (formField.Type != FormDefFieldType.FormContainerGrid)
                                    throw new XMLParsingException(fieldSubEl, "RowFields is allowed only for FieldTypes: FormContainerGrid");
                                var rowFormName = formField.fieldName + "Row";
                                rowFormDef = new FormDef();
                                rowFormDef.FormType = FormType.ContainerRowFormGenerated;
                                rowFormDef.ContainerFormField = formField;
                                rowFormDef.id = AppPress.GetUniqueId();
                                rowFormDef.formName = rowFormName;
                                rowFormDef.DoNoSaveInDB = formField.DoNotSaveInDB;
                                rowFormDef.GenerationType = 1;
                                formDefs.Add(rowFormDef);
                                formField.rowFields = LoadFields(a, rowFormDef, fieldSubEl, formDefs, keepOriginal, fieldSubEl.Name.LocalName);
                                foreach (XAttribute fieldSubElAttribute in fieldSubEl.Attributes())
                                {
                                    switch (fieldSubElAttribute.Name.LocalName)
                                    {
                                        case "Width":
                                            formField.width = fieldSubElAttribute.Value;
                                            break;
                                        case "Height":
                                            formField.height = fieldSubElAttribute.Value;
                                            if (!formField.height.EndsWith("px"))
                                                throw new XMLParsingException(fieldSubEl, "Height attribute value should end with px");
                                            break;
                                        case "Sortable":
                                            formField.Sortable = fieldSubElAttribute.Value.Equals("true", StringComparison.OrdinalIgnoreCase);
                                            break;
                                        case "Pivot":
                                            rowFormDef.Pivot = fieldSubElAttribute.Value.Equals("true", StringComparison.OrdinalIgnoreCase);
                                            break;
                                        default:
                                            throw new XMLParsingException(fieldSubEl, "FormDef:" + formDef.formName + " Field: " + fieldSubEl.Name + " has Invalid Attribute:" + fieldSubElAttribute.Name);
                                    }
                                }
                                break;
                            }
                        default:
                            throw new XMLParsingException(fieldSubEl, "In FormDef:" + formDef.formName + " Field: " + formField.fieldName + " has a invalid Element:" + fieldSubEl.Name.LocalName);
                    }
                }
                if (!keepOriginal)
                    switch (formField.Type)
                    {
                        case FormDefFieldType.DateRange:
                            {
                                formField.Type = FormDefFieldType.DateTime;
                                formField.OriginalType = (int)FormDefFieldType.DateRange;
                                formField.Required = formField.DateFromRequired || formField.Contiguous;
                                var dateRangeToFromField = formField.Clone();
                                dateRangeToFromField.Type = FormDefFieldType.DateTime;
                                dateRangeToFromField.fieldName = formField.fieldName + "To";
                                dateRangeToFromField.IsDateRange = 2;
                                dateRangeToFromField.id = AppPress.GetUniqueId();
                                dateRangeToFromField.Required = formField.DateToRequired;
                                formField.IsDateRange = 1;
                                formField.Label = AppPress.InsertSpacesBetweenCaps(formField.fieldName);
                                formField.fieldName += "From";
                                formFields.Add(dateRangeToFromField);
                                break;
                            }
                        case FormDefFieldType.Pickone:
                            {
                                var serverFunction = formField.FieldFunctions.Find(t => t.ServerFunctionType == FunctionType.Options);
                                if (serverFunction != null)
                                {
                                    var parentFieldName = serverFunction.TryGetFunctionParameterValue("ParentFieldName");
                                    //var tableName = serverFunction.TryGetFunctionParameterValue("TableName");
                                    if (parentFieldName != null)
                                    {
                                        var parentFormField = formFields.Find(t => t.fieldName == parentFieldName);
                                        var parentOptionFunction = parentFormField.FieldFunctions.Find(t => t.FunctionName == "GenericGetOptionsFromTable");
                                        if (parentOptionFunction == null)
                                            throw new Exception("Option function for parent field '" + parentFormField.fieldName + "' should be GenericGetOptionsFromTable.");


                                        var refreshFunction = new ServerFunction { FunctionName = "GenericGetOptionsFromTableRefreshField", ServerFunctionType = FunctionType.OnChange };
                                        refreshFunction.Parameters.Add(new ServerFunctionParameter { Name = "FieldName", Value = formField.fieldName });
                                        parentFormField.FieldFunctions.Add(refreshFunction);
                                    }
                                }
                                break;
                            }
                        case FormDefFieldType.FormContainerGrid:
                            {
                                ExpandFormContainerExt(a, formDef, formField, rowFormDef, popupFormDef, popupWidth, popupHeight, popupPosition, formFields, formDefs);
                                break;
                            }
                        case FormDefFieldType.EmbeddedForm:
                            {
                                var formFieldContainer = new FormField(formField.fieldName + "Container", FormDefFieldType.FormContainerDynamic);
                                formFieldContainer.OriginalType = (int)formField.Type;
                                formFieldContainer.Hidden = formField.Hidden;
                                formField.OriginalType = (int)formField.Type;
                                formField.Type = FormDefFieldType.Text;
                                formField.Hidden = true;
                                formFieldContainer.FormNameProperty = formField.FormNameProperty ?? formField.fieldName;
                                formField.FormNameProperty = formFieldContainer.FormNameProperty;
                                formFields.Add(formFieldContainer);
                                break;
                            }
                        case FormDefFieldType.MultiFileUpload:
                            {
                                // Create a Grid

                                var formDef1 = new FormDef();
                                formDef1.id = AppPress.GetUniqueId();
                                formDef1.formName = "MultiFileUploadRow" + formField.fieldName;
                                formDef1.FormType = FormType.ContainerRowForm;
                                string fieldName = "File";
                                /* TBD While building TT should have site
                                var query = a.site.GetForeignKeysQuery("Application_Files", "Id");
                                var dr = a.site.ExecuteQuery(query);
                                try
                                {
                                    while (dr.Read())
                                        if (dr.GetString(0).Equals(formField.SaveTableName, StringComparison.OrdinalIgnoreCase))
                                            fieldName = dr.GetString(1);
                                }
                                finally
                                {
                                    dr.Close();
                                }
                                if (fieldName == null)
                                    throw new Exception("Could not find Foreign Key from Table: "+ formField.SaveTableName+ " to Files Table: Application_Files");
                                    */
                                var formField1 = new FormField(fieldName, FormDefFieldType.Text, formDef1);
                                formField1.Label = "";
                                formField1.OriginalType = (int)FormDefFieldType.MultiFileUpload;
                                formField1.Static = formField1.StaticSubmitValue = true;
                                formField1.Hidden = true;
                                formField1.AutoUpload = formField.AutoUpload;
                                formDef1.formFields.Add(formField1);

                                formField1 = new FormField("Download", FormDefFieldType.Button, formDef1);
                                formField1.Style = FormDefFieldStyle.Link;
                                formField1.OriginalType = (int)FormDefFieldType.MultiFileUpload;
                                var onClickFunction = new ServerFunction(a, FunctionType.OnClick, "MultiFileUploadDownloadFile");
                                formField1.FieldFunctions.Add(onClickFunction);
                                var calcFunction = new ServerFunction(a, FunctionType.Calc, "MultiFileUploadPreview");
                                formField1.FieldFunctions.Add(calcFunction);
                                formDef1.formFields.Add(formField1);

                                formField1 = new FormField("Delete", FormDefFieldType.Button, formDef1);
                                formField1.Style = FormDefFieldStyle.Link;
                                formField1.OriginalType = (int)FormDefFieldType.MultiFileUpload;
                                onClickFunction = new ServerFunction(a, FunctionType.OnClick, "DeleteThisForm");
                                formField1.FieldFunctions.Add(onClickFunction);
                                formDef1.formFields.Add(formField1);
                                if (formField.SaveTableForeignKey != null)
                                {
                                    formField1 = new FormField(formField.SaveTableForeignKey, FormDefFieldType.ForeignKey, formDef1);
                                    formDef1.formFields.Add(formField1);
                                }
                                formDef1.TableName = formField.SaveTableName;
                                formDef1.PrimaryKey = formField.PrimaryKey;
                                formField.SaveTableName = formField.SaveTableForeignKey = formField.PrimaryKey = null;
                                formDefs.Add(formDef1);

                                formField.OriginalType = (int)formField.Type;
                                formField.Type = FormDefFieldType.FormContainerDynamic;
                                formField.width = "350";
                                var domainFunction = new ServerFunction(a, FunctionType.Domain, "GetContainerRowForms");
                                domainFunction.Parameters.Add(new ServerFunctionParameter { Name = "ContainerRowForm", Value = formDef1.formName });
                                formField.FieldFunctions.Add(domainFunction);

                                var fileUpload = new FormField("Add" + formField.fieldName, FormDefFieldType.FileUpload, formDef);
                                fileUpload.OriginalType = (int)FormDefFieldType.MultiFileUpload;
                                fileUpload.DoNotSaveInDB = true;
                                fileUpload.AutoUpload = true;
                                fileUpload.NonSecure = formField.NonSecure;
                                fileUpload.containerFormField = formField;
                                fileUpload.FileUploadStorage = formField.FileUploadStorage;
                                fileUpload.FileUploadDirectory = formField.FileUploadDirectory;
                                fileUpload.containerColumnName = formField.containerColumnName;
                                fileUpload.Label = "";
                                fileUpload.MaxFileSizeInKB = formField.MaxFileSizeInKB;
                                var onChangeFunction = new ServerFunction(a, FunctionType.OnChange, "AddUploadRow");
                                fileUpload.FieldFunctions.Add(onChangeFunction);
                                formFields.Add(fileUpload);
                            }
                            break;
                    }
                if (formField.Static && formField.MaxChars != null)
                    throw new XMLParsingException(fieldEl, "MaxChars not allowed for Static Fields");
            }
            return formFields;
        }

        internal static void ExpandFormContainerExt(AppPress a, FormDef formDef, FormField formField, FormDef rowFormDef, FormDef popupFormDef, int popupWidth, int popupHeight, string popupPosition, List<FormField> formFields, List<FormDef> formDefs)
        {
            ServerFunction modifyClickFunction = null;
            ServerFunction addClickFunction = null;
            if (formField.rowFields != null)
            {
                rowFormDef.TableName = formField.TableName;// == null ? formField.fieldName : formField.TableName;
                rowFormDef.PrimaryKey = formField.PrimaryKey;
                if (formField.Type == FormDefFieldType.FormContainerGrid)
                {
                    rowFormDef.DoNoSaveInDB = true;
                    foreach (var f in formField.rowFields)
                    {
                        if (f.fieldName != "SelectRow" && f.Type != FormDefFieldType.ForeignKey && /*f.Type != FormDefFieldType.FileUpload && */f.containerFormField == null)
                        {
                            if (!f.NonStatic)
                                f.Static = true;
                            if (!f.Hidden && f.DoNotSaveInDB == false && !f.StaticSubmitValue)
                                f.DoNotSaveInDB = !f.NonStatic;
                        }
                        if (!f.DoNotSaveInDB)
                            rowFormDef.DoNoSaveInDB = false;
                        var e = f.CheckSortable();
                        if (f.OriginalType == (int)FormDefFieldType.None && f.Sortable)
                        {
                            if (e != null)
                                throw new Exception(f.GetDescription() + " " + e);
                        }
                        if (formField.Sortable && e == null)
                            f.Sortable = true;

                        if (f.Sortable)
                        {
                            var sortableFormField = new FormField("Sortable" + f.fieldName, FormDefFieldType.Pickone, formDef);
                            sortableFormField.OriginalType = (int)f.Type;
                            sortableFormField.containerFormField = formField;
                            sortableFormField.containerColumnName = formField.containerColumnName;
                            sortableFormField.Label = "";
                            sortableFormField.Style = FormDefFieldStyle.ImageRotation;
                            sortableFormField.FieldFunctions.Add(new ServerFunction(a, FunctionType.OnChange, "SortContainer"));
                            sortableFormField.Sortable = true;
                            sortableFormField.Required = true;// to allow null only first time
                            formFields.Add(sortableFormField);
                        }
                        if (f.RowFilter)
                        {
                            var rowFilter = new FormField("RowFilter" + f.fieldName, FormDefFieldType.PickMultiple, formDef);
                            rowFilter.OriginalType = (int)f.Type;
                            rowFilter.containerFormField = formField;
                            rowFilter.containerColumnName = formField.containerColumnName;
                            rowFilter.Label = "";
                            rowFilter.ShowSelectAll = true;
                            rowFilter.Style = FormDefFieldStyle.Checkboxes;
                            rowFilter.RowFilter = true;
                            formFields.Add(rowFilter);
                        }

                    }
                }

                rowFormDef.formFields.AddRange(formField.rowFields);

                var domainFunction = new ServerFunction(a, FunctionType.Domain, "GetContainerRowForms");
                domainFunction.Parameters.Add(new ServerFunctionParameter { Name = "ContainerRowForm", Value = rowFormDef.id.ToString() });
                formField.FieldFunctions.Add(domainFunction);

                if (formField.popupFields != null || (formField.rowFields.Find(t => t.fieldName == "SelectRow") != null && formField.rowFields.Find(t => t.fieldName != "SelectRow" && t.NonStatic) != null))
                {
                    var button = formFields.Find(t => t.fieldName == "Add" + formField.fieldName);
                    if (button == null)
                    {
                        button = new FormField("Add" + formField.fieldName, FormDefFieldType.Button, formDef);
                        button.Role = "Add";
                        button.OriginalType = (int)formField.Type;
                        button.containerFormField = formField;
                        button.containerColumnName = formField.containerColumnName;
                        button.Label = "Add";
                        button.Style = FormDefFieldStyle.Button;
                        formFields.Add(button);
                    }
                    if (formField.Type == FormDefFieldType.FormContainerGrid && formField.popupFields != null)
                    {
                        addClickFunction = new ServerFunction(a, FunctionType.OnClick, "OpenForm");
                        addClickFunction.Parameters.Add(new ServerFunctionParameter { Name = "FormDefId", Value = popupFormDef.id.ToString() });
                        addClickFunction.Parameters.Add(new ServerFunctionParameter { Name = "Popup", Value = "" });
                        if (formField.PopupTitle != null)
                            addClickFunction.Parameters.Add(new ServerFunctionParameter { Name = "PopupTitle", Value = formField.PopupTitle });
                        if (popupWidth != 0)
                            addClickFunction.Parameters.Add(new ServerFunctionParameter { Name = "PopupWidth", Value = popupWidth.ToString() });
                        if (popupHeight != 0)
                            addClickFunction.Parameters.Add(new ServerFunctionParameter { Name = "PopupHeight", Value = popupHeight.ToString() });
                        if (popupPosition != null)
                            addClickFunction.Parameters.Add(new ServerFunctionParameter { Name = "PopupPosition", Value = popupPosition });
                        button.FieldFunctions.Add(addClickFunction);
                    }
                    else
                    {
                        addClickFunction = new ServerFunction(a, FunctionType.OnClick, "AddNewForm");
                        button.FieldFunctions.Add(addClickFunction);
                    }
                    if (rowFormDef.formFields.Find(t => t.fieldName == "SelectRow") != null)
                    {
                        if (formField.Type == FormDefFieldType.FormContainerGrid && formField.popupFields != null)
                        {
                            button = formFields.Find(t => t.fieldName == "Modify" + formField.fieldName);
                            if (button == null)
                            {
                                button = new FormField("Modify" + formField.fieldName, FormDefFieldType.Button, formDef);
                                button.OriginalType = (int)formField.Type;
                                button.containerFormField = formField;
                                button.containerColumnName = formField.containerColumnName;
                                button.Label = "Modify";
                                button.Role = "Modify";
                                button.Style = FormDefFieldStyle.Button;
                                formFields.Add(button);
                            }
                            modifyClickFunction = new ServerFunction(a, FunctionType.OnClick, "ModifySelectedSubForm");
                            modifyClickFunction.Parameters.Add(new ServerFunctionParameter { Name = "Popup", Value = "" });
                            modifyClickFunction.Parameters.Add(new ServerFunctionParameter { Name = "FormDefId", Value = popupFormDef.id.ToString() });
                            if (formField.PopupTitle != null)
                                modifyClickFunction.Parameters.Add(new ServerFunctionParameter { Name = "PopupTitle", Value = formField.PopupTitle });
                            if (popupWidth != 0)
                                modifyClickFunction.Parameters.Add(new ServerFunctionParameter { Name = "PopupWidth", Value = popupWidth.ToString() });
                            if (popupHeight != 0)
                                modifyClickFunction.Parameters.Add(new ServerFunctionParameter { Name = "PopupHeight", Value = popupHeight.ToString() });
                            if (popupPosition != null)
                                modifyClickFunction.Parameters.Add(new ServerFunctionParameter { Name = "PopupPosition", Value = popupPosition });
                            button.FieldFunctions.Add(modifyClickFunction);
                        }
                        button = formFields.Find(t => t.fieldName == "Delete" + formField.fieldName);
                        if (button == null)
                        {
                            button = new FormField("Delete" + formField.fieldName, FormDefFieldType.Button, formDef);
                            button.OriginalType = (int)formField.Type;
                            button.containerFormField = formField;
                            button.containerColumnName = formField.containerColumnName;
                            button.Label = "Delete";
                            button.Role = "Delete";
                            button.Style = FormDefFieldStyle.Button;
                            formFields.Add(button);
                        }
                        {
                            var onClickFunction = new ServerFunction(a, FunctionType.OnClick, "DeleteSelectedSubForms");
                            onClickFunction.Parameters.Add(new ServerFunctionParameter { Name = "DeleteFromDB", Value = "" });
                            button.FieldFunctions.Add(onClickFunction);
                        }
                    }
                }
                formField.rowFields = null;
            }
            if (formField.popupFields != null)
            {
                popupFormDef.TableName = formField.TableName;// == null ? formField.fieldName : formField.TableName;
                popupFormDef.PrimaryKey = formField.PrimaryKey;

                if (formField.popupFields.Find(t => t.fieldName == "Save" + formField.fieldName) == null)
                {
                    var button = new FormField("Save" + formField.fieldName, FormDefFieldType.Button, popupFormDef);
                    button.OriginalType = (int)formField.Type;
                    //button.Shortcut = "Enter";
                    button.Label = "Save";
                    button.Style = FormDefFieldStyle.Button;
                    var onClickFunction = new ServerFunction(a, FunctionType.OnClick, "SaveForm");
                    button.FieldFunctions.Add(onClickFunction);
                    formField.popupFields.Add(button);
                }
                popupFormDef.formFields.AddRange(formField.popupFields);
                formField.popupFields = null;

            }
            formField.OriginalType = (int)formField.Type;
            formField.Type = FormDefFieldType.FormContainerDynamic;
        }

        public static FormDef FindFormDef(List<FormDef> formDefs, string formName)
        {
            var formDef = formDefs.Find(t => t.formName.Equals(formName, StringComparison.OrdinalIgnoreCase));
            return formDef;
        }
        public static FormDef FindFormDef(List<FormDef> formDefs, long formId)
        {
            var formDef = formDefs.Find(t => t.id == formId);
            return formDef;
        }


        internal List<ServerFunction> GetServerFunctionCalls(AppPress a, FormField formField, XElement formEl, FunctionType functionType)
        {
            var ServerFunctions = new List<ServerFunction>();
            foreach (var formElem in formEl.Elements())
            {
                if (formElem.Name.LocalName != functionType.ToString())
                    continue;
                foreach (var function in formElem.Elements())
                {
                    if (function.Name.LocalName != "Function")
                        throw new AppPressException(functionType.ToString() + " Can have Child nodes of type only Function");
                    var serverFunctionCall = new ServerFunction(a, functionType, function.Attribute("Name").Value);
                    foreach (var node in function.Elements())
                        switch (node.Name.LocalName)
                        {
                            case "Parameters":
                                serverFunctionCall.Parameters = new List<ServerFunctionParameter>();
                                foreach (var parameter in node.Elements())
                                {
                                    var serverFunctionParameter = new ServerFunctionParameter(parameter.Name.LocalName, parameter.Value);
                                    serverFunctionCall.Parameters.Add(serverFunctionParameter);
                                }
                                break;
                            default:
                                string message = "Invalid child Node:" + node.Name + " in formDef:" + formName;
                                if (formField != null)
                                    message += " in Field: " + formField.fieldName;
                                message += " in Function:" + serverFunctionCall.FunctionName;
                                throw new Exception(message);
                        }
                    ServerFunctions.Add(serverFunctionCall);
                }
            }
            return ServerFunctions;
        }
        class XMLParsingException : Exception
        {
            public XMLParsingException(XElement elem, string m)
                : base("File: " + elem.Document.BaseUri + " Line: " + ((IXmlLineInfo)elem).LineNumber + " Error: " + m)
            {
            }

        }
        internal static FormDef LoadFormDef(AppPress a, XElement formEl, List<FormDef> formDefs, bool keepOriginal)
        {
            FormDef formDef = new FormDef();
            switch (formEl.Name.LocalName)
            {
                case "Form":
                    formDef.FormType = FormType.Application;
                    break;
                case "MasterForm":
                    formDef.FormType = FormType.MasterForm;
                    break;
                case "MergedForm":
                    formDef.FormType = FormType.MergedForm;
                    break;
                case "PluginForm":
                    formDef.FormType = FormType.PluginForm;
                    break;
                case "UserControlScalarForm":
                    formDef.FormType = FormType.UserControlScalarForm;
                    break;
                default:
                    throw new XMLParsingException(formEl, "Invalid Node:" + formEl.Name + " in FormDefs");
            }
            formDef.id = AppPress.GetUniqueId(); // need a unique if for forms loaded from xml
            foreach (XAttribute formDefAttribute in formEl.Attributes())
            {
                switch (formDefAttribute.Name.LocalName)
                {
                    case "Name":
                        formDef.formName = formDefAttribute.Value;
                        if (formDef.formName.Contains(" "))
                            throw new XMLParsingException(formEl, "Invalid character in FormDef with Name:" + formDef.formName);
                        break;
                    default:
                        throw new XMLParsingException(formEl, "Invalid Attribute:" + formDefAttribute.Name + " in FormDef with Name:" + formDef.formName);
                }
                if (formDef.formName == null)
                    throw new Exception("Could not find Name Attribute in FormDef");
            }
            if (formDef.formName == null)
                throw new Exception("FormDef found without Name attribute.");
            foreach (XElement formDefElement in formEl.Elements())
            {
                switch (formDefElement.Name.LocalName)
                {
                    case "TableName":
                        if (formDef.FormType != FormType.Application)
                            throw new XMLParsingException(formDefElement, "TableName is allowed only for Form");
                        formDef.TableName = formDefElement.Value.Trim();
                        break;
                    case "MasterFormName":
                        if (formDef.FormType != FormType.Application)
                            throw new XMLParsingException(formDefElement, "MasterFormName is allowed only for Form");
                        formDef.MasterFormName = formDefElement.Value.Trim();
                        break;
                    case "MergedIntoForm":
                        if (formDef.FormType != FormType.PluginForm)
                            throw new XMLParsingException(formDefElement, "MergeIntoForm is allowed only for PluginForm");
                        var mergeIntoForm = new MergeIntoForm();
                        foreach (XElement el in formDefElement.Elements())
                        {
                            switch (el.Name.LocalName)
                            {
                                case "FormName":
                                    mergeIntoForm.FormName = el.Value.Trim();
                                    break;
                                case "BeforeFieldName":
                                    mergeIntoForm.BeforeFieldName = el.Value.Trim();
                                    break;
                                default:
                                    throw new XMLParsingException(el, "Invalid node: " + el.Name.LocalName);
                            }
                        }
                        if (mergeIntoForm.FormName == null)
                            throw new XMLParsingException(formDefElement, "FormName is required in MergeIntoForm Node");
                        formDef.MergeIntoForms.Add(mergeIntoForm);
                        break;
                    case "CSSClass":
                        if (formDef.FormType != FormType.Application)
                            throw new XMLParsingException(formDefElement, "CSSClass is allowed only for Form");
                        formDef.CSSClass = formDefElement.Value.Trim();
                        break;
                    case "NonSecure":
                        if (formDef.FormType != FormType.Application)
                            throw new XMLParsingException(formDefElement, "NonSecure is allowed only for Form");
                        formDef.NonSecure = true;
                        break;
                    case "Fields":
                        string groupName = null;
                        foreach (XAttribute formDefAttribute in formDefElement.Attributes())
                        {
                            switch (formDefAttribute.Name.LocalName)
                            {
                                case "Group":
                                    groupName = formDefAttribute.Value;
                                    break;
                                default:
                                    throw new XMLParsingException(formEl, "Invalid Attribute:" + formDefAttribute.Name + " in FormDef with Name:" + formDef.formName);
                            }
                        }
                        var fields = LoadFields(a, formDef, formDefElement, formDefs, keepOriginal, formDefElement.Name.LocalName);
                        foreach (var field in fields)
                            field.GroupName = groupName;
                        formDef.formFields.AddRange(fields);
                        break;
                    default:
                        throw new XMLParsingException(formDefElement, "Invalid Element:" + formDefElement.Name + " in FormDef:" + formDef.formName);
                }
            }
            if (formDef.FormType == FormType.PluginForm && formDef.MergeIntoForms.Count() == 0)
                throw new XMLParsingException(formEl, "For FormType PluginForm there should be at least one child node of type MergeIntoForm");
            return formDef;
        }

        public static List<FormDef> LoadFormDefs(AppPress a, XDocument _xDocument, bool keepOriginal)
        {
            var formDefs = new List<FormDef>();
            foreach (XElement formEl in _xDocument.Elements().Elements())
            {
                var formDef = LoadFormDef(a, formEl, formDefs, keepOriginal);
                formDefs.Add(formDef);
            }
            return formDefs;
        }

        static long CountLinesInString(string s, int upto)
        {
            long count = 1;
            int start = 0;
            while ((start = s.IndexOf('\n', start, upto - start)) != -1)
            {
                count++;
                start++;
            }
            return count;
        }

        internal string GetSkin(AppPress a, bool forceStatic, bool popup, string skinError, SkinType skinType, int skinIndex)
        {
            var formSkin = Skins.Find(t => t.skinType == skinType && t.index == skinIndex);
            if (formSkin == null)
                throw new AppPressException(GetDescription() + " Could not find Skin for index: " + skinIndex + " Skin Type: " + skinType);
#if DEBUG
            if (formSkin.skinFileTime != null)
            {
                if (File.GetLastWriteTime(formSkin.skinFileName) != formSkin.skinFileTime)
                {
                    string code = "";
                    formSkin.skin = File.ReadAllText(formSkin.skinFileName);
                    Util.SplitCode(ref formSkin.skin, ref code, ref functionId, ref AppPress.codeFragments);
                    formSkin.skinFileTime = File.GetLastWriteTime(formSkin.skinFileName);
                }
            }
#endif
            //if (a.URLFormName != null && a.URLFormName == formName)
            //{
            //    SkinFileData skinFileData;
            //    if (!AppPress.skins.TryGetValue(a.SkinFileName, out skinFileData))
            //        throw new AppPressException("Could not find Skin File:" + a.SkinFileName + " in Skins Folder");
            //    skin = skinFileData.skin;
            //}
            return formSkin.skin.Clone().ToString();

        }

        internal string GenerateSkinTop(AppPress a, bool executed, bool popup)
        {
            if (popup)
                return "";// "<tr><td>";
            string displayName = AppPress.InsertSpacesBetweenCaps(formName);
            string templateStr = "<!doctype html><html><head>";
            templateStr += executed ? a.GetHtmlHeader() : "AppPressHTMLHeader";
            templateStr += "<title>" + displayName + "</title>";
            templateStr += "</head><body>";
            templateStr += "<span class='FormTitle'>" + displayName + "</span>";
            return templateStr;
        }
        internal string GenerateSkinBottom(bool popup)
        {
            if (popup)
                return "";// "</td></tr>";
            return "</body></html>";
        }

        internal static void CreateKnownTypes()
        {
            if (knownTypes == null)
            {
                knownTypes = new List<Type>();
                knownTypes.Add(typeof(PickFieldValue));
                knownTypes.Add(typeof(ButtonFieldValue));
                knownTypes.Add(typeof(DateTimeFieldValue));
                knownTypes.Add(typeof(FileUploadFieldValue));
                knownTypes.Add(typeof(NumberFieldValue));
                knownTypes.Add(typeof(AppPressResponse));
                if (AppPress.Settings != null && AppPress.Settings.ApplicationAppPress != null)
                    knownTypes.Add(AppPress.Settings.ApplicationAppPress);
                foreach (var assembly in AppPress.Assemblies)
                {
                    var types = assembly.assembly.GetTypes().ToList();
                    foreach (var type in types)
                        if (type.Namespace == "ApplicationClasses" && type.Name != "FormDefs")
                            if (knownTypes.Find(t1 => t1.FullName == type.FullName) == null)
                                knownTypes.Add(type);
                }
                //if (AppPress.formDefs != null)
                //    foreach (var formDef in AppPress.formDefs)
                //    {
                //        var t = formDef.GetTopClassType();
                //        if (t != null)
                //            if (knownTypes.Find(t1 => t1 == t) == null)
                //                knownTypes.Add(t);
                //        t = Util.GetType(formDef.GetClassName());
                //        if (t != null)
                //            if (knownTypes.Find(t1 => t1 == t) == null)
                //                knownTypes.Add(t);
                //        foreach (var formField in formDef.formFields)
                //        {
                //            t = Util.GetType(formField.GetClassName());
                //            if (t != null)
                //                if (knownTypes.Find(t1 => t1 == t) == null)
                //                    knownTypes.Add(t);
                //            t = Util.GetType(formField.GetClassName(formDef));
                //            if (t != null)
                //                if (knownTypes.Find(t1 => t1 == t) == null)
                //                    knownTypes.Add(t);
                //        }
                //    }
            }
        }
        internal static string Serialize(Object o, Type targetType)
        {
            MemoryStream stream1 = new MemoryStream();
            CreateKnownTypes();
            DataContractJsonSerializer ser = new DataContractJsonSerializer(targetType, new DataContractJsonSerializerSettings
            {
                UseSimpleDictionaryFormat = true,
                KnownTypes = knownTypes
            });
            ser.WriteObject(stream1, o);
            var s1 = Encoding.UTF8.GetString(stream1.ToArray());
            return s1;
        }
        internal static Object Deserialize(string o, Type targetType)
        {
            CreateKnownTypes();
            DataContractJsonSerializer ser = new DataContractJsonSerializer(targetType, new DataContractJsonSerializerSettings
            {
                UseSimpleDictionaryFormat = true,
                KnownTypes = knownTypes
            });
            // Deserialize the data and read it from the instance.
            var o1 = ser.ReadObject(new MemoryStream(Encoding.UTF8.GetBytes(o)));
            return o1;
        }
        //internal class AppPressConvertor : JavaScriptConverter
        //{

        //    public override IEnumerable<Type> SupportedTypes
        //    {
        //        get { return new ReadOnlyCollection<Type>(new List<Type>(new Type[] { typeof(List<FormData>) })); }
        //    }

        //    public override object Deserialize(IDictionary<string, object> dictionary, Type type, JavaScriptSerializer serializer)
        //    {
        //        var f = serializer.ConvertToType<FormData>(dictionary);
        //        Object o = new Object();
        //        foreach (string key in dictionary.Keys)
        //        {
        //            switch (key)
        //            {
        //                case "Name":
        //                    //a.Name = (string)dictionary[key];
        //                    break;

        //            }
        //        }
        //        return o;
        //    }
        //    public override IDictionary<string, object> Serialize(object obj, JavaScriptSerializer serializer)
        //    {

        //        return new Dictionary<string, object>();
        //    }
        //}

        internal string GenerateSkin(AppPress a, bool forceStatic, string fieldName)
        {
            var skin = string.Empty;
            for (int i = 0; i < formFields.Count(); ++i)
            {
                var formField = formFields[i];
                if (formField.Hidden)
                    continue;
                if (formField.containerFormField != null || formField.Type == FormDefFieldType.ForeignKey/* || formField.Extension*/)
                    continue;
                if (a.skinType == SkinType.FO && formField.Type == FormDefFieldType.Button)
                    continue;
                var displayName = "AppPressDisplayName";
                if (formField.Type == FormDefFieldType.HTML)
                    displayName = "";
                if (formField.IsDateRange == 0)
                {
                    if (a.skinType == SkinType.FO)
                        skin += @"<!--|AppPress." + formField.fieldName + @".Begin|--><fo:table-row>";
                    else
                    {
                        var fieldClass = "form-group";
                        if (formField.Type == FormDefFieldType.Checkbox)
                            fieldClass = "checkbox";
                        else if (formField.Type == FormDefFieldType.FormContainerGrid)
                            fieldClass = "FormContainerGrid";
                        else if (formField.Type == FormDefFieldType.FormContainerDynamic)
                            fieldClass = "FormContainerDynamic";
                        if (formField.CSSClass != null)
                            fieldClass += " " + formField.CSSClass;
                        if (formField.Type == FormDefFieldType.FormContainerDynamic && formField.OriginalType != (int)FormDefFieldType.MultiFileUpload)
                        {
                            if (formField.GetFormContainerStyle() == FormContainerStyle.InLine)
                                fieldClass = ("AppPressFormContainerDynamic " + fieldClass.Replace("AppPressField", "")).Trim();
                            else
                                fieldClass = ("AppPressFormContainerGrid " + fieldClass.Replace("AppPressField", "")).Trim();
                        }
                        skin += "\n<!--|AppPress." + formField.fieldName + ".Begin|-->\n<div id='AppPressContainerId' style=\"AppPressControlStyle\" class=\"" + fieldClass + "\">";
                    }
                }
                // else
                //   if (isfFO)
                //     skin += "<fo:table-row>";
                var lableStyle = "style=\"AppPressLabelStyle\"";
                switch (formField.Type)
                {
                    case FormDefFieldType.UserControlScalar:
                        var extensionFormDef = AppPress.FindFormDef(formField.ExtensionFormName);
                        // do not generate Label if Form Contains Button
                        if (extensionFormDef == null)
                            throw new AppPressException(formField.GetDescription() + " could not find Form: " + formField.ExtensionFormName);
                        skin += @"<label for='AppPressId' class='LabelWidth control-label'><span class='FieldTitle'>" + (extensionFormDef.formFields[0].Type == FormDefFieldType.Button ? "" : "AppPressDisplayName") + "</span></label>";
                        skin += formField.GenerateSkin(a, forceStatic, false, false, displayName);
                        break;
                    case FormDefFieldType.MergedForm:
                        skin += @"<label for='AppPressId' class='LabelWidth control-label'><span class='FieldTitle'>AppPressDisplayName</span></label>";
                        skin += formField.GenerateSkin(a, forceStatic, false, false, displayName);
                        break;
                    case FormDefFieldType.HTML:
                    case FormDefFieldType.Pickone:
                    case FormDefFieldType.PickMultiple:
                    case FormDefFieldType.Text:
                    case FormDefFieldType.Number:
                    case FormDefFieldType.FileUpload:
                    case FormDefFieldType.Password:
                    case FormDefFieldType.DateTime:
                    case FormDefFieldType.TextArea:
                        if (formField.IsDateRange == 0)
                        {
                            if (a.skinType == SkinType.FO)
                            {
                                skin += "<fo:table-cell><fo:block font-size='8pt' font-family='sans-serif' font-weight='bold' space-after.optimum='20pt' text-align='start' padding =\"4pt\">";
                                if (displayName.Length > 0)
                                    skin += displayName;
                                skin += "</fo:block></fo:table-cell>";
                                if (formField.Type == FormDefFieldType.TextArea)
                                    // start a new Row
                                    skin += "<fo:table-cell><fo:block/></fo:table-cell></fo:table-row><fo:table-row><fo:table-cell><fo:block font-size='8pt' font-family='sans-serif' space-after.optimum='20pt' text-align='start' padding =\"4pt\">";
                                else
                                    skin += "<fo:table-cell><fo:block font-size='8pt' font-family='sans-serif' space-after.optimum='20pt' text-align='start' padding =\"4pt\">";
                            }
                            else if (formField.Type == FormDefFieldType.HTML)
                            {
                                skin += "<span class='FieldTitle'>" + displayName + "</span>";
                            }
                            else
                            {
                                skin += @"<label for='AppPressId' class='LabelWidth control-label'>";
                                if (displayName.Length > 0)
                                    skin += "<span class='FieldTitle'>" + displayName + "</span>";
                                skin += "</label>";
                            }
                        }
                        //if (a.skinType != SkinType.FO)
                        //    skin += "<div >";
                        if (formField.IsDateRange != 0)
                        {
                            skin += @"<div class='form-group'><!--|AppPress." + formField.fieldName + @".Begin|--><span id='AppPressContainerId'><label for='AppPressId' class='LabelWidth control-label'><span class='FieldTitle'>AppPressDisplayName</span></label>";
                            skin += formField.GenerateSkin(a, forceStatic, false, false, null);
                            skin += @"</span><!--|AppPress." + formField.fieldName + @".End|-->";
                            var helpSpans = "<!--|AppPress." + formField.fieldName + @".Begin|--><span id='AppPressHelpId' class='help-block has-success'>AppPressHelpText</span><span id='AppPressErrorId' class='help-block has-error'></span><!--|AppPress." + formField.fieldName + @".End|-->";
                            formField = formFields[i + 1];
                            skin += @"<!--|AppPress." + formField.fieldName + @".Begin|--><span id='AppPressContainerId'>";
                            if (formField.Required)
                                skin += "<span style='color:red;width:20px'>*</span>";
                            else
                                skin += "<span style='color:red;width:20px'>&nbsp;&nbsp;</span>";
                            skin += formField.GenerateSkin(a, forceStatic, false, false, null);

                            skin += @"</span><!--|AppPress." + formField.fieldName + @".End|-->";
                            helpSpans += "<!--|AppPress." + formField.fieldName + @".Begin|--><span id='AppPressHelpId' class='help-block has-success'>AppPressHelpText</span><span id='AppPressErrorId' class='help-block has-error'></span><!--|AppPress." + formField.fieldName + @".End|-->";
                            skin += helpSpans;
                            skin += "</div>";
                            i++;
                        }
                        else
                            skin += formField.GenerateSkin(a, forceStatic, false, false, null);
                        if (a.skinType == SkinType.FO)
                            skin += "</fo:block></fo:table-cell>";
                        //else
                        //    skin += "</div>\n";
                        break;
                    case FormDefFieldType.Checkbox:
                        if (a.skinType == SkinType.FO)
                        {
                            skin += "<fo:table-cell number-columns-spanned='2'><fo:block font-size='8pt' font-family='sans-serif' space-after.optimum='20pt' text-align='start' padding =\"4pt\"><fo:table><fo:table-body><fo:table-row><fo:table-cell><fo:block font-size='8pt' font-family='sans-serif' space-after.optimum='20pt' text-align='start' padding =\"4pt\">";
                            skin += formField.GenerateSkin(a, forceStatic, false, false, null);
                            skin += "</fo:block></fo:table-cell><fo:table-cell><fo:block font-size='8pt' font-family='sans-serif' space-after.optimum='20pt' text-align='start' padding =\"4pt\">";
                            if (displayName.Length > 0)
                                skin += displayName;
                            skin += "</fo:block></fo:table-cell></fo:table-row></fo:table-body></fo:table>";
                            skin += "</fo:block></fo:table-cell>";
                        }
                        else
                        {
                            skin += @"<label class='LabelWidth'></label><label for='AppPressId'>";
                            skin += formField.GenerateSkin(a, forceStatic, false, false, null);
                            skin += displayName + @"<span id='AppPressHelpId' class='help-block has-success no-margin'>AppPressHelpText</span><span id='AppPressErrorId' class='help-block has-error no-margin'></span></label>";
                        }
                        break;
                    case FormDefFieldType.Button:
                        if (a.skinType == SkinType.FO)
                            skin += "<fo:table-cell number-columns-spanned='2'><fo:block font-size='8pt' font-family='sans-serif' space-after.optimum='20pt' text-align='start' padding =\"4pt\">";
                        else
                            skin += @"<label for='AppPressId' class='LabelWidth control-label'><span class='FieldTitle'></span></label>";
                        skin += formField.GenerateSkin(a, false, false, false, displayName);
                        if (a.skinType == SkinType.FO)
                            skin += "</fo:block></fo:table-cell>";
                        break;
                    case FormDefFieldType.FormContainerDynamic:
                        if (formField.width != null)
                            skin += @"<label for='AppPressId' " + lableStyle + "><span class='FieldTitle'>AppPressDisplayName</span></label>";
                        if (a.skinType == SkinType.FO)
                        {
                            skin += "<fo:table-cell number-columns-spanned='2'><fo:block font-size='8pt' font-family='sans-serif' space-after.optimum='20pt' text-align='start' padding =\"4pt\">";
                            if (displayName.Length > 0)
                                skin += displayName;
                        }
                        else
                        {
                            skin += "<div>";
                        }
                        if (a.skinType != SkinType.FO)
                            skin += "<span class='help-block has-success no-margin' id='AppPressHelpId'>AppPressHelpText</span><span class='help-block has-error no-margin' id='AppPressErrorId'></span>";
                        switch (formField.GetFormContainerStyle())
                        {
                            case FormContainerStyle.Grid:
                                skin += formField.GenerateGridSkin(a);
                                break;
                            case FormContainerStyle.InLine:
                                skin += @"
                                    <!--|AppPress." + formField.fieldName + @".RowBegin|--><!--|AppPress." + formField.fieldName + @".RowEnd|-->";
                                break;
                            default:
                                throw new Exception("Invalid Form Container Style");

                        }
                        //skin += "<br/>" + formField.GenerateFormContainerFields(a, isfFO);
                        if (a.skinType == SkinType.FO)
                            skin += "</fo:block></fo:table-cell>";
                        else
                            skin += "</div>";
                        break;
                    default:
                        throw new NotImplementedException();
                }
                if (formField.IsDateRange == 0)
                {
                    if (a.skinType == SkinType.FO)
                        skin += "</fo:table-row><!--|AppPress." + formField.fieldName + ".End|-->\n";
                    else
                        skin += "</div>\n<!--|AppPress." + formField.fieldName + ".End|-->";
                }
                //else
                //    if (isfFO)
                //        skin += "</fo:table-row>";
            }
            if (skin.Trim() != string.Empty)
            {
                var stop = string.Empty;
                if (a.skinType == SkinType.FO)
                    stop += "<fo:table>\n<fo:table-body>";
                else
                    stop += @"";
                if (fieldName != null)
                    stop += "<!--|AppPress." + fieldName + ".RowBegin|-->\n";
                else if (a.skinType != SkinType.FO)
                    stop += "<div class='box box-none'><div class='form-horizontal'><div class='box-body'><div id='AppPressFormErrorId' class='callout callout-danger'></div>";
                skin = stop + skin;

                if (fieldName != null)
                {
                    //skin += "<td id='AppPressFormErrorId'></td>";
                    skin += "<!--|AppPress." + fieldName + ".RowEnd|-->\n";
                }
                if (a.skinType == SkinType.FO)
                    skin += "</fo:table-body >\n</fo:table>\n";
                else
                    skin += "</div></div></div>";
            }
            return skin;
        }

        internal Dictionary<string, string> GenerateLocalizationKey(Dictionary<string, string> ExistingKeys)
        {
            Dictionary<string, string> newKeys = new Dictionary<string, string>();
            var formKeys = _GenerateLocalizationKey();
            foreach (var itemKey in formKeys.Keys)
            {
                if (!ExistingKeys.ContainsKey(itemKey))
                    newKeys.Add(itemKey, formKeys[itemKey]);
            }
            return newKeys;
        }
        private Dictionary<string, string> _GenerateLocalizationKey()
        {
            var formKeys = new Dictionary<string, string>();
            for (int i = 0; i < formFields.Count(); ++i)
            {
                var formField = formFields[i];
                if (formField.Hidden || formField.Type == FormDefFieldType.ForeignKey)
                    continue;
                var keyName = formField.fieldName;
                if (formField.OriginalType == (int)FormDefFieldType.FormContainerGrid)
                    keyName = formField.Label;
                if (keyName == null || keyName == string.Empty)
                    continue;
                if (!formKeys.ContainsKey("LKey_" + keyName))
                {
                    string displayName = formField.Label;
                    if (displayName == string.Empty)
                        continue;
                    if (displayName == null)
                        displayName = AppPress.InsertSpacesBetweenCaps(keyName);
                    formKeys.Add("LKey_" + keyName, displayName);
                }
            }
            return formKeys;
        }

        internal string GenerateCode()
        {
            var code = "<div style='font-family:consolas;font-size:12px'><span style='color:blue'>using</span> System;<br/><span style='color:blue'>using</span> System.Collections.Generic;<br/><span style='color:blue'>using</span> ApplicationClasses;<br/><span style='color:blue'>using</span> AppPressFramework;<br/><br/><span style='color:blue'>namespace</span> Application<br/>{<br/><br/>\t<span style='color:blue'><span style='margin-left:" + 40 + "px'></span>public partial class</span> AppLogic\t{<br/>";
            code += _GenerateCode(2);
            return code + "<br/><span style='margin-left:" + 40 + "px'></span>}<br/><br/>}</div>";

        }
        internal string _GenerateCode(int indent, FormField formField1 = null, string functionName = null)
        {
            string indentTabs = "<span style='margin-left:" + indent * 40 + "px'></span>";
            var code = "";
            var aClassName = AppPress.Settings.ApplicationAppPress.Name;
            if (formField1 == null)
            {
                code = "<br/>" + indentTabs + "<span style='color:green'>// " + formName + "</span><br/>";
                code += indentTabs + "<span style='color:blue'>public static void</span> Init(<span style='color:#2B91AF'>" + aClassName + "</span> a, " + "<span style='color:#2B91AF'>" + this.GetClassName().Replace("AppPress.", "").Replace("+", ".") + "</span>" + " " + this.formName + ") {}<br/>";
                code += indentTabs + "<span style='color:blue'>public static void</span> BeforeSave(<span style='color:#2B91AF'>" + aClassName + "</span> a, " + "<span style='color:#2B91AF'>" + this.GetClassName().Replace("AppPress.", "").Replace("+", ".") + "</span>" + " " + this.formName + ") {}<br/>";
                code += indentTabs + "<span style='color:blue'>public static void</span> AfterSave(<span style='color:#2B91AF'>" + aClassName + "</span> a, " + "<span style='color:#2B91AF'>" + this.GetClassName().Replace("AppPress.", "").Replace("+", ".") + "</span>" + " " + this.formName + ") {}<br/>";
            }
            for (int i = 0; i < formFields.Count(); ++i)
            {
                var formField = formFields[i];
                if (formField1 != null && formField1.id != formField.id)
                    continue;
                if (formField.Type == FormDefFieldType.ForeignKey)
                    continue;
                if (formField.OriginalType == (int)FormDefFieldType.MultiFileUpload)
                    continue;
                code += Environment.NewLine;
                if (functionName == null || functionName == "OnClick" || functionName == "OnChange")
                    switch (formField.Type)
                    {
                        case FormDefFieldType.Button:
                            var comment = "/* Perform Actions*/";
                            if (formField.OriginalType == (int)FormDefFieldType.FormContainerGrid)
                            {
                                if (formField.fieldName.StartsWith("Add"))
                                    comment = "<br/>//" + formField.containerFormField.formDef.GetClassName().Replace("AppPress.", "").Replace("+", ".") + "." + formField.containerFormField.fieldName + "PopupClass.Popup(a,null,null);<br/>";
                                if (formField.fieldName.StartsWith("Modify"))
                                    comment = "<br/>//" + formField.containerFormField.formDef.GetClassName().Replace("AppPress.", "").Replace("+", ".") + "." + formField.containerFormField.fieldName + "PopupClass.Popup(a," + formField.fieldName + ".FormData." + formField.containerFormField.fieldName + ".GetSingleSelection().id,null);<br/>";
                                if (formField.fieldName.StartsWith("Delete"))
                                    comment = "<br/>//" + formField.fieldName + ".FormData.Delete(a);<br/>";
                                if (formField.fieldName.StartsWith("Save"))
                                    comment = @"<br/>" + indentTabs + @"//a.BeginTrans();<br/>
" + indentTabs + @"//try {<br/>
" + indentTabs + @"//" + formField.fieldName + @".FormData.Save(a);<br/>
" + indentTabs + @"//" + formField.fieldName + @".FormData.RefreshContainer(a);<br/>
" + indentTabs + @"//a.ClosePopup();<br/>
" + indentTabs + @"//a.CommitTrans();<br/>
" + indentTabs + @"//}<br/>
" + indentTabs + @"//catch (Exception)<br/>
" + indentTabs + @"//{<br/>
" + indentTabs + @"//a.RollbackTrans();<br/>
" + indentTabs + @"//throw;<br/>
" + indentTabs + @"//}<br/>
";
                            }
                            code += indentTabs + @"
<span style='color:black'><span style='color:blue'>public static void</span> OnClick(<span style='color:#2B91AF'>" + aClassName + "</span> a, " + "<span style='color:#2B91AF'>" + formField.GetClassName().Replace("AppPress.", "").Replace("+", ".") + "</span>" + " " + formField.fieldName + @") {
" + indentTabs + @"<span style='color:green'>" + comment + @"</span>
" + indentTabs + @"throw new NotImplementedException();}<br/></span>";
                            break;
                        case FormDefFieldType.PickMultiple:
                        case FormDefFieldType.Pickone:
                        case FormDefFieldType.TextArea:
                        case FormDefFieldType.Number:
                        case FormDefFieldType.Checkbox:
                        case FormDefFieldType.DateTime:
                        case FormDefFieldType.FileUpload:
                        case FormDefFieldType.Text:
                            code += indentTabs + @"
<span style='color:black'><span style='color:blue'>public static void</span> OnChange(<span style='color:#2B91AF'>" + aClassName + "</span> a, " + "<span style='color:#2B91AF'>" + "<span style='color:#2B91AF'>" + formField.GetClassName().Replace("AppPress.", "").Replace("+", ".") + "</span>" + "</span>" + " " + formField.fieldName + @") {
" + indentTabs + @"<span style='color:green'>/* Perform Actions*/</span>
" + indentTabs + @"throw new NotImplementedException();}<br/></span>";
                            if (formField.Type != FormDefFieldType.FileUpload)
                                code += indentTabs + @"
<span style='color:black'><span style='color:blue'>public static void</span> Calc(<span style='color:#2B91AF'>" + aClassName + "</span> a, " + "<span style='color:#2B91AF'>" + "<span style='color:#2B91AF'>" + formField.GetClassName().Replace("AppPress.", "").Replace("+", ".") + "</span>" + "</span>" + " " + formField.fieldName + @") {
" + indentTabs + @"<span style='color:green'>/* Calculate the val of the field*/</span>
" + indentTabs + @"throw new NotImplementedException();}<br/></span>";
                            break;
                    }
                if (functionName == null || functionName == "Options")
                    switch (formField.Type)
                    {
                        case FormDefFieldType.Checkbox:
                        case FormDefFieldType.PickMultiple:
                        case FormDefFieldType.Pickone:
                            code += indentTabs + @"
<span style='color:black'><span style='color:blue'>public static string</span> Options(<span style='color:#2B91AF'>" + aClassName + "</span> a, <span style='color:#2B91AF'>" + formField.GetClassName().Replace("AppPress.", "").Replace("+", ".") + "</span>" + " " + formField.fieldName + @") {
" + indentTabs + @"<span style='color:green'>/* return a SQL Query*/</span>
" + indentTabs + @"<span style='color:green'>/* return ""Select Id, Value From TableName Order By Id"";*/</span>
" + indentTabs + @"throw new NotImplementedException();}<br/></span>";
                            code += indentTabs + @"
<span style='color:black'><span style='color:blue'>public static List&lt;Option&gt;</span> Options(<span style='color:#2B91AF'>" + aClassName + "</span> a, " + "<span style='color:#2B91AF'>" + formField.GetClassName().Replace("AppPress.", "").Replace("+", ".") + "</span>" + " " + formField.fieldName + @") {
" + indentTabs + @"<span style='color:green'>/* return list of Option */</span>
throw new NotImplementedException();}<br/></span>";
                            break;
                    }
                if (functionName == null || functionName == "Domain")
                    switch (formField.Type)
                    {
                        case FormDefFieldType.FormContainerDynamic:
                            if (formField.GetFormContainerStyle() == FormContainerStyle.Grid)
                            {
                                var columns = "Id";
                                var rowFomDef = formField.rowFormDef;
                                if (rowFomDef == null)
                                    throw new Exception(formField.GetDescription() + " is of Style Grid but does not contain RowFormDef");
                                foreach (var f in rowFomDef.formFields)
                                    if (!f.fieldName.Equals("SelectRow", StringComparison.OrdinalIgnoreCase) && !f.fieldName.Equals("Id", StringComparison.OrdinalIgnoreCase) && f.Type != FormDefFieldType.FormContainerDynamic && f.Type != FormDefFieldType.FormContainerGrid && f.Type != FormDefFieldType.Button)
                                        columns += "," + f.fieldName;
                                code += indentTabs + @"
<span style='color:black'><span style='color:blue'>public static string</span> Domain(<span style='color:#2B91AF'>" + aClassName + "</span> a, " + "<span style='color:#2B91AF'>" + formField.GetClassName().Replace("AppPress.", "").Replace("+", ".") + "</span>" + " " + formField.fieldName + @") {
" + indentTabs + @"<span style='color:green'>/* return a SQL Query having columns of the row*/</span>
" + indentTabs + @"<span style='color:green'>/* return ""Select " + columns + @" From TableName Order By Id"";*/</span>
" + indentTabs + @"throw new NotImplementedException();}<br/></span>";
                            }
                            code += indentTabs + @"
<span style='color:black'><span style='color:blue'>public static List&lt;FormData&gt;</span> Domain(<span style='color:#2B91AF'>" + aClassName + "</span> a, " + "<span style='color:#2B91AF'>" + formField.GetClassName().Replace("AppPress.", "").Replace("+", ".") + "</span>" + " " + formField.fieldName + @") {
" + indentTabs + @"<span style='color:green'>/* return a List of FormData*/</span>
" + indentTabs + @"throw new NotImplementedException();}<br/></span>";
                            if (functionName == null)
                            {
                                if (formField.rowFormDef != null && formField.rowFormDef.GenerationType != 0)
                                    code += formField.rowFormDef._GenerateCode(indent + 1);
                                var popupFormName = formField.fieldName + "Popup";
                                var popupFormDef = AppPress.FindFormDef(popupFormName);
                                if (popupFormDef != null && popupFormDef.GenerationType != 0)
                                    code += popupFormDef._GenerateCode(indent + 1);
                            }
                            code += "<br/>";
                            break;
                    }

            }
            return code;
        }

        internal static void ValidateForm(AppPress a)
        {
            FormData formData = FormData.GetFormData(a);
            if (!formData._Validate())
            {
                throw new AppPressException();
            }
        }

        internal string GetViewQuery(AppPress a)
        {
            if (TableName == null)
                return null;
            return "Select * From " + a.SQLQuote + TableName + a.SQLQuote + " Where 1=1\n";
        }
        internal void Popup(AppPress a, string formId, PopupParams popupParams)
        {
            var popup = AppPressResponse.Popup(a, this, formId, popupParams);
            if (popupParams != null)
            {
                popup.popupWidth = popupParams.PopupWidth;
                popup.popupHeight = popupParams.PopupHeight == null ? "auto" : popupParams.PopupHeight.ToString();
                if (popupParams.title != null)
                    popup.popupTitle = popupParams.title;
                popup.popupPosition = popupParams.PopupPosition;
            }
            // Move Field Errors after Popup as the error may be using the popup fields
            a.AppPopup(popup);
        }
        internal void Redirect(AppPress a, string formId, RedirectParams redirectParams)
        {
            a.appPressResponse.Add(AppPressResponse.Redirect(a, id, formId, redirectParams));
        }
        internal Type GetTopClassType()
        {
            if (ClassType != null)
                return ClassType;
            var formDef = this;
            var calcClassName = formName + "Class";
            while (formDef.ContainerFormField != null)
            {
                formDef = formDef.ContainerFormField.formDef;
                if (formDef.extensionFormDefId != 0)
                {
                    var formDef1 = AppPress.FindFormDef(formDef.extensionFormDefId);
                    calcClassName = formDef.formName + "Class+" + calcClassName;
                    formDef = formDef1;
                }
                else
                    calcClassName = formDef.formName + "Class+" + calcClassName;
            }
            ClassType = Util.GetType(calcClassName);
            return ClassType;
        }
        internal string GetClassName()
        {
            var calcClassName = formName + "Class";
            var formDef = this;
            while (formDef.ContainerFormField != null)
            {
                formDef = formDef.ContainerFormField.formDef;
                if (formDef.extensionFormDefId != 0)
                {
                    var formDef1 = AppPress.FindFormDef(formDef.extensionFormDefId);
                    calcClassName = formDef.formName + /*"_" + formDef1.formName + */"Class+" + calcClassName;
                    formDef = formDef1;
                }
                else
                    calcClassName = formDef.formName + "Class+" + calcClassName;
            }
            return calcClassName;
        }

        internal string GenerateDeveloperLinks(AppPress a)
        {
            var baseUrl = Util.GetBaseUrl();
            var AspxPage = AppPress.GetDefaultAspx();
            var formDef = this;
            var localizationLink = "";
            if (this.FormType == AppPressFramework.FormType.MasterForm)
                localizationLink = "&nbsp;<a class='iframe' href='" + baseUrl + AspxPage + "?Form=" + formDef.formName + "&GetLocalization=' target='_blank' style='color:orange'>Localization</a><br/>";

            var s = @" 
                  <div style='margin-left: 5px;font-size:11px;float:none'><span>Dev Links: " +
                  formDef.formName + "&nbsp;<a class='iframe' href='" + baseUrl + AspxPage + "?GetSkin=" + formDef.id + "&Popup=' target='_blank' style='color:blue'>skin</a>" +
                  "&nbsp;<a class='iframe' href='" + baseUrl + AspxPage + "?GetFO=" + formDef.id + "' target='_blank' style='color:blue'>fo</a>" +
                  "&nbsp;<a class='iframe' href='" + baseUrl + AspxPage + "?GetCode=" + formDef.id + "' target='_blank' style='color:red'>code</a>" +
                   localizationLink +
                  "</span></div>";
#if DEBUG
            if (a.fieldsNotGenerated != null && a.fieldsNotGenerated.Count > 0)
            {
                s += "<span style='color:red;font-size:20px'>Could not find in Skin following Fields: ";
                foreach (var fieldValue in a.fieldsNotGenerated)
                    s += fieldValue.formField.formDef.formName + ":" + fieldValue.formField.fieldName + ", ";
                s += "</span>";
            }
#endif
            return s;
        }

        internal string GetDescription()
        {
            return "Form: " + formName;
        }
    }
}
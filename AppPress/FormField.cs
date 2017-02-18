using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

namespace AppPressFramework
{
    // Abbreviated FormField Class to use in JS. FormField needs to be serialized fully in case of writing in CS file and for JS few fields needs to 
    // be serialized. Hence this class

    public enum TimeFormat
    {
        Hours = 1,
        Minutes = 2,
        Seconds = 3
    }
    [DataContract]
    internal class FormFieldJS
    {
        [DataMember]
        internal FormDefFieldType Type = 0;
        [DataMember]
        internal FormDefFieldStyle Style;
        [DataMember]
        internal long id;
        [DataMember]
        internal bool Static;

    }

    [DataContract]
    public class FormField
    {

        [DataMember]
        public string fieldName;
        [DataMember]
        public FormDefFieldType Type = 0;
        [DataMember]
        public FormDefFieldStyle Style;
        [DataMember]
        internal long id;
        [DataMember]
        public bool Static;
        [DataMember]
        public bool NonStatic = false;
        [DataMember]
        public bool Hidden = false;
        [DataMember]
        public FormField containerFormField; // fields within a formContainer
        [DataMember]
        public int OriginalType = (int)FormDefFieldType.None;
        [DataMember]
        public bool DoNotSaveInDB;
        // for Type=FormContainer
        [DataMember]
        public int decimals = 0;
        [DataMember]
        public string width = null, height = null;
        [DataMember]
        internal List<ServerFunction> FieldFunctions = new List<ServerFunction>();
        [DataMember]
        internal Dictionary<string, string> UserControlParameters = new Dictionary<string, string>();
        [DataMember]
        internal string containerColumnName; // which grid column header the field will be displayed. For Sorting symbol
        [DataMember]
        public string Shortcut;
        [DataMember]
        public bool ZeroAsBlank;
        [DataMember]
        public int? MaxFileSizeInKB = null;
        [DataMember]
        public int? MinChars;
        [DataMember]
        public int? MaxChars;
        [DataMember]
        public string TableName;
        [DataMember]
        public string PrimaryKey;
        [DataMember]
        public string Label;
        [DataMember]
        public string PopupTitle;
        [DataMember]
        internal int IsDateRange = 0; // 1 DateFrom, 2 DateTo
        [DataMember]
        public EncryptionType? EncryptionType;
        [DataMember]
        public bool Contiguous;
        [DataMember]
        public bool NonOverlapping;
        [DataMember]
        public bool Required = false;
        [DataMember]
        public bool EmailValidation = false;
        [DataMember]
        public string RegexValidation = null;
        [DataMember]
        public bool DateFromRequired;
        [DataMember]
        public bool DateToRequired;
        [DataMember]
        public decimal? MinimumValue = null;
        [DataMember]
        public decimal? MaximumValue = null;
        [DataMember]
        public string CSSClass = null;
        [DataMember]
        public string LabelStyle = null;
        [DataMember]
        public string ControlStyle = null;
        [DataMember]
        public bool AllowMultiSelect = false;
        [DataMember]
        public bool StaticSubmitValue = false;
        [DataMember]
        public bool NoSubmit = false;
        [DataMember]
        public TimeFormat TimeFormat = TimeFormat.Minutes;
        [DataMember]
        public FileUploadStorageType FileUploadStorage = FileUploadStorageType.Database;
        [DataMember]
        public string FileUploadDirectory = null;
        [DataMember]
        public bool AutoUpload = false;
        [DataMember]
        public bool SaveOnUpload = false;
        [DataMember]
        public string Accept = null;
        [DataMember]
        public bool ValidateOnServer = false;
        [DataMember]
        public string GroupName = null;
        [DataMember]
        internal string ExtensionFormName;
        [DataMember]
        internal string FormNameProperty = null; // FormName for Redirect Field
        [DataMember]
        public bool RowFilter; // for RowFields        
        [DataMember]
        public bool Sortable = false; // for RowFields
        [DataMember]
        internal long? formDefId = null; // Used only in RemoteForms
        [DataMember]
        internal string PartStyle;
        [DataMember]
        internal string ContainerStyle;
        [DataMember]
        public bool NonSecure = false;
        [DataMember]
        public string SaveTableName = null;
        /// <summary>
        /// table to save Pick Multiple Options
        /// </summary>
        [DataMember]
        public string SaveTableForeignKey;
        /// <summary>
        /// for text field. No encoding of & to &amps;.usefull for URLs
        /// </summary>
        [DataMember]
        public bool NoEncode = false;
        [DataMember]
        public string Placeholder = null;
        [DataMember]
        public string UserControlScalarFieldName = null;
        [DataMember]
        internal bool ShowSelectAll = false;
        [DataMember]
        internal bool decimalsAssigned = false;
        [DataMember]
        public string DateFormat = null;
        [DataMember]
        public string Help = null;
        [DataMember]
        public string Role = null;

        internal List<Option> optionsCache = null; // for caching options generated from ForeignKey

        internal FormDef formDef;
        internal string Suffix = null;
        internal FormDef rowFormDef;
        internal List<FormField> rowFields = null;
        internal List<FormField> popupFields = null;
        internal Object fieldDetail = null; // Stores data specific to FieldType
        internal int NumberOfColumn = 1;
        internal List<ServerFunctionParameter> Parameters = new List<ServerFunctionParameter>();
        internal UploadFileDetail UploadDetail;
        internal bool Extension = false;
        public long dbId = 0;
        internal List<MethodCache> BeforeDeleteMethods = null;
        internal List<MethodCache> AfterDeleteMethods = null;
        internal List<MethodCache> CalcMethods = null;

        internal FormField()
        {
            this.id = AppPress.GetUniqueId();
        }
        /// <summary>
        /// ???
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="Type"></param>
        public FormField(string fieldName, FormDefFieldType Type)
            : this()
        {
            if (Type == FormDefFieldType.MergedForm)
                throw new Exception("FieldType: " + Type + " is not allowed for Field: " + fieldName);

            this.fieldName = fieldName;
            this.Type = Type;
        }
        internal FormField Clone()
        {
            return (FormField)MemberwiseClone();
        }
        /// <summary>
        /// For Dynamic Forms bind the Pickone or PickMultiple fields to Options function
        /// public static string <functionName>(AppPress a, FieldValue fieldValue)
        /// public static List<Option> <functionName>(AppPress a, FieldValue fieldValue)
        /// </summary>
        /// <param name="a"></param>
        /// <param name="functionName"></param>
        public void AddOptionFunction(AppPress a, string functionName)
        {
            if (Type != FormDefFieldType.PickMultiple && Type != FormDefFieldType.Pickone)
                throw new AppPressException("AddOptionFunction can only be called for Fields of Type Pickone or PickMultiple");
            FieldFunctions.Add(new ServerFunction(a, FunctionType.Options, functionName));
        }

        internal FormField(string fieldName, FormDefFieldType formDefFieldType, FormDef parentFormDef)
            : this(fieldName, formDefFieldType)
        {
            this.formDef = parentFormDef;
        }
        internal FormField(string formName, string fieldName)
        {
            this.fieldName = fieldName;
            this.formDef = new FormDef();
            this.formDef.formName = formName;
        }
        internal MethodInfo GetMethod(AppPress a, string functionName)
        {
            var type = Util.GetType(a, formDef, id);
            //Add null check. Its come null for functionality in ExtensionForm.
            if (type != null)
                return Util.GetMethod(a, functionName, new Type[] { type });
            else
                return null;
        }
        internal StringBuilder BuildOptionsForJS()
        {
            var AppPressJSStr = new StringBuilder();
            var formField = this;
            AppPressJSStr.Append("optionsCache[").Append(formField.id).Append("] = new Array();\n");
            for (int i = 0; i < formField.optionsCache.Count; ++i)
            {
                var o = formField.optionsCache[i];
                AppPressJSStr.Append("optionsCache[").Append(formField.id).Append("][").Append(i).Append("]= new Object();optionsCache[").Append(formField.id).Append("][").Append(i).Append("].id='").Append(o.id).Append("';");
                AppPressJSStr.Append("optionsCache[").Append(formField.id).Append("][").Append(i).Append("].localizedValues=new Object();");
                if (AppPress.LocalizationData.ContainsKey(o.value))
                {
                    var l = AppPress.LocalizationData[o.value];
                    foreach (var l1 in l)
                        AppPressJSStr.Append("optionsCache[").Append(formField.id).Append("][").Append(i).Append("].localizedValues['" + l1.Key + "']='").Append(HttpUtility.JavaScriptStringEncode(l1.Value)).Append("';\n");
                }
                else
                    AppPressJSStr.Append("optionsCache[").Append(formField.id).Append("][").Append(i).Append("].localizedValues['" + "English" + "']='").Append(HttpUtility.JavaScriptStringEncode(o.value)).Append("';\n");
            }
            return AppPressJSStr;
        }
        internal void BuildOptions(AppPress a, FormDef formDef)
        {
            var optionsFunction = this.FieldFunctions.Find(t => t.ServerFunctionType == FunctionType.Options);
            if (optionsFunction != null)
                return;
            Type t1 = null;
            var formField = this;
            switch (formField.formDef.FormType)
            {
                case FormType.UserControlScalarForm:
                    t1 = Util.GetType(a, formField.formDef, formField.formDef.formFields[0].id); // will have only one field
                    break;
                case FormType.MergedForm:
                    t1 = Util.GetType(a, formField.formDef, formField.id);
                    break;
                default:
                    t1 = Util.GetType(a, formDef, formField.id);
                    break;
            }
            var optionsMethod = Util.GetMethod(a, "Options", new Type[] { AppPress.Settings.ApplicationAppPress, t1 });
            if (TableName != null)
            {
                if (optionsMethod != null)
                    throw new Exception(GetDescription() + ". If Options function is defined then TableName property should not be added in the field");
                var primaryKey = a.site.GetPrimaryKey(TableName);
                if (primaryKey == null)
                    throw new AppPressException(this.GetDescription() + " Table does not exist or there is no Primary Key for Table: " + TableName);
                GetOptionsFromTable(a, TableName, primaryKey, this.fieldName);
                return;
            }
            if (optionsMethod != null)
                return;
            string tableName = null;
            if (this.Type == FormDefFieldType.PickMultiple)
                tableName = this.SaveTableName;
            else
                tableName = formDef.TableName;
            if (tableName != null)
            {
                string query;
                if (a.site.databaseType == DatabaseType.MySql)
                    query = @"
                            SELECT REFERENCED_TABLE_NAME,REFERENCED_COLUMN_NAME 
                            FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE
                            WHERE TABLE_NAME = '" + tableName + @"'
                                AND COLUMN_NAME = '" + this.fieldName + @"'
                                AND REFERENCED_TABLE_NAME IS NOT NULL 
                                AND " + DAOBasic.SchemaColumnName + @" = '" + a.site.dbName + "'";
                else
                    query = @"
                                SELECT 
                                    tab2.name AS [referenced_table_name],
                                    col2.name AS [referenced_column_name],
                                    obj.name AS FK_NAME,
                                    sch.name AS [schema_name],
                                    tab1.name AS [table_name],
                                    col1.name AS [column_Name]
                                FROM sys.foreign_key_columns fkc
                                INNER JOIN sys.objects obj
                                    ON obj.object_id = fkc.constraint_object_id
                                INNER JOIN sys.tables tab1
                                    ON tab1.object_id = fkc.parent_object_id
                                INNER JOIN sys.schemas sch
                                    ON tab1.schema_id = sch.schema_id
                                INNER JOIN sys.columns col1
                                    ON col1.column_id = parent_column_id AND col1.object_id = tab1.object_id
                                INNER JOIN sys.tables tab2
                                    ON tab2.object_id = fkc.referenced_object_id
                                INNER JOIN sys.columns col2
                                    ON col2.column_id = referenced_column_id AND col2.object_id = tab2.object_id
                                WHERE tab1.name = '" + tableName + @"'
                                AND col1.name = '" + this.fieldName + @"'";
                var dr = a.ExecuteQuery(query);
                try
                {
                    if (dr.Read())
                    {
                        var rTableName = dr.GetString(0);
                        var rColumnName = dr.GetString(1);
                        var displayColumnName = this.fieldName;
                        dr.Close();
                        GetOptionsFromTable(a, rTableName, rColumnName, displayColumnName);
                    }
                }
                finally { dr.Close(); }
            }
        }

        private void GetOptionsFromTable(AppPress a, string optionsTableName, string optionsValueColumnName, string displayColumnName)
        {
            var exists = a.ExecuteString(@"
                                    SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS
                                    WHERE TABLE_NAME = '" + optionsTableName + @"' and COLUMN_NAME = '" + displayColumnName + "' and " + DAOBasic.SchemaColumnName + @"='" + a.site.dbName + "'") != null;
            if (!exists)
            {
                // get second column
                displayColumnName = a.ExecuteString(@"
                                        SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS
                                        WHERE TABLE_NAME = '" + optionsTableName + @"' And DATA_TYPE in ('varchar','nvarchar', 'nchar', 'char') and " + DAOBasic.SchemaColumnName + @"='" + a.site.dbName + "' Order By ORDINAL_POSITION");
                if (displayColumnName == null)
                    throw new Exception(this.GetDescription() + " Could not Find Column '" + this.fieldName + "' or a column of type 'varchar','nvarchar', 'nchar', 'char' in Table: " + optionsTableName + " for generating options for Pick Field");
            }
            if (this.Style != FormDefFieldStyle.AutoComplete)
            {
                var dr = a.ExecuteQuery("Select " + a.SQLQuote + optionsValueColumnName + a.SQLQuote + ", " + a.SQLQuote + displayColumnName + a.SQLQuote + " From " + a.SQLQuote + optionsTableName + a.SQLQuote + " Order by " + a.SQLQuote + optionsValueColumnName + a.SQLQuote);
                this.optionsCache = new List<Option>();
                try
                {
                    while (dr.Read())
                    {
                        var s = dr.IsDBNull(1) ? "" : dr[1].ToString();
                        this.optionsCache.Add(new Option
                        {
                            id = dr[0].ToString(),
                            value = s
                        });
                    }
                }
                finally
                {
                    dr.Close();
                }
                if (this.Style == FormDefFieldStyle.DropDown)
                {
                    if (!this.Required)
                        this.optionsCache.Insert(0, new Option { id = null, value = "" });
                }
                if (AppPress.optionsCacheTables.Find(t => t.tableName == optionsTableName && t.optionFormField == this && t.formDef == formDef) == null)
                    AppPress.optionsCacheTables.Add(new OptionsCacheTables { tableName = optionsTableName, optionFormField = this, formDef = formDef });
            }
            else
            {
                var optionsFunction = new ServerFunction(a, FunctionType.Options, "GenericGetOptionsFromTable");
                optionsFunction.Parameters.Add(new ServerFunctionParameter { Name = "TableName", Value = optionsTableName });
                optionsFunction.Parameters.Add(new ServerFunctionParameter { Name = "IdColumnName", Value = optionsValueColumnName });
                optionsFunction.Parameters.Add(new ServerFunctionParameter { Name = "DisplayColumnName", Value = displayColumnName });
                FieldFunctions.Add(optionsFunction);
            }

        }

        internal List<ServerFunction> GetFieldFunctions(FunctionType functionType)
        {
            if (this.FieldFunctions == null)
                return new List<ServerFunction>();
            return this.FieldFunctions.FindAll(t => t.ServerFunctionType == functionType);
        }

        internal string GenerateSkin(AppPress a, bool forceStatic, bool gridContainerField, bool columnField, string displayName = null)
        {
            var controlStyle = "style=\"AppPressControlStyle\"";
            var fieldStatic = forceStatic || Static;
            var noMargin = "";
            if (columnField || gridContainerField)
                noMargin += " no-margin";
            if (gridContainerField)
                noMargin += " display-inline";
            var helpBlocks = "<span id='AppPressHelpId' class='help-block has-success" + noMargin + "'>AppPressHelpText</span><span id='AppPressErrorId' class='help-block has-error" + noMargin + "'></span>";
            if (a.skinType == SkinType.FO)
                helpBlocks = "";
            switch (Type)
            {
                case FormDefFieldType.MergedForm:
                case FormDefFieldType.UserControlScalar:
                    return "<!--|AppPress.FieldContent|-->";
                case FormDefFieldType.Pickone:
                    {
                        if (a.skinType == SkinType.FO)
                            return "AppPressValue";
                        if (fieldStatic)
                        {
                            var s = "<span " + controlStyle + ">AppPressValue</span>";
                            if (Help != null)
                                s += helpBlocks;
                            return s;
                        }
                        var placeholder = "";
                        if (Placeholder != null)
                            placeholder = @" placeholder=""" + HttpUtility.HtmlAttributeEncode(Placeholder) + @""" ";
                        var str = string.Empty;
                        if (Style == FormDefFieldStyle.DropDown)
                        {
                            str += @"<select id='AppPressId' onchange='AppPressOnChange' class='form-control select2' " + placeholder + @" style='display:none'>
                                    <!--|AppPress.PartBegin|--><option value='AppPressValue'>AppPressLabel</option><!--|AppPress.PartEnd|-->
                                    </select>";
                        }
                        else if (Style == FormDefFieldStyle.AutoComplete)
                        {
                            str += @"<select id='AppPressId' onchange='AppPressOnChange' class='form-control select2' " + placeholder + @" style='display:none'>
                                    <!--|AppPress.PartBegin|--><option value='AppPressValue'>AppPressLabel</option><!--|AppPress.PartEnd|-->
                                    </select>";
                        }
                        else if (Style == FormDefFieldStyle.Radio)
                        {
                            str += @"<!--|AppPress.PartBegin|-->
                                    <div class='pickoneRadio'><input type='radio' id='AppPressId:AppPressValue' name='AppPressId'
                                    onclick='AppPressOnChange' Value='AppPressValue' " + controlStyle + @"/><label for='AppPressId:AppPressValue'>AppPressLabel</label></div>
                                    <!--|AppPress.PartEnd|-->";
                        }
                        else if (Style == FormDefFieldStyle.ImageRotation)
                        {
                            str += @"<a id='AppPressId' onclick='AppPressRotateImage(this);AppPressOnChange'><span id='Image_AppPressId'></span></a>";
                        }
                        if (!Sortable)
                            str += helpBlocks;
                        return str;
                    }
                case FormDefFieldType.PickMultiple:
                    {
                        if (a.skinType == SkinType.FO)
                            return "<!--|AppPress.PartBegin|-->AppPressValue<!--|AppPress.PartEnd|-->";
                        if (fieldStatic || a.skinType == SkinType.FO)
                        {
                            var s = "<!--|AppPress.PartBegin|--><span " + controlStyle + ">AppPressValue</span><!--|AppPress.PartEnd|-->";
                            if (Help != null)
                                s += helpBlocks;
                            return s;
                        }
                        var str = string.Empty; //Change width from 200px to 100%
                        var fieldClass = "";
                        if (CSSClass != null)
                            fieldClass = CSSClass;
                        if (Style == FormDefFieldStyle.DropDown)
                        {
                            str += @"<select id='AppPressId' onchange='AppPressOnChange' multiple='multiple' class='form-control select2' style='display:none'>
                                    <!--|AppPress.PartBegin|--><option value='AppPressValue'>AppPressLabel</option><!--|AppPress.PartEnd|-->
                                    </select>";
                        }
                        else
                        {

                            var onChangeStr = "  onclick='AppPressOnClick' ";
                            var checkBoxesStr = @"<!--|AppPress.PartBegin|--><div class='checkbox'>";
                            if (!RowFilter)
                                checkBoxesStr += @"<label class='LabelWidth'></label>";
                            checkBoxesStr += @"<label style='AppPressPartStyle' for=""AppPressId:AppPressValue"">
                                                    <input type=""checkbox"" id=""AppPressId:AppPressValue"" name=""AppPressId"" value=""AppPressValue"" " + onChangeStr + @"/>
                                                    AppPressLabel
                                                </label></div>
                                              <!--|AppPress.PartEnd|-->";
                            if (ShowSelectAll)
                            {
                                var cstr = @"<div class='checkbox'>";
                                if (!RowFilter)
                                    cstr += @"<label class='LabelWidth'></label>";
                                cstr += @"<input type='checkbox' id='SelectAll_AppPressId' onclick='CheckUncheckAll(this,""AppPressId"")'/></div>";
                                checkBoxesStr = cstr + checkBoxesStr;
                            }
                            checkBoxesStr = "<div " + controlStyle + " class='" + fieldClass + "'>" + checkBoxesStr + "</div>";
                            str += checkBoxesStr;
                        }
                        str += helpBlocks;
                        return str;
                    }
                case FormDefFieldType.FileUpload:
                    {
                        if (a.skinType == SkinType.FO)
                            return "";
                        var s = "";
                        if (!fieldStatic)
                            s += @"<!--|AppPress.FileUpload.UploadPartBegin|-->
                                <button class=""btn btn-primary btn-xs"" type=""button"" onclick=""$(JQueryEscape('#AppPressId')).click()"">
                                Choose File
                                </button>
                                <input id='AppPressId' fileid='AppPressValue' type='file' name='file' style=""display:none"" />
                                <span id='UIFileName:AppPressId' class='fileUploadPendingFileName'></span>
                                <span id='UIProgress:AppPressId'><span class='bar' style='width: 0%;'></span></span>
                                <a id='UIFileUpload:AppPressId' class='appPressButton appPressSmallButton' itemprop='None'><span></span></a>
                                <!--|AppPress.FileUpload.UploadPartEnd|-->";
                        if (OriginalType != (int)FormDefFieldType.MultiFileUpload)
                        {
                            s += @"<!--|AppPress.FileUpload.FileNamePartBegin|--><a class='appPressLink' href=""AppPressFileUploadDownloadUrl"">AppPressFileUploadFileName</a><!--|AppPress.FileUpload.FileNamePartEnd|-->";
                            if (!Required && !fieldStatic)
                                s += @"<!--|AppPress.FileUpload.DeletePartBegin|-->
                                <button class=""btn btn-danger btn-xs"" type=""button"" onclick=""AppPressFileUploadDeleteUrl"">x</button>
                                <!--|AppPress.FileUpload.DeletePartEnd|-->";
                        }
                        if (!fieldStatic)
                            s += helpBlocks;
                        return s;
                    }
                case FormDefFieldType.FormContainerDynamic:
                    {
                        var fieldsSkin = @"<span id='AppPressHelpId' class='help-block has-success nomargin'>AppPressHelpText</span><span id='AppPressErrorId' class='help-block has-error nomargin'></span>";
                        switch (GetFormContainerStyle())
                        {
                            case FormContainerStyle.Grid:
                                return GenerateGridSkin(a) + fieldsSkin;
                            case FormContainerStyle.InLine:
                                return @"<span id='AppPressFormErrorId'></span><!--|AppPress." + fieldName + @".RowBegin|--><!--|AppPress." + fieldName + @".RowEnd|-->" + fieldsSkin;
                            default:
                                throw new Exception("Invalid Form Container Style");
                        }
                    }
                case FormDefFieldType.Checkbox:
                    {
                        if ((fieldStatic && fieldName != "SelectRow") || a.skinType == SkinType.FO)
                        {
                            var s = "<span " + controlStyle + ">AppPressValue</span>";
                            if (Help != null)
                                s += helpBlocks;
                            return s;
                        }
                        var str = "<input type='checkbox' id='AppPressId' onclick='AppPressOnClick' AppPressValue />";
                        return str;
                    }
                case FormDefFieldType.Password:
                    {
                        if (a.skinType == SkinType.FO)
                            return "";
                        var sizeAttr = MaxChars != null ? "maxlength='" + MaxChars + "'" : "";

                        string str = "<input " + controlStyle + @" type='password' " + sizeAttr + " id='AppPressId' value='AppPressValue'/>";
                        str += helpBlocks;
                        return str;
                    }
                case FormDefFieldType.HTML:
                    {
                        var str = "<div id='AppPressId'>AppPressValue</div>";
                        if (formDef.FormType != FormType.ContainerRowFormGenerated || formDef.GenerationType != 1 || Help != null)
                            str += helpBlocks;
                        return str;
                    }
                case FormDefFieldType.Number:
                case FormDefFieldType.Text:
                    {
                        if (a.skinType == SkinType.FO)
                            return "AppPressValue";
                        string str;
                        if (fieldStatic)
                        {
                            var cs = controlStyle;
                            if (this.Type == FormDefFieldType.Number && formDef.GenerationType == 1) // for columns of grid only
                                cs = "style=\"AppPressControlStyle;float:right;\"";
                            if (gridContainerField)
                                str = "&nbsp;<span " + cs + @">AppPressValue</span>";
                            else
                                str = "<span " + cs + @">AppPressValue</span>";
                        }
                        else
                        {
                            var placeholder = "";
                            if (Placeholder != null)
                                placeholder = @" placeholder=""" + HttpUtility.HtmlAttributeEncode(Placeholder) + @""" ";
                            str = "<input " + controlStyle + placeholder + @" type='text' maxlength='AppPressMaxLength' id='AppPressId' value='AppPressValue' onchange='AppPressOnChange'/>";
                        }
                        // do not generate for Static field in Grid
                        if (!fieldStatic || formDef.FormType != FormType.ContainerRowFormGenerated || formDef.GenerationType != 1 || Help != null)
                            str += helpBlocks;
                        return str;
                    }
                case FormDefFieldType.TextArea:
                    {
                        if (a.skinType == SkinType.FO)
                            return "AppPressValue";
                        if (fieldStatic)
                        {
                            var s = "<div " + controlStyle + @">AppPressValue</div>";
                            if (Help != null)
                                s += helpBlocks;
                            return s;
                        }
                        var placeholder = "";
                        if (Placeholder != null)
                            placeholder = @" placeholder=""" + HttpUtility.HtmlAttributeEncode(Placeholder) + @""" ";
                        string str = "<textarea  " + controlStyle + placeholder + @" maxlength='AppPressMaxLength' id='AppPressId' onchange='AppPressOnChange'>AppPressValue</textarea>";
                        str += helpBlocks;
                        return str;
                    }
                case FormDefFieldType.Button:
                    {
                        if (a.skinType == SkinType.FO)
                            return "";

                        if (CSSClass.IsNullOrEmpty())
                            CSSClass = "";
                        string str;
                        var roleStr = "";
                        if (Role != null)
                            roleStr = " role='"+Role+"'";
                        if (Style == FormDefFieldStyle.Link)
                        {
                            str = "<a id='AppPressId'"+roleStr+" " + controlStyle + @" class='appPressLink " + CSSClass + "' onclick='AppPressOnClick'>AppPressDisplayName</a>";
                        }
                        else
                        {
                            var buttonSize = "";
                            if (gridContainerField)
                                buttonSize = "btn-xs";
                            str = @"<button id=""AppPressId""" + roleStr + @" onclick='AppPressOnClick' class=""btn btn-primary " + buttonSize + @""" type=""button"">AppPressDisplayName</button>";
                        }
                        if (!Sortable)
                            str += helpBlocks;
                        return str;
                    }
                case FormDefFieldType.DateTime:
                    {
                        if (a.skinType == SkinType.FO)
                            return "AppPressValue";
                        if (fieldStatic)
                        {
                            var s = "<span " + controlStyle + ">AppPressValue</span>";
                            s += helpBlocks;
                            return s;
                        }
                        var str = "<input type='text' " + controlStyle + @" size='12' maxlength='20' value='AppPressValue' id='AppPressId' onchange='AppPressOnChange' /> ";
                        if (IsDateRange == 0)
                            str += helpBlocks;
                        return str;
                    }
                case FormDefFieldType.ForeignKey:
                    return string.Empty;
            }
            throw new NotImplementedException();
        }

        internal string GenerateGridSkin(AppPress a)
        {
            string skin = "";

            var rowFormDef = GetContainerRowFormDef(a);

            if (rowFormDef == null)
            {
                skin += "<!--|AppPress." + fieldName + ".RowBegin|--><!--|AppPress." + fieldName + ".RowEnd|-->";
            }
            else
            {
                if (a.skinType == SkinType.FO)
                {
                    skin += "<fo:table>";
                }
                else
                {
                    var gridClass = "table ";
                    if (this.CSSClass != null)
                        gridClass += this.CSSClass;
                    if (height != null)
                        gridClass += " TableScroll ";
                    skin +=
                        "<table id='FormContainerTable' class='" + gridClass + " tbl_valign_top'";
                    if (width != null)
                    {
                        int w;
                        string wstr = width;
                        if (int.TryParse(width, out w))
                            wstr = w + "px";
                        skin += " style='width:" + wstr + "'";
                    }
                    skin += ">";

                    var cf = GenerateFormContainerFields(a);
                    //if (formDef.FormType != FormType.ContainerRowFormGenerated || !cf.IsNullOrEmpty())
                    {
                        // column Heading already has the Caption
                        string heading = "";
                        if (this.width == null && (containerFormField == null || containerFormField.rowFormDef == null))
                            heading = "<span>AppPressDisplayName</span>";
                        if (!heading.IsNullOrEmpty() || !cf.IsNullOrEmpty())
                        {
                            skin += "<caption class='tbl_heading'>" + heading + "<span class='AppPressFormContainerFields'>";
                            skin += cf;
                            skin += "</span></caption>";
                        }
                    }

                }

                var generateHeader = OriginalType != (int)FormDefFieldType.MultiFileUpload;

                if (rowFormDef.Pivot)
                {
                    skin += "<tbody>";
                    foreach (var columnField in rowFormDef.formFields)
                    {
                        if (columnField.containerFormField != null || columnField.Type == FormDefFieldType.ForeignKey || columnField.Hidden)
                            continue;

                        if (columnField.Type == FormDefFieldType.MergedForm || columnField.Type == FormDefFieldType.UserControlScalar)
                        {
                            skin += @"<!--|AppPress." + this.fieldName + @".HeaderBegin|--><!--|AppPress." + columnField.fieldName + @".Begin|--><tr id='AppPressColumnId'><td style=""AppPressLabelStyle"">AppPressDisplayName</td><!--|AppPress." + this.fieldName + @".RowBegin|--><td><!--|AppPress." + columnField.fieldName + @".Begin|--><span id='AppPressContainerId' style=""AppPressControlStyle""><!--|AppPress.FieldContent|--></span><!--|AppPress." + columnField.fieldName + @".End|--></td><!--|AppPress." + this.fieldName + @".RowEnd|--></tr><!--|AppPress." + columnField.fieldName + @".End|--><!--|AppPress." + this.fieldName + @".HeaderEnd|-->";
                        }
                        else
                        {
                            skin += "<tr id='AppPressColumnId'>";
                            skin += "<!--|AppPress." + this.fieldName + ".HeaderBegin|-->";
                            skin += "<td style=\"AppPressLabelStyle\"><!--|AppPress." + columnField.fieldName + ".Begin|-->AppPressDisplayName<!--|AppPress." + columnField.fieldName + ".End|--></td>";
                            skin += "<!--|AppPress." + this.fieldName + ".HeaderEnd|-->";
                            skin += "<!--|AppPress." + this.fieldName + ".RowBegin|-->";
                            skin += "<td><!--|AppPress." + columnField.fieldName + ".Begin|--><span id='AppPressContainerId' style=\"AppPressControlStyle\">" + columnField.GenerateSkin(a, false, false, true) + "</span><!--|AppPress." + columnField.fieldName + ".End|--></td>";
                            skin += "<!--|AppPress." + this.fieldName + ".RowEnd|-->";
                            skin += "</tr>";
                        }

                    }
                    skin += "</tbody></table>";
                }
                else
                {
                    if (generateHeader)
                    {
                        skin += "<!--|AppPress." + this.fieldName + ".HeaderBegin|-->";
                        if (a.skinType == SkinType.FO)
                            skin += "<fo:table-header><fo:table-row>";
                        else
                            skin += "<thead><tr>";
                        foreach (var columnField in rowFormDef.formFields)
                        {
                            if (columnField.containerFormField != null || columnField.Type == FormDefFieldType.ForeignKey || columnField.Hidden)
                                continue;

                            if (a.skinType == SkinType.FO)
                            {
                                skin += "<!--|AppPress." + columnField.fieldName + ".Begin|--><fo:table-cell><fo:block font-size='8pt' font-family='sans-serif' font-weight='bold' space-after.optimum='20pt' text-align='start' padding =\"4pt\">" + columnField.GetDisplayName() + "</fo:block></fo:table-cell>";
                                skin += @"<!--|AppPress." + columnField.fieldName + @".End|-->";
                            }
                            else
                            {
                                skin += "<!--|AppPress." + columnField.fieldName + ".Begin|-->";
                                var lableStyle = columnField.LabelStyle;
                                var cw = columnField.width;
                                if (cw == null && columnField.Type == FormDefFieldType.FileUpload)
                                    cw = "150px";
                                if (cw != null)
                                {
                                    int wi;
                                    if (int.TryParse(cw, out wi))
                                        cw += "px";
                                    lableStyle += "min-width:" + cw + ";" + "width:" + cw + ";" + "max-width:" + cw + ";";
                                }
                                skin += "<th id='AppPressColumnId'";
                                if (!lableStyle.IsNullOrEmpty())
                                    skin += " style='" + lableStyle + "'";
                                if (columnField.fieldName == "SelectRow")
                                    skin += " class='SelectRow AppPressColumnHeader'";
                                else
                                    skin += " class='AppPressColumnHeader'";
                                skin += ">";
                                if (columnField.Type == FormDefFieldType.Checkbox)
                                    if (columnField.AllowMultiSelect && columnField.fieldName == "SelectRow")
                                    {
                                        skin += "<input type='checkbox' id='SelectAll_AppPressId' rel='SelectAll' onclick='CheckUncheckAll(this,\"AppPressId\")'/>";
                                    }
                                //if (columnField.Type != FormDefFieldType.Button)
                                skin += @"AppPressDisplayName";
                                if (columnField.Sortable)
                                {
                                    var sortableField = formDef.GetFormField("Sortable" + columnField.fieldName);
                                    skin += "<!--|AppPress." + sortableField.fieldName + ".Begin|-->";
                                    skin += "<div id='AppPressContainerId' class='pull-right'>" + sortableField.GenerateSkin(a, false, false, true, null) + "</div>";
                                    skin += "<!--|AppPress." + sortableField.fieldName + ".End|-->";
                                }
                                if (columnField.RowFilter)
                                {
                                    var filterField = formDef.GetFormField("RowFilter" + columnField.fieldName);
                                    skin += "<!--|AppPress." + filterField.fieldName + ".Begin|-->";
                                    skin += "<a href='#' id='FilterButton_AppPressId' onclick=\"AppPress_GridFilterClick('AppPressContainerId','AppPressId',false)\" class='pull-right'><i class='fa fa-filter'></i></a>";
                                    var fieldSkin = filterField.GenerateSkin(a, false, false, true, null).Replace("AppPressOnClick", "");
                                    skin += "<div id='AppPressContainerId' class='AppPressGridFilter' style='display:none;position:absolute'>" + fieldSkin + "<br/>";
                                    skin += "<button class='btn btn-primary btn-xs'  name='AppPress_RowFilter_Cancel' onclick=\"AppPress_GridFilterClick('AppPressContainerId','AppPressId',true)\">Cancel</button>";
                                    skin += "<button id='AppPressId' name='AppPress_RowFilter_Apply' class='btn btn-primary btn-xs pull-right' onclick='AppPressOnChange;AppPress_GridFilterClick(\"AppPressContainerId\",\"AppPressId\",false)'>Apply</button>";
                                    skin += "</div><!--|AppPress." + filterField.fieldName + ".End|-->";
                                }
                                skin += "</th>";
                                skin += "<!--|AppPress." + columnField.fieldName + ".End|-->";
                            }
                        }
                        if (a.skinType == SkinType.FO)
                            skin += "</fo:table-row></fo:table-header>";
                        else
                            skin += "</tr></thead>";
                        skin += "<!--|AppPress." + this.fieldName + ".HeaderEnd|-->";
                    }
                    var columnSkin = string.Empty;
                    foreach (var columnField in rowFormDef.formFields)
                    {
                        if (columnField.containerFormField != null || columnField.Type == FormDefFieldType.ForeignKey || columnField.Hidden)
                            continue;
                        if (a.skinType == SkinType.FO && columnField.Type == FormDefFieldType.Checkbox && columnField.fieldName == "SelectRow")
                            continue;
                        var displayName = "AppPressDisplayName";
                        if (a.skinType == SkinType.FO)
                        {
                            columnSkin += "<fo:table-cell><fo:block font-size='8pt' font-family='sans-serif' space-after.optimum='20pt' text-align='start' padding =\"4pt\">";
                            columnSkin += "<!--|AppPress." + columnField.fieldName + ".Begin|-->";
                            columnSkin += columnField.GenerateSkin(a, false, false, true, columnField.GetDisplayName());
                            columnSkin += "<!--|AppPress." + columnField.fieldName + ".End|-->";
                            columnSkin += "</fo:block></fo:table-cell>";
                        }
                        else
                        {
                            columnSkin += "<!--|AppPress." + columnField.fieldName + ".Begin|-->";
                            columnSkin += "<td id='AppPressContainerId'";
                            var lableStyle = "";
                            if (columnField.width != null)
                            {
                                int wi;
                                var w = columnField.width;
                                if (int.TryParse(w, out wi))
                                    w += "px";
                                lableStyle += "min-width:" + w + ";" + "width:" + w + ";" + "max-width:" + w + ";";
                            }
                            if (!lableStyle.IsNullOrEmpty())
                                columnSkin += " style='" + lableStyle + "'";
                            columnSkin += ">\n";
                            columnSkin += columnField.GenerateSkin(a, false, false, true, displayName);
                            columnSkin += "</td><!--|AppPress." + columnField.fieldName + ".End|-->\n";
                        }
                    }

                    if (a.skinType == SkinType.FO)
                    {
                        skin += "<fo:table-body>";
                        skin += "<!--|AppPress." + fieldName + ".RowBegin|-->";
                        skin += "<fo:table-row>";
                    }
                    else
                    {
                        skin += "<tbody";
                        if (height != null)
                            skin += " style='height:" + height + "' ";
                        skin += ">\n<!--|AppPress." + fieldName + ".RowBegin|-->";
                        skin += "<tr>";
                    }
                    skin += columnSkin;
                    if (a.skinType == SkinType.FO)
                    {
                        skin += "</fo:table-row>";
                        skin += "<!--|AppPress." + fieldName + ".RowEnd|-->";
                        skin += "</fo:table-body>";
                        skin += "</fo:table>";
                    }
                    else
                    {
                        skin += "</tr>";
                        skin += "<!--|AppPress." + fieldName + ".RowEnd|-->\n";
                        skin += "</tbody>";
                        skin += "</table>";
                    }
                }
            }
            return skin;
        }

        private string GetNativeWidth()
        {
            if (Type == FormDefFieldType.Checkbox && fieldName == "SelectRow")
                return "23px";
            if (Type == FormDefFieldType.DateTime && Style == FormDefFieldStyle.Time)
                return "150px";
            if (Type == FormDefFieldType.DateTime && Style != FormDefFieldStyle.Time)
                return "100px";
            if (Type == FormDefFieldType.Number)
                return (70 + decimals * 10) + "px";
            return null;
        }

        internal string GenerateFormContainerFields(AppPress a)
        {
            // generate fields attached to formContainer
            var skin = "";
            foreach (var fField in formDef.formFields)
            {
                if (fField.containerFormField != null && fField.containerFormField.id == this.id && !fField.RowFilter && !fField.Sortable)
                {
                    skin += "\n<!--|AppPress." + fField.fieldName + ".Begin|-->";
                    if (a.skinType != SkinType.FO)
                        skin += "<span id='AppPressContainerId'>";
                    var displayName = "AppPressDisplayName";
                    if (a.skinType != SkinType.FO)
                    {
                        if (fField.Type != FormDefFieldType.Button)
                        {
                            skin += "<span class='FieldTitle'>" + displayName + "</span>";
                        }
                    }
                    skin += fField.GenerateSkin(a, false, true, false, displayName);
                    if (a.skinType != SkinType.FO)
                        skin += "</span>";
                    skin += "<!--|AppPress." + fField.fieldName + ".End|-->\n";
                }
            }
            return skin;
        }


        internal string GetDisplayName(bool addRequired = true)
        {
            if (fieldName == "SelectRow")
                return "";
            string dName = Label != null ? Label : AppPress.InsertSpacesBetweenCaps(fieldName);
            if (addRequired)
                if (!Static && IsDateRange == 0)
                    if (Required) // add a trailing * if the field is required
                        dName += "*";

            return dName;
        }

        public string GetParameter(string parameterName)
        {
            return Parameters.Find(t => t.Name.Equals(parameterName, StringComparison.OrdinalIgnoreCase)).Value;
        }
        public string TryGetParameter(string parameterName)
        {
            if (Parameters == null)
                return null;
            var parameter = Parameters.Find(t => t.Name.Equals(parameterName, StringComparison.OrdinalIgnoreCase));
            if (parameter == null)
                return null;
            return parameter.Value;
        }

        internal string GetFieldValueFromDB(AppPress a, System.Data.IDataReader dr, int ford, FieldValue fieldValue)
        {
            switch (Type)
            {
                case FormDefFieldType.Text:
                case FormDefFieldType.HTML:
                case FormDefFieldType.TextArea:
                case FormDefFieldType.Password:
                case FormDefFieldType.Button:
                    if (dr.IsDBNull(ford))
                        return null;
                    var s = dr[ford].ToString();
                    if (this.EncryptionType.HasValue)
                    {
                        try
                        {
                            if (s == "N/A")
                                return s;
                            if (this.Type == FormDefFieldType.Number)
                                decimal.Parse(s);
                        }
                        catch (Exception)
                        {
                            if (AppPress.Settings.developer)
                            {
                                if (this.Type == FormDefFieldType.Number)
                                    return "99999.99";
                                // ignore error and Generic string
                                var s1 = s;
                                s = "";
                                for (int i = 0; i < s1.Length; ++i)
                                    s += "*";
                                if (fieldValue != null)
                                    fieldValue.DoNotSaveInDB = true;// such fields should not be saved
                            }
                            else
                                throw;
                        }
                    }
                    return s;
                case FormDefFieldType.Number:
                    if (this.EncryptionType != null)
                        goto case FormDefFieldType.Text;
                    return dr.IsDBNull(ford) ? null :
                        (decimals == 0 ? dr[ford].ToString() : dr.GetDecimal(ford).ToString());
                case FormDefFieldType.Checkbox:
                    return dr.IsDBNull(ford) ? "0" : Convert.ToInt32(dr[ford]).ToString();
                case FormDefFieldType.Pickone:
                case FormDefFieldType.FileUpload:
                case FormDefFieldType.ForeignKey:
                    return dr.IsDBNull(ford) ? null : dr[ford].ToString();
                case FormDefFieldType.DateTime:
                    return dr.IsDBNull(ford) ? null : ((DateTime)dr.GetDateTime(ford)).ToString(DAOBasic.DBDateTimeFormat);
                case FormDefFieldType.PickMultiple:
                    return null;

            }
            throw new Exception("GetFieldValueFromDB does not support Field Type:" + Type);
        }

        public long? GetContainerRowForm(AppPress a)
        {
            var pServerFunction = a.serverFunction;
            var pFieldValue = a.fieldValue;
            try
            {
                foreach (var domainFunction in GetFieldFunctions(FunctionType.Domain))
                {
                    a.serverFunction = domainFunction;
                    var method = Util.GetMethod(a, domainFunction.FunctionName, new Type[] { AppPress.Settings == null ? typeof(AppPress) : AppPress.Settings.ApplicationAppPress, typeof(FormCallType) });
                    if (method == null)
                        return null;

                    a.fieldValue = new FieldValue();
                    a.fieldValue.formField = this;
                    var obj = Util.InvokeMethod(a, method, new object[] { a, FormCallType.GetContainerRowForm });
                    if (obj == null)
                        return null;
                    return (long?)Convert.ChangeType(obj, typeof(long));


                }
                return null;
            }
            finally
            {
                a.fieldValue = pFieldValue;
                a.serverFunction = pServerFunction;
            }
        }
        public FormDef GetContainerRowFormDef(AppPress a)
        {
            if (Type != FormDefFieldType.FormContainerGrid && Type != FormDefFieldType.FormContainerDynamic)
                return null;
            if (rowFormDef != null)
                return rowFormDef;
            var formDefId = GetContainerRowForm(a);
            if (formDefId == null)
            {
                if (OriginalType == (int)FormDefFieldType.FormContainerGrid)
                {
                    //return AppPress.FindFormDef(fieldName + "Row");
                    var f = AppPress.formDefs.FindAll(t => t.ContainerFormField != null && t.ContainerFormField.id == id && t.formName == fieldName + "Row");
                    if (f.Count > 1)
                        throw new Exception(GetDescription() + " Found 2 forms in GetContainerRowForm");
                    if (f.Count == 0)
                        throw new Exception(GetDescription() + " Could not find GetContainerRowForm");
                    return f[0];
                }
                return null;
            }
            return AppPress.FindFormDef((long)formDefId);
        }
        internal string GetContainerColumnName(AppPress a)
        {
            var pServerFunction = a.serverFunction;
            try
            {
                foreach (var domainFunction in GetFieldFunctions(FunctionType.Domain))
                {
                    a.serverFunction = domainFunction;
                    var method = Util.GetMethod(a, domainFunction.FunctionName, new Type[] { typeof(AppPress), typeof(FormCallType) });
                    if (method != null)
                        return (string)Util.InvokeMethod(a, method, new object[] { a, FormCallType.GetContainerColumnName });

                }
                return null;
            }
            finally
            {
                a.serverFunction = pServerFunction;
            }
        }

        internal static string RemoveBetweenMarkers(AppPress a, string skin, int skinStartIndex, string beginMarker, string endMarker, out int startIndex, out string betweenStr, string fileName)
        {
            if (a.skinType == SkinType.DOCX)
            {
                beginMarker = beginMarker.Replace("<", "&lt;").Replace(">", "&gt;");
                endMarker = endMarker.Replace("<", "&lt;").Replace(">", "&gt;");
            }
            startIndex = skin.IndexOf(beginMarker, skinStartIndex);
            if (startIndex == -1)
            {
                betweenStr = null;
                return skin;
            }
            int eCount = 1;
            var si = startIndex;
            // find nested endMarker
            // <Begin>
            //   <Begin>
            //   <End>
            //  <End>
            while (true)
            {
                var sIndex = skin.IndexOf(beginMarker, si + beginMarker.Length);
                var endIndex = skin.IndexOf(endMarker, si);
                if (endIndex == -1)
                    throw new AppPressException("Could not find:" + HttpUtility.HtmlEncode(endMarker) + " in Skin file: " + fileName);
                if (sIndex != -1 && sIndex < endIndex)
                {
                    eCount++;
                    si = sIndex;
                    continue;
                }
                eCount--;
                si = endIndex + endMarker.Length;
                if (eCount == 0)
                {
                    betweenStr = skin.Substring(startIndex, endIndex + endMarker.Length - startIndex);
                    return skin.Substring(0, startIndex) + skin.Substring(endIndex + endMarker.Length);
                }
            }
        }
        internal string RemoveBetweenMarkers(AppPress a, string skin, int skinStartIndex, string marker, out int startIndex, out string betweenStr, out bool outer, string formName)
        {

            outer = true;
            var beginMarker = @"<!--|AppPress." + fieldName + "." + marker + @"Begin|-->";
            var endMarker = @"<!--|AppPress." + fieldName + "." + marker + @"End|-->";
            var s = RemoveBetweenMarkers(a, skin, skinStartIndex, beginMarker, endMarker, out startIndex, out betweenStr, formDef.formName);

            if (betweenStr != null)
                return s;

            outer = false;
            beginMarker = @"<!--|" + formName + fieldName + marker + @"Begin|-->";
            endMarker = @"<!--|" + formName + fieldName + marker + @"End|-->";
            return RemoveBetweenMarkers(a, skin, skinStartIndex, beginMarker, endMarker, out startIndex, out betweenStr, formDef.formName);
        }
        internal string RemoveBetweenMarkers(AppPress a, string skin, int skinStartIndex, string marker, out int startIndex, out string betweenStr, out bool outer)
        {
            var formName = (marker == "Row" || marker == "Fields") ? "" : formDef.formName;
            return RemoveBetweenMarkers(a, skin, skinStartIndex, marker, out startIndex, out betweenStr, out outer, formName);
        }
        internal string RemoveBetweenMarkers(AppPress a, string ts, string marker, out bool outer, string replaceStr = null)
        {
            int startIndex;
            string betweenStr;
            while (true)
            {
                var s = RemoveBetweenMarkers(a, ts, 0, marker, out startIndex, out betweenStr, out outer);
                if (replaceStr != null && startIndex != -1)
                    s = s.Substring(0, startIndex) + replaceStr + s.Substring(startIndex);
                if (s.Length == ts.Length)
                    return ts;
                ts = s;

            }
        }


        internal static string ExtractBetweenMarkers(AppPress a, string skin, int skinStartIndex, string beginMarker, string endMarker, out string unique, out string betweenStr, string fileName)
        {
            if (a.skinType == SkinType.DOCX)
            {
                beginMarker = beginMarker.Replace("<", "&lt;").Replace(">", "&gt;");
                endMarker = endMarker.Replace("<", "&lt;").Replace(">", "&gt;");
            }
            int startIndex = skin.IndexOf(beginMarker, skinStartIndex);
            if (startIndex == -1)
            {
                unique = null;
                betweenStr = null;
                return skin;
            }
            int eCount = 1;
            var si = startIndex;
            // find nested endMarker
            // <Begin>
            //   <Begin>
            //   <End>
            //  <End>
            while (true)
            {
                var sIndex = skin.IndexOf(beginMarker, si + beginMarker.Length);
                var endIndex = skin.IndexOf(endMarker, si);
                if (endIndex == -1)
                    throw new AppPressException("Could not find:" + HttpUtility.HtmlEncode(endMarker) + " in Skin file: " + fileName);
                if (sIndex != -1 && sIndex < endIndex)
                {
                    eCount++;
                    si = sIndex;
                    continue;
                }
                eCount--;
                si = endIndex + endMarker.Length;
                if (eCount == 0)
                {
                    betweenStr = skin.Substring(startIndex + beginMarker.Length, endIndex - startIndex - beginMarker.Length);
                    unique = AppPress.GetUniqueId().ToString() + "jhsbhdbjhsdbjdbjhdbjbjd";
                    return skin.Substring(0, startIndex) + unique + skin.Substring(endIndex + endMarker.Length);
                }
            }
        }
        internal string ExtractBetweenMarkers(AppPress a, string skin, int skinStartIndex, string marker, out string unique, out string betweenStr, out bool outer, string formName, out string beginMarker, out string endMarker)
        {

            outer = true;
            beginMarker = @"<!--|AppPress." + fieldName + "." + marker + @"Begin|-->";
            endMarker = @"<!--|AppPress." + fieldName + "." + marker + @"End|-->";
            var s = ExtractBetweenMarkers(a, skin, skinStartIndex, beginMarker, endMarker, out unique, out betweenStr, formDef.formName);

            if (betweenStr != null)
                return s;

            outer = false;
            beginMarker = @"<!--|" + formName + fieldName + marker + @"Begin|-->";
            endMarker = @"<!--|" + formName + fieldName + marker + @"End|-->";
            return ExtractBetweenMarkers(a, skin, skinStartIndex, beginMarker, endMarker, out unique, out betweenStr, formDef.formName);
        }
        internal string ExtractBetweenMarkers(AppPress a, string skin, int skinStartIndex, string marker, out string unique, out string betweenStr, out bool outer, out string beginMarker, out string endMarker)
        {
            var formName = (marker == "Row" || marker == "Fields") ? "" : formDef.formName;
            return ExtractBetweenMarkers(a, skin, skinStartIndex, marker, out unique, out betweenStr, out outer, formName, out beginMarker, out endMarker);
        }
        internal FormContainerStyle GetFormContainerStyle()
        {
            if (OriginalType == (int)FormDefFieldType.EmbeddedForm)
                return FormContainerStyle.InLine;
            if (OriginalType != (int)FormDefFieldType.None || rowFormDef != null)
                return FormContainerStyle.Grid;
            if (FieldFunctions == null)
                return FormContainerStyle.InLine;
            var domainFunction = FieldFunctions.Find(t => t.ServerFunctionType == FunctionType.Domain);
            if (domainFunction == null)
                return FormContainerStyle.InLine;
            string style = domainFunction.TryGetFunctionParameterValue("Style");
            if (style == null)
                return FormContainerStyle.Grid;
            return (FormContainerStyle)int.Parse(style);

        }

        internal FieldValue NewFieldValue(FormDef containerFormDef)
        {

            var className = GetClassName(containerFormDef);
            var fieldValue1 = (FieldValue)Util.CreateInstance(className);
            if (fieldValue1 == null)
                switch (Type)
                {
                    case FormDefFieldType.FormContainerDynamic:
                        fieldValue1 = new FormContainerFieldValue();
                        break;
                    case FormDefFieldType.Checkbox:
                    case FormDefFieldType.Pickone:
                    case FormDefFieldType.PickMultiple:
                        fieldValue1 = new PickFieldValue();
                        break;
                    case FormDefFieldType.DateTime:
                        fieldValue1 = new DateTimeFieldValue();
                        break;
                    case FormDefFieldType.FileUpload:
                        fieldValue1 = new FileUploadFieldValue();
                        break;
                    case FormDefFieldType.Number:
                        fieldValue1 = new NumberFieldValue();
                        break;
                    case FormDefFieldType.Button:
                        fieldValue1 = new ButtonFieldValue();
                        break;
                    default:
                        fieldValue1 = new FieldValue();
                        break;
                }
            fieldValue1.formField = this;
            fieldValue1.fieldDefId = id;
            return fieldValue1;
        }

        internal string GetClassName(FormDef f = null)
        {
            if (f == null || formDef.FormType == FormType.MergedForm || formDef.FormType == FormType.PluginForm)
                f = formDef;
            var calcClassName = f.formName + "Class+";
            //if (OriginalType == FormDefFieldType.UserControlScalar)
            //    calcClassName += AppPress.FindFormDef(ExtensionFormName).formFields[0].fieldName;
            //else
            calcClassName += fieldName;
            calcClassName += "FieldClass";
            while (f.ContainerFormField != null)
            {
                calcClassName = f.ContainerFormField.formDef.formName + "Class+" + calcClassName;
                f = f.ContainerFormField.formDef;
            }
            return calcClassName;
        }
        internal static string GetClassName(FormDef formDef, long fieldDefId)
        {
            var formField = formDef.GetFormField(fieldDefId);
            var calcClassName = formDef.formName + "Class+";
            //if (OriginalType == FormDefFieldType.UserControlScalar)
            //    calcClassName += AppPress.FindFormDef(ExtensionFormName).formFields[0].fieldName;
            //else
            calcClassName += formField.fieldName;
            calcClassName += "FieldClass";
            var f = formDef;
            while (f.ContainerFormField != null)
            {
                calcClassName = f.ContainerFormField.formDef.formName + "Class+" + calcClassName;
                f = f.ContainerFormField.formDef;
            }
            return calcClassName;
        }

        internal string GetDescription()
        {
            // for Error Display

            if (formDef.FormType == FormType.ContainerRowFormGenerated)
            {
                var fn = formDef.formName;
                if (fn.EndsWith("Row"))
                    fn = fn.Substring(0, fn.Length - 3);
                if (fn.EndsWith("Popup"))
                    fn = fn.Substring(0, fn.Length - 5);
                return "FormContainerGrid: " + fn + " Field: " + fieldName;
            }
            return "Form: " + formDef.formName + " Field: " + fieldName;
        }

        internal string CheckSortable()
        {
            if (!Static)
                return "Field should be static for Sortable Property";
            switch (Type)
            {
                case FormDefFieldType.Text:
                case FormDefFieldType.TextArea:
                case FormDefFieldType.Number:
                case FormDefFieldType.Checkbox:
                case FormDefFieldType.Pickone:
                case FormDefFieldType.DateTime:
                    return null;
                default:
                    return "Sortable Property cannot be set for Field of Type: " + Type;
            }
        }

        internal bool IsSortingControl()
        {
            return Sortable && FieldFunctions.Count() == 1 && FieldFunctions[0].FunctionName == "SortContainer";
        }
    }
}

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Reflection;
using System.Configuration;
using System.Net.Configuration;
using MySql.Data.MySqlClient;

namespace AppPressFramework
{
    internal class AppPressLogic
    {

        internal static void OnSessionExpiredException(AppPress a)
        {
            if (a.PageURL != null && a.PageURL["s"] != null)
            {
                a.AlertMessage("Your Session has been expired."); //Do not allow login from secure url.
            }
            else
            {
                throw new AppPressException("Session Expired.");
            }
        }

        internal static void ActionFunctionCall(AppPress a)
        {
            try
            {
                var functionName = a.GetFunctionParameterValue("FunctionName");
                var pServerFunction = a.serverFunction;
                try
                {
                    a.serverFunction = new ServerFunction(a, FunctionType.OnClick, functionName);
                    if (a.serverFunction.method == null)
                        throw new Exception("Could not find Function:" + functionName);
                    Util.InvokeMethod(a, a.serverFunction.method, new object[] { a });

                }
                catch (Exception ex)
                {
                    throw ex.InnerException != null ? ex.InnerException : ex;
                }
                finally
                {
                    a.serverFunction = pServerFunction;
                }
            }
            catch
            {
                throw;
            }
        }

        internal static void GenericCloseWindow(AppPress a)
        {
            a.ClosePopup();
        }

        internal static void OpenForm(AppPress a)
        {
            var formField = a.fieldValue.formField;
            var formName = a.TryGetFunctionParameterValue("FormName");
            var formDefId = a.TryGetFunctionParameterValue("FormDefId");
            var PopupTitle = a.TryGetFunctionParameterValue("PopupTitle");
            if (formName == null && formDefId == null)
                throw new Exception("Internal Error: in open Form FormName and formDefId are null");
            var formDef = formName == null ? AppPress.FindFormDef(long.Parse(formDefId) * 10 - AppPress.LocalInstanceId) : AppPress.FindFormDef(formName);
            bool popup = a.TryGetFunctionParameterValue("Popup") != null;
            var formId = a.TryGetFunctionParameterValue("FormId");
            OpenForm(a, formDef, formId, popup, PopupTitle);

        }

        internal static void OpenForm(AppPress a, FormDef destFormDef, string formId, bool popup, string PopupTitle)
        {

            if (destFormDef == null)
                throw new Exception("Could not find FormDef:" + destFormDef.formName);
            if (popup)
            {
                a.CallReason = CallReasonType.PageLoad;
                if (formId == null)
                    formId = AppPress.GetUniqueId().ToString();
                a.pageStackCount++;
                try
                {
                    var formData = a.LoadFormData(destFormDef.id, formId, a.fieldValue, null);
                    if (formData.IsNew)
                        foreach (var fieldValue in formData.fieldValues)
                            if (fieldValue.formField.Type == FormDefFieldType.ForeignKey)
                                fieldValue.Value = a.fieldValue.FormData.id;
                    a.CalcFormDatas(formData, null, true);
                    formData = a.formDatas.Find(t => t.formDefId == formData.formDefId && t.id == formData.id);
                    if (formData == null)
                        throw new Exception("Internal Error: OpenForm");
                    var popupParams = new PopupParams();
                    popupParams.title = PopupTitle;
                    a.appPressResponse.Add(AppPressResponse.Popup(a, formData, popupParams));
                }
                catch
                {
                    // if error remove all forms in pageStackCount
                    a.formDatas.RemoveAll(t => t.pageStackIndex == a.pageStackCount);
                    a.pageStackCount--;
                    throw;
                }
            }
            else
                a.appPressResponse.Add(AppPressResponse.Redirect(a, destFormDef.id, formId, null));
        }

        internal static List<FormData> GetSelectedFormDatas(AppPress a, FieldValue containerFieldValue)
        {
            //string selectionFieldName = a.TryGetFunctionParameterValue("SelectionFieldName");            
            string selectionFieldName = "SelectRow";
            var selectedFormDatas = new List<FormData>();
            foreach (var formData1 in a.formDatas)
            {
                if (formData1.IsDeleted)
                    continue;
                if (formData1.containerFieldValue != containerFieldValue)
                    continue;
                var selectionFiedValue = formData1.GetFieldValue(selectionFieldName);
                if (selectionFiedValue == null || selectionFiedValue.formField.Type != FormDefFieldType.Checkbox)
                    throw new Exception("Could not find Form field with field name:" + selectionFieldName + " of Type Checkbox in Form Def:" + formData1.formDefId);
                if (selectionFiedValue.Value != "1")
                    continue;
                selectedFormDatas.Add(formData1);
            }
            return selectedFormDatas;
        }
        internal static List<FormData> GetSelectedFormDatas(AppPress a)
        {
            var selectedFormDatas = new List<FormData>();
            var containerFieldValue = a.fieldValue.FormData.GetFieldValue(a.fieldValue.formField.containerFormField.fieldName);
            return GetSelectedFormDatas(a, containerFieldValue);
        }
        internal static FormData GetFirstSelectedFormData(AppPress a)
        {
            var selectedFormDatas = GetSelectedFormDatas(a);
            if (selectedFormDatas.Count() != 1)
            {
                throw new AppPressException("Please select any one row by clicking check-box on the left column in required row to execute this step.");
            }
            return selectedFormDatas.First();
        }


        internal static void ModifySelectedSubForm(AppPress a)
        {
            var formData = GetFirstSelectedFormData(a);
            var formDefIdStr = a.TryGetFunctionParameterValue("FormDefId");
            var formDefId = formDefIdStr == null ? formData.formDefId : long.Parse(formDefIdStr + AppPress.LocalInstanceId);
            AppPressResponse clientAction;
            var destFormDef = AppPress.FindFormDef(formDefId);
            if (a.TryGetFunctionParameterValue("Popup") != null)
            {
                var popupParams = new PopupParams();
                var popupWidth = a.TryGetFunctionParameterValue("PopupWidth");
                if (popupWidth != null)
                    popupParams.PopupWidth = int.Parse(popupWidth);
                var popupHeight = a.TryGetFunctionParameterValue("PopupHeight");
                if (popupHeight != null)
                    popupParams.PopupHeight = int.Parse(popupHeight);
                var popupPosition = a.TryGetFunctionParameterValue("PopupPosition");
                if (popupPosition != null)
                    popupParams.PopupPosition = popupPosition;
                popupParams.title = a.TryGetFunctionParameterValue("PopupTitle");
                clientAction = AppPressResponse.Popup(a, destFormDef, formData.id, popupParams);
                a.AppPopup(clientAction);
            }
            else
            {
                var redirectParams = new RedirectParams();
                redirectParams.urlParams = a.TryGetFunctionParameterValue("URLParams");
                clientAction = AppPressResponse.Redirect(a, formDefId, formData.id, redirectParams);
                a.appPressResponse.Add(clientAction);
            }
        }

        internal static void ModifySelectedForm(AppPress a, long formDefId)
        {
            var formData = GetFirstSelectedFormData(a);
            a.pageStackCount++;
            formData = a.LoadFormData(formDefId, formData.id, a.fieldValue, null);
            a.CalcFormDatas(formData, null, true);
            var clientAction = AppPressResponse.Popup(a, formData, null);
            a.AppPopup(clientAction);
        }

        internal static void ViewSelectedSubForm(AppPress a)
        {
            ModifySelectedSubForm(a);
        }
        internal static void DeleteThisForm(AppPress a)
        {
            a.fieldValue.FormData.IsDeleted = true;
            a.appPressResponse.Add(AppPressResponse.RefreshField(a, a.fieldValue.FormData.containerFieldValue, false));
            a.appPressResponse.Add(AppPressResponse.SetPageDirty(true));
            var fieldValue = a.fieldValue.FormData.fieldValues.Find(t => t.formField.Hidden && t.formField.OriginalType == (int)FormDefFieldType.MultiFileUpload);
            if (fieldValue.formField.AutoUpload && !a.fieldValue.FormData.IsNew)
            {
                a.PromptClient("Do you really want to delete this?");
                a.fieldValue.FormData.Delete(a);
            }
        }
        internal static void GenericDeleteAllForms(AppPress a, FieldValue containerFieldValue)
        {
            var deletedFormDatas = a.formDatas.FindAll(t => t.containerFieldValue == containerFieldValue && !t.IsDeleted);
            foreach (FormData formData in deletedFormDatas)
            {
                formData.IsDeleted = true;
            }
        }
        internal static void GenericDeleteAllForms(AppPress a)
        {
            FieldValue containerFieldValue = a.fieldValue.FormData.GetFieldValue(a.fieldValue.formField.containerFormField.fieldName);
            GenericDeleteAllForms(a, containerFieldValue);
        }

        internal static void MoveRowsInFormContainer(AppPress a)
        {
            var formField = a.fieldValue.formField.containerFormField;
            var formDef = formField.formDef;
            string Direction = a.GetFunctionParameterValue("Direction");
            string selectionFieldName = "SelectRow";
            var containerFieldValue = a.fieldValue.FormData.GetFieldValue(a.fieldValue.formField.containerFormField.fieldName);
            containerFieldValue.ReArranged = true;

            var containedFormDatas = a.formDatas.FindAll(t => t.containerFieldValue == containerFieldValue);
            var moveFormDatas = containedFormDatas.FindAll(t => t.GetFieldValue(selectionFieldName).Value == "1");
            if (moveFormDatas.Count == 0)
            {
                throw new AppPressException("At least one row should be selected to Move.");
            }

            if (Direction != "1") // Down
            {
                moveFormDatas.Reverse();
                containedFormDatas.Reverse();
            }
            int destIndex = containedFormDatas.FindIndex(t => t == moveFormDatas.First()) - 1;
            if (destIndex < 0)
                destIndex = 0;
            bool moved = false;
            for (int i = 0; i < moveFormDatas.Count(); ++i)
            {
                var moveFormData = moveFormDatas[i];
                int ci = containedFormDatas.FindIndex(t => t == moveFormData);
                while (ci > destIndex && destIndex != ci)
                {
                    var swapFormData = containedFormDatas[ci - 1];
                    SwapFormData(containedFormDatas, moveFormData, swapFormData);
                    SwapFormData(a.formDatas, moveFormData, swapFormData);
                    ci--;
                    moved = true;
                }
                destIndex++;
            }
            if (moved)
            {
                a.appPressResponse.Add(AppPressResponse.RefreshField(a, a.fieldValue.FormData.GetFieldValue(a.fieldValue.formField.containerFormField.fieldName), false));
                a.appPressResponse.Add(AppPressResponse.SetPageDirty(true));
            }
        }

        private static void SwapFormData(List<FormData> containedFormDatas, FormData moveFormData, FormData nextFormData)
        {
            int mIndex = containedFormDatas.FindIndex(t => t == moveFormData);
            int nIndex = containedFormDatas.FindIndex(t => t == nextFormData);
            containedFormDatas[mIndex] = nextFormData;
            containedFormDatas[nIndex] = moveFormData;
        }
        internal static void DeleteSelectedSubForms(AppPress a)
        {
            if (a.fieldValue.formField.GetMethod(a, "OnClick") != null)
                return;
            string selectionFieldName = a.TryGetFunctionParameterValue("SelectionFieldName");

            bool DeleteFromDB = a.TryGetFunctionParameterValue("DeleteFromDB") != null;
            DeleteSelectedSubForms(a, selectionFieldName, DeleteFromDB);
        }
        internal static void DeleteSelectedSubForms(AppPress a, string selectionFieldName, bool DeleteFromDB)
        {
            var formField = a.fieldValue.formField.containerFormField;
            var formDef = formField.formDef;
            if (selectionFieldName == null)
                selectionFieldName = "SelectRow";
            var containerFieldValue = a.fieldValue.FormData.GetFieldValue(a.fieldValue.formField.containerFormField.fieldName);
            var deletedFormDatas = a.formDatas.FindAll(t => t.containerFieldValue == containerFieldValue && !t.IsDeleted &&
                 t.GetFieldValue(selectionFieldName).Value == "1");
            if (deletedFormDatas.Count == 0)
                throw new AppPressException("At least one row should be selected for Delete.");
            a.PromptClient("Do you really want to delete selected records.");
            if (formField.BeforeDeleteMethods == null)
            {
                formField.BeforeDeleteMethods = new List<MethodCache>();
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
                        var method = assembly.appLogicType.GetMethod("BeforeDelete", BindingFlags.ExactBinding | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static, null, new Type[] { AppPress.Settings.ApplicationAppPress, t }, null);
                        if (method != null)
                        {
                            formField.BeforeDeleteMethods.Add(new MethodCache { method = method, SecondParam = t });
                        }
                    }
                }
            }
            if (formField.AfterDeleteMethods == null)
            {
                formField.AfterDeleteMethods = new List<MethodCache>();
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
                        var method = assembly.appLogicType.GetMethod("AfterDelete", BindingFlags.ExactBinding | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static, null, new Type[] { AppPress.Settings.ApplicationAppPress, t }, null);
                        if (method != null)
                        {
                            formField.AfterDeleteMethods.Add(new MethodCache { method = method, SecondParam = t });
                        }
                    }
                }
            }
            bool doTrans = a.site.trans == null;
            if (doTrans)
                a.BeginTrans();
            try
            {
                foreach (var methodCache in formField.BeforeDeleteMethods)
                {
                    var o = containerFieldValue;
                    if (o.GetType() != methodCache.SecondParam)
                    {
                        o = (FieldValue)Activator.CreateInstance(methodCache.SecondParam, new object[] { (FieldValue)o });
                    }
                    var errorCount = a.appPressResponse.Count(t => t.appPressResponseType == AppPressResponseType.FormError || t.appPressResponseType == AppPressResponseType.FieldError || t.appPressResponseType == AppPressResponseType.AlertMessage);

                    Util.InvokeMethod(a, methodCache.method, new object[] { a, o });
                    if (errorCount != a.appPressResponse.Count(t => t.appPressResponseType == AppPressResponseType.FormError || t.appPressResponseType == AppPressResponseType.FieldError || t.appPressResponseType == AppPressResponseType.AlertMessage))
                        throw new AppPressException();
                }
                DeleteForms(a, deletedFormDatas, DeleteFromDB);
                a.CheckContainerfieldValues();
                foreach (var methodCache in formField.AfterDeleteMethods)
                {
                    var o = containerFieldValue;
                    if (o.GetType() != methodCache.SecondParam)
                    {
                        o = (FieldValue)Activator.CreateInstance(methodCache.SecondParam, new object[] { (FieldValue)o });
                    }
                    var errorCount = a.appPressResponse.Count(t => t.appPressResponseType == AppPressResponseType.FormError || t.appPressResponseType == AppPressResponseType.FieldError || t.appPressResponseType == AppPressResponseType.AlertMessage);

                    Util.InvokeMethod(a, methodCache.method, new object[] { a, o });
                    if (errorCount != a.appPressResponse.Count(t => t.appPressResponseType == AppPressResponseType.FormError || t.appPressResponseType == AppPressResponseType.FieldError || t.appPressResponseType == AppPressResponseType.AlertMessage))
                        throw new AppPressException();
                }
                a.appPressResponse.Add(AppPressResponse.RefreshField(a, a.fieldValue.FormData.GetFieldValue(a.fieldValue.formField.containerFormField.fieldName), true));
                if (doTrans)
                    a.CommitTrans();
            }
            catch
            {
                if (doTrans)
                    a.RollbackTrans();
                throw;
            }
        }
        internal class DeleteEmbeddedTables
        {
            internal string embeddedTableName;
            internal string embeddedTableId;
        }
        internal class DateRangeContigousField
        {
            internal FormData formData;
            internal FormField fromFormField;
            internal FormField toFormField;
        }
        internal static void DeleteForms(AppPress a, List<FormData> deletedFormDatas, bool DeleteFromDB)
        {
            bool doTrans = a.site.trans == null;
            if (doTrans)
                a.BeginTrans();
            var allDeletedFormDatas = new List<FormData>();
            try
            {
                var DateRangeContigousFields = new List<DateRangeContigousField>();

                foreach (FormData formData1 in deletedFormDatas)
                {
                    var dfs = new List<FormData>();
                    Util.ApplyOnChildFormDatas(a.formDatas, formData1, t => dfs.Add(t));
                    dfs.Reverse(); // delete starting from deepest form
                    foreach (var formData in dfs)
                    {
                        if (DeleteFromDB)
                        {
                            if (!formData.IsNew)
                            {
                                string TableName = formData.formDef.TableName;
                                if (TableName == null)
                                    throw new Exception("Cannot Delete From: " + formData.formDef.formName + " as it does not have a TableName parameter.");
                                // Update for Contiguous Date Range
                                var containerIdFormField = formData.formDef.formFields.Find(t => t.Type == FormDefFieldType.ForeignKey);
                                var updateFieldValues = new List<FieldValue>();
                                updateFieldValues.AddRange(formData.fieldValues);
                                for (int i = 0; i < formData.formDef.formFields.Count(); ++i)
                                {
                                    var formField = formData.formDef.formFields[i];
                                    if (formField.Type == FormDefFieldType.DateTime && formField.IsDateRange != 0 && formField.Contiguous)
                                    {
                                        var dateToFormField = formData.formDef.formFields[i + 1];
                                        var idList = a.ExecuteStringList("Select Id From " + a.SQLQuote + TableName + a.SQLQuote + " Where " + containerIdFormField.fieldName + "=" + formData.containerFieldValue.FormData.id + " Order by " + formField.fieldName + ",Ifnull(" + dateToFormField.fieldName + ",'2100-1-1')");
                                        var idx = idList.FindIndex(t => t == formData.id);
                                        if (idx > 0)
                                        {
                                            var lastDateTo = a.ExecuteDateTime("Select " + dateToFormField.fieldName + " From " + a.SQLQuote + TableName + a.SQLQuote + " Where Id=" + idList[idx]);
                                            var lastDateToStr = lastDateTo == null ? "null" : ("'" + lastDateTo.Value.ToString(DAOBasic.DBDateFormat) + "'");
                                            var nr = a.ExecuteNonQuery("Update " + a.SQLQuote + TableName + a.SQLQuote + " Set " + dateToFormField.fieldName + "=" + lastDateToStr + " Where Id=" + idList[idx - 1]);
                                            if (nr != 1)
                                                throw new Exception("Internal Error: number of records updated should be 1");

                                        }
                                        i++;
                                    }
                                    else if (formField.Type == FormDefFieldType.PickMultiple)
                                    {
                                        if (formField.SaveTableName != null)
                                        {
                                            var lastFieldValue = a.ExecuteStringList("Select " + formField.fieldName + " From " + a.SQLQuote + formField.SaveTableName + a.SQLQuote + " Where " + a.SQLQuote + formField.SaveTableForeignKey + a.SQLQuote + "=" + formData.id);
                                            if (lastFieldValue != null)
                                            {
                                                var pmFieldValue = updateFieldValues.Find(t => t.formField.fieldName == formField.fieldName);
                                                var pmFieldValueA = new PickFieldValue();
                                                pmFieldValueA.formField = formField;
                                                pmFieldValueA.FormData = formData;
                                                pmFieldValueA.Title = string.Join(",", lastFieldValue);
                                                updateFieldValues.RemoveAll(t => t == pmFieldValue);
                                                updateFieldValues.Add(pmFieldValueA);
                                            }
                                        }
                                    }
                                }
                                a.SaveDBAudit(AuditType.DeleteRow, TableName, formData1.formDef.PrimaryKey, formData, updateFieldValues);
                                var deleteEmbeddedTables = new List<DeleteEmbeddedTables>();
                                foreach (var embeddedFormField in AppPress.EmbeddedFormsTables.FindAll(t => t.TableName == TableName))
                                {
                                    // save the form to be deleted
                                    var embeddedFormId = a.ExecuteString("Select " + embeddedFormField.FieldName + " From " + a.SQLQuote + embeddedFormField.TableName + a.SQLQuote + " Where id=" + formData.id);
                                    if (embeddedFormId != null)
                                        deleteEmbeddedTables.Add(new DeleteEmbeddedTables { embeddedTableName = embeddedFormField.EmbeddedTableName, embeddedTableId = embeddedFormId });
                                }
                                try
                                {
                                    var c = a.ExecuteNonQuery("Delete From " + a.SQLQuote + TableName + a.SQLQuote + " Where " + a.SQLQuote + formData.formDef.PrimaryKey + a.SQLQuote + "='" + formData.id + "'");
                                }
                                catch (MySqlException ex)
                                {
                                    if (ex.Number == 1451) // foreign Key
                                    {
                                        var query = a.site.GetForeignKeysQuery(TableName, formData.formDef.PrimaryKey);
                                        var dr = a.site.ExecuteQuery(query);
                                        var tables = new List<String>();
                                        var columns = new List<String>();
                                        var message = "Cannot Delete Row as it is used in Tables: ";
                                        try
                                        {
                                            while (dr.Read())
                                            {
                                                tables.Add(dr.GetString(0));
                                                columns.Add(dr.GetString(1));
                                            }
                                        }
                                        finally
                                        {
                                            dr.Close();
                                        }
                                        for (int i = 0; i < tables.Count; ++i)
                                        {
                                            var table = tables[i];
                                            // check if table has the value
                                            if (a.site.ExecuteScalar("Select " + a.site.SQLQuote + columns[i] + a.site.SQLQuote + " From " + a.site.SQLQuote + tables[i] + a.site.SQLQuote + " Where " + a.site.SQLQuote + columns[i] + a.site.SQLQuote + " = '" + formData.id + "'") == null)
                                            {
                                                tables.RemoveAt(i);
                                                columns.RemoveAt(i);
                                                i--;
                                            }

                                        }

                                        message = message + string.Join(",", tables) + " Columns: " + string.Join(",", columns) + " <br/><br/>Original Message:<br/>" + ex.Message;
                                        a.AlertMessage(message);
                                        throw new AppPressException();
                                    }
                                }
                                if (TableName != null)
                                {
                                    var optionCache = AppPress.optionsCacheTables.FindAll(t => t.tableName.ToLower() == TableName.ToLower());
                                    if (optionCache != null && optionCache.Count > 0)
                                    {
                                        for (int i = 0; i < optionCache.Count; i++)
                                            optionCache[i].optionFormField.BuildOptions(a, optionCache[i].formDef);
                                        AppPress.startTime = DateTime.UtcNow.Ticks;
                                        AppPressHandler.AppPressJSStr = null;
                                    }
                                }
                                foreach (var deleteEmbeddedTable in deleteEmbeddedTables)
                                {
                                    // delete the Embedded form
                                    a.ExecuteNonQuery("Delete From " + a.site.SQLQuote + deleteEmbeddedTable.embeddedTableName + a.site.SQLQuote + " Where Id=" + deleteEmbeddedTable.embeddedTableId);
                                }
                                for (int i = 0; i < formData.formDef.formFields.Count(); ++i)
                                {
                                    var dateFromFormField = formData.formDef.formFields[i];
                                    if (dateFromFormField.Type == FormDefFieldType.DateTime && dateFromFormField.IsDateRange != 0 && dateFromFormField.Contiguous)
                                    {
                                        var dateToFormField = formData.formDef.formFields[i + 1];
                                        DateRangeContigousFields.Add(new DateRangeContigousField { formData = formData, fromFormField = dateFromFormField, toFormField = dateToFormField });

                                        i++;
                                    }
                                }
                            }
                            allDeletedFormDatas.Add(formData);
                            a.formDatas.Remove(formData);
                        }
                        else
                        {
                            formData.IsDeleted = true;
                            a.appPressResponse.Add(AppPressResponse.SetPageDirty(true));
                        }
                    }
                }
                if (DeleteFromDB)
                {
                    foreach (var DateRangeContigousField in DateRangeContigousFields)
                        CheckDateRangeContiguousInDB(a, DateRangeContigousField.formData, DateRangeContigousField.fromFormField, DateRangeContigousField.toFormField);
                }
                if (doTrans)
                    a.CommitTrans();
            }

            catch (Exception)
            {
                // undo
                a.formDatas.AddRange(allDeletedFormDatas);
                if (doTrans)
                    a.RollbackTrans();
                throw;
            }
        }

        internal static void AddNewFieldDetail(AppPress a)
        {
            var containerFieldValue = a.fieldValue.FormData.GetFieldValue(a.fieldValue.formField.containerFormField.fieldName);
            GenericDeleteAllForms(a, containerFieldValue);

            a.appPressResponse.Add(AppPressResponse.RefreshField(a, containerFieldValue, true));
            a.appPressResponse.Add(AppPressResponse.SetPageDirty(true));

        }


        internal static void AddNewForm(AppPress a, FormData formData, FormContainerFieldValue containerFieldValue)
        {
            if (containerFieldValue != null)
            {
                var containerIdFormField = formData.formDef.formFields.Find(t => t.Type == FormDefFieldType.ForeignKey);
                if (containerIdFormField != null)
                    formData.SetFieldValue(containerIdFormField.fieldName, containerFieldValue.FormData.id);
            }
            var formDatas = containerFieldValue.GetContainedFormDatas(a);
            formData.fromPopupSave = true;
            a.CalcFormDatas(formData, null, true);
            formDatas.Add(formData);
            containerFieldValue.SetContainedFormDatas(formDatas);
            a.JsStr.Append("var obj = new Object(); obj.id='AppPress" + AppPress.IdSep + (int)containerFieldValue.formField.Type + AppPress.IdSep + containerFieldValue._GetHtmlId() + "';OnChange(obj,false);");
            a.appPressResponse.Add(AppPressResponse.RefreshField(a, containerFieldValue, true));
            formData.SetFocus(a);
            a.appPressResponse.Add(AppPressResponse.SetPageDirty(true));
        }
        internal static void AddNewForm(AppPress a, long? formDefId, FormContainerFieldValue containerFieldValue)
        {
            var formField = containerFieldValue.formField;
            FormDef formDef;
            if (formDefId != null)
                formDef = AppPress.FindFormDef(formDefId.Value);
            else
                formDef = formField.GetContainerRowFormDef(a);
            if (formDef == null)
                throw new Exception("Could not find ContainerRowForm for Field: " + formField.fieldName);
            var newFormData = FormData.NewFormData(a, formDef, containerFieldValue);
            newFormData.containerFieldValue = containerFieldValue;
            AddNewForm(a, newFormData, containerFieldValue);
        }

        internal static void AddNewForm(AppPress a)
        {
            AddNewForm(a, (long?)null, (FormContainerFieldValue)a.fieldValue.FormData.GetFieldValue(a.fieldValue.formField.containerFormField.fieldName));
        }

        internal static void AddNewForm(AppPress a, FormData formData)
        {
            AddNewForm(a, formData, (FormContainerFieldValue)a.fieldValue.FormData.GetFieldValue(a.fieldValue.formField.containerFormField.fieldName));
        }
        internal static void AddUploadRow(AppPress a)
        {
            var formDef = AppPress.FindFormDef("MultiFileUploadRow" + a.fieldValue.FormData.GetFieldValue(a.fieldValue.formField.containerFormField.fieldName).formField.fieldName);
            var formData = new FormData(a, formDef, null);
            var fieldValue = formData.fieldValues.Find(t => t.formField.Hidden && t.formField.OriginalType == (int)FormDefFieldType.MultiFileUpload);
            fieldValue.Value = a.fieldValue.Value;
            AddNewForm(a, formData);
            if (fieldValue.formField.AutoUpload && !a.fieldValue.FormData.IsNew)
                formData.Save(a);
        }

        internal static void SaveForm(AppPress a)
        {
            SaveForm(a, null);
            a.fieldValue.FormData.UpdateUI(a);
            a.ClosePopup();

        }
        internal static void SaveForm(AppPress a, FormData formDataToSave = null)
        {
            //if (a.fieldValue.formField != null && a.fieldValue.GetMethod(a, "OnClick") != null)
            //    return;
            //SessionData.GetSessionData(); // to check for session expiry

            int OriginalClientActionCount = a.appPressResponse.Count;
            //a.clientActions.Add(ClientAction.ClearFieldErrors());
            bool doTrans = a.site.trans == null;
            if (doTrans)
            {
                a.BeginTrans();
            }
            try
            {
                var formDatas = new List<FormData>();
                if (formDataToSave == null)
                    formDatas.AddRange(a.formDatas.FindAll(t => t.pageStackIndex == a.pageStackCount && (t.containerFieldValue == null || t.containerFieldValue.FormData.pageStackIndex != a.pageStackCount)));
                else
                    formDatas.Add(formDataToSave);
                //formDatas.Add(a.fieldValue.FormData);
                formDatas.RemoveAll(t => t.formDef.DoNoSaveInDB);
                var newFormDatas = formDatas;
                while (true)
                {
                    var allFormContainers = new List<FieldValue>();
                    foreach (var formData in newFormDatas)
                        allFormContainers.AddRange(formData.fieldValues.FindAll(t => t.formField.Type == FormDefFieldType.FormContainerDynamic));
                    newFormDatas = a.formDatas.FindAll(t => allFormContainers.Find(t1 => t.containerFieldValue != null && t.containerFieldValue.fieldDefId == t1.fieldDefId && t.containerFieldValue.FormData.id == t1.FormData.id) != null);
                    if (newFormDatas.Count == 0)
                        break;
                    formDatas.AddRange(newFormDatas.FindAll(t => !t.formDef.DoNoSaveInDB && t.formDef.TableName != null));
                }
                var startCount = a.appPressResponse.Count(t => t.appPressResponseType == AppPressResponseType.FormError || t.appPressResponseType == AppPressResponseType.FieldError);
                a.ClearErrors();
                foreach (var formData in formDatas)
                    if (!formData.IsDeleted)
                    {
                        if (formData._Validate())
                        {
                            if (formData.formDef.BeforeSaveMethods == null)
                            {
                                formData.formDef.BeforeSaveMethods = new List<MethodCache>();
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
                                        var method = assembly.appLogicType.GetMethod("BeforeSave", BindingFlags.ExactBinding | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static, null, new Type[] { AppPress.Settings.ApplicationAppPress, t }, null);
                                        if (method != null)
                                        {
                                            formData.formDef.BeforeSaveMethods.Add(new MethodCache { method = method, SecondParam = t });
                                        }
                                    }
                                }
                            }
                            foreach (var methodCache in formData.formDef.BeforeSaveMethods)
                            {
                                var o = formData;
                                if (o.GetType() != methodCache.SecondParam)
                                {
                                    o = (FormData)Activator.CreateInstance(methodCache.SecondParam, new object[] { (FormData)o });
                                }
                                var errorCount = a.appPressResponse.Count(t => t.appPressResponseType == AppPressResponseType.FormError || t.appPressResponseType == AppPressResponseType.FieldError || t.appPressResponseType == AppPressResponseType.AlertMessage);
                                var pFieldValue = a.fieldValue;
                                a.fieldValue = a.originalFieldValue;
                                try
                                {
                                    Util.InvokeMethod(a, methodCache.method, new object[] { a, o });
                                }
                                finally
                                {
                                    a.fieldValue = pFieldValue;
                                }
                                if (errorCount != a.appPressResponse.Count(t => t.appPressResponseType == AppPressResponseType.FormError || t.appPressResponseType == AppPressResponseType.FieldError || t.appPressResponseType == AppPressResponseType.AlertMessage))
                                    throw new AppPressException();
                            }
                        }
                    }
                if (startCount < a.appPressResponse.Count(t => t.appPressResponseType == AppPressResponseType.FormError || t.appPressResponseType == AppPressResponseType.FieldError || t.appPressResponseType == AppPressResponseType.AlertMessage))
                    throw new AppPressException();

                // first delete the forms
                foreach (var formData in formDatas.FindAll(t => t.IsDeleted))
                {
                    FormDef formDef = formData.formDef;
                    if (!formData.IsNew)
                    {
                        // save history
                        // save all column
                        var tableName = formDef.TableName;
                        if (tableName == null)
                            throw new Exception("Could not find TableName for formDef:" + formDef.formName);
                        a.SaveDBAudit(AuditType.DeleteRow, tableName, formDef.PrimaryKey, formData, formData.fieldValues);
                        a.ExecuteNonQuery("Delete From " + a.SQLQuote + "" + tableName + "" + a.SQLQuote + " Where " + a.SQLQuote + formDef.PrimaryKey + a.SQLQuote + "=" + formData.id);
                        if (tableName != null)
                        {
                            var optionCache = AppPress.optionsCacheTables.FindAll(t => t.tableName.ToLower() == tableName.ToLower());
                            if (optionCache != null && optionCache.Count > 0)
                            {
                                for (int i = 0; i < optionCache.Count; i++)
                                {
                                    optionCache[i].optionFormField.BuildOptions(a, optionCache[i].formDef);
                                    if (optionCache[i].optionFormField.optionsCache != null && optionCache[i].optionFormField.Type == FormDefFieldType.Pickone && optionCache[i].optionFormField.Style == FormDefFieldStyle.DropDown)
                                        a.JsStr.Append(optionCache[i].optionFormField.BuildOptionsForJS());
                                }
                                AppPress.startTime = DateTime.UtcNow.Ticks;
                                AppPressHandler.AppPressJSStr = null;
                            }
                        }
                    }
                    formDatas.Remove(formData);
                }
                var embeddedFormsSaved = new List<FormData>();
                foreach (var formData in formDatas)
                {
                    if (embeddedFormsSaved.Find(t => t == formData) != null)
                        continue;
                    var TableName = formData.formDef.TableName;
                    var containerIdFormField = formData.formDef.formFields.Find(t => t.Type == FormDefFieldType.ForeignKey);
                    for (int i = 0; i < formData.formDef.formFields.Count(); ++i)
                    {
                        var sFormField = formData.formDef.formFields[i];
                        if (sFormField.Type == FormDefFieldType.DateTime && sFormField.IsDateRange != 0)
                        {
                            var dateToFormField = formData.formDef.formFields[++i];// skip DateTo

                            if (!sFormField.Static && sFormField.Contiguous)
                            {
                                var idList = a.ExecuteStringList("Select " + formData.formDef.PrimaryKey + " From " + a.SQLQuote + "" + TableName + "" + a.SQLQuote + " Where " + containerIdFormField.fieldName + "='" + formData.GetFieldString(containerIdFormField.fieldName) + "' Order by " + sFormField.fieldName + ",Ifnull(" + dateToFormField.fieldName + ",'2100-1-1')");
                                if (formData.IsNew)
                                {
                                    var idx = idList.Count() - 1;
                                    if (idx >= 0)
                                    {
                                        DateTime toDate = (DateTime)formData.GetFieldDateTime(sFormField.fieldName);
                                        //Set Date as one day before from new Status
                                        var q = "Select " + sFormField.fieldName + " From " + a.SQLQuote + "" + TableName + "" + a.SQLQuote + " Where " + formData.formDef.PrimaryKey + "=" + idList[idx];
                                        if (a.ExecuteDateTime(q).Value.Date != toDate)
                                            toDate = toDate.AddDays(-1);
                                        a.ExecuteNonQuery("Update " + a.SQLQuote + "" + TableName + "" + a.SQLQuote + " Set " + dateToFormField.fieldName + "='" + toDate.ToString(DAOBasic.DBDateTimeFormat) + "' Where " + formData.formDef.PrimaryKey + "=" + idList[idx]);
                                    }
                                }
                                else
                                {
                                    // If DateFrom,DateTo of old status is updated then update in other records also
                                    var idx = idList.FindIndex(t => t == formData.id);
                                    if (idx > 0)
                                    {
                                        // update previous
                                        var fromDate = (DateTime)formData.GetFieldDateTime(sFormField.fieldName);
                                        var q = "Select " + sFormField.fieldName + " From " + a.SQLQuote + "" + TableName + "" + a.SQLQuote + " Where " + formData.formDef.PrimaryKey + "=" + idList[idx];
                                        var oldDate = a.ExecuteDateTime(q).Value.Date;
                                        var daysDiff = oldDate.Subtract(fromDate).TotalDays;
                                        if (daysDiff < 0 || daysDiff > 1)
                                        {
                                            fromDate = fromDate.AddDays(-1);
                                            a.ExecuteNonQuery("Update " + a.SQLQuote + "" + TableName + "" + a.SQLQuote + " Set " + dateToFormField.fieldName + "='" + fromDate.ToString(DAOBasic.DBDateTimeFormat) + "' Where " + formData.formDef.PrimaryKey + "=" + idList[idx - 1]);
                                        }
                                    }
                                    if (idx < idList.Count() - 1)
                                    {
                                        // update next
                                        var toDate = (DateTime)formData.GetFieldDateTime(dateToFormField.fieldName);
                                        var q = "Select " + dateToFormField.fieldName + " From " + a.SQLQuote + "" + TableName + "" + a.SQLQuote + " Where " + formData.formDef.PrimaryKey + "=" + idList[idx];
                                        var oldDate = a.ExecuteDateTime(q).Value.Date;
                                        var daysDiff = toDate.Subtract(oldDate).TotalDays;
                                        if (daysDiff < 0 || daysDiff > 1)
                                        {
                                            toDate = toDate.AddDays(+1);
                                            a.ExecuteNonQuery("Update " + a.SQLQuote + "" + TableName + "" + a.SQLQuote + " Set " + sFormField.fieldName + "='" + toDate.ToString(DAOBasic.DBDateTimeFormat) + "' Where id=" + idList[idx + 1]);
                                        }
                                    }
                                }
                            }
                        }
                        else if (sFormField.Type == FormDefFieldType.FormContainerDynamic && sFormField.OriginalType == (int)FormDefFieldType.EmbeddedForm)
                        {
                            // Save Embedded Forms First
                            var embeddedForm = formDatas.Find(t => t.containerFieldValue == formData.fieldValues.Find(t1 => t1.formField.id == sFormField.id));
                            if (embeddedForm == null)
                            {
                                if (sFormField.Hidden)
                                {
                                    // Forms Mapped to table having Embedded Form have a hidden Embedded field
                                    var embeddedFormDef = AppPress.FindFormDef(sFormField.fieldName.Substring(0, sFormField.fieldName.Length - "Container".Length));
                                    var id = a.ExecuteIdentityInsert("Insert Into " + a.SQLQuote + embeddedFormDef.TableName + a.SQLQuote + "() Values()", embeddedFormDef.TableName);
                                    formData.fieldValues.Find(t => t.formField.FormNameProperty == sFormField.FormNameProperty && t.formField.Type == FormDefFieldType.Text).Value = id.ToString();
                                }
                            }
                            else if (embeddedForm.IsNew)
                            {
                                embeddedForm.Save(a);
                                formData.fieldValues.Find(t => t.formField.FormNameProperty == sFormField.FormNameProperty && t.formField.Type == FormDefFieldType.Text).Value = embeddedForm.id;
                                embeddedFormsSaved.Add(embeddedForm);
                            }
                        }
                    }
                    var oid = formData.id;
                    var oIsNew = formData.IsNew;
                    _SaveForm(formData, a);
                    // Update Ids of child forms
                    if (oIsNew)
                    {
                        var query = a.site.GetForeignKeysQuery(formData.formDef.TableName, formData.formDef.PrimaryKey);
                        var dr = a.ExecuteQuery(query);
                        try
                        {
                            while (dr.Read())
                            {
                                var tableName = dr.GetString(0);
                                var columnName = dr.GetString(1);
                                foreach (var cFormData in formDatas)
                                    //Ram: Added null check here because in some case table is null. Fix for EmploeeExitForm
                                    if (cFormData.formDef.TableName != null && cFormData.formDef.TableName.Equals(tableName, StringComparison.OrdinalIgnoreCase))
                                    {
                                        var f = cFormData.GetFieldValue(columnName);
                                        if (f != null)
                                        {
                                            //if (f.Value != oid)
                                            //    throw new Exception("Form: " + cFormData.formDef.formName + " Field: " + f.formField.fieldName + " has Value: " + f.Value + " which should be: " + oid);
                                            if (f.Value == oid)
                                                f.Value = formData.id;
                                        }
                                    }
                            }
                        }
                        finally
                        {
                            dr.Close();
                        }

                    }

                }
                foreach (var formData in formDatas)
                    foreach (var fieldValue in formData.fieldValues)
                        if (fieldValue.formField.Type == FormDefFieldType.FormContainerDynamic && fieldValue.ReArranged)
                        {
                            var gridFormDatas = formDatas.FindAll(t => t.containerFieldValue == fieldValue);
                            var formIds = new List<string>();
                            foreach (var fData in gridFormDatas)
                                formIds.Add(fData.id);
                            formIds.Sort();
                            for (int i = 0; i < gridFormDatas.Count(); ++i)
                            {
                                var formData1 = gridFormDatas[i];
                                var tableName = formData1.formDef.TableName;
                                if (tableName == null)
                                    throw new Exception("Cannot Change id for Form: " + formData1.formDefId + " as tableName is not present in parameter.");
                                var PrimaryKey = formData1.formDef.PrimaryKey;
                                var newId = formIds[i];
                                if (newId != formData1.id)
                                {
                                    int updateCount = 0;
                                    updateCount += a.ExecuteNonQuery("Update " + tableName + " Set " + a.SQLQuote + PrimaryKey + a.SQLQuote + "=0 Where Id=" + newId);
                                    updateCount += a.ExecuteNonQuery("Update " + tableName + " Set " + a.SQLQuote + PrimaryKey + a.SQLQuote + "=" + newId + " Where Id=" + formData1.id);
                                    updateCount += a.ExecuteNonQuery("Update " + tableName + " Set " + a.SQLQuote + PrimaryKey + a.SQLQuote + "=" + formData1.id + " Where Id=0");
                                    if (updateCount != 3)
                                        throw new Exception("Internal Error in Updating Form Id.");
                                    var id = formData1.id;
                                    var nFormData = a.formDatas.Find(t => t.formDefId == formData1.formDefId && t.id == newId);
                                    var oFormData = a.formDatas.Find(t => t.formDefId == formData1.formDefId && t.id == id);
                                    nFormData.id = id;
                                    oFormData.id = newId;
                                }
                            }
                        }
                foreach (var formData in formDatas)
                    for (int i = 0; i < formData.formDef.formFields.Count(); ++i)
                    {
                        var formField = formData.formDef.formFields[i];
                        if (formField.Type == FormDefFieldType.DateTime && formField.IsDateRange != 0)
                        {
                            if (!formField.Static)
                                if (formField.Contiguous)
                                    AppPressLogic.CheckDateRangeContiguousInDB(a, formData, formField, formData.formDef.formFields[i + 1]);
                                else if (formField.NonOverlapping)
                                    AppPressLogic.CheckDateRangeNonOverlappingInDB(a, formData, formField, formData.formDef.formFields[i + 1]);
                            i++;
                        }
                    }
                for (int i = 0; i < formDatas.Count; ++i)
                {
                    var formData = formDatas[i];
                    if (formData.formDef.AfterSaveMethods == null)
                    {
                        formData.formDef.AfterSaveMethods = new List<MethodCache>();
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
                                var method = assembly.appLogicType.GetMethod("AfterSave", BindingFlags.ExactBinding | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static, null, new Type[] { AppPress.Settings.ApplicationAppPress, t }, null);
                                if (method != null)
                                {
                                    formData.formDef.AfterSaveMethods.Add(new MethodCache { method = method, SecondParam = t });
                                }
                            }
                        }
                    }
                    foreach (var methodCache in formData.formDef.AfterSaveMethods)
                    {
                        var o = formData;
                        if (o.GetType() != methodCache.SecondParam)
                        {
                            o = (FormData)Activator.CreateInstance(methodCache.SecondParam, new object[] { (FormData)o });
                        }
                        //var errorCount = a.appPressResponse.Count(t => t.appPressResponseType == AppPressResponseType.FormError || t.appPressResponseType == AppPressResponseType.FieldError || t.appPressResponseType == AppPressResponseType.AlertMessage);

                        var pFieldValue = a.fieldValue;
                        a.fieldValue = a.originalFieldValue;
                        try
                        {
                            Util.InvokeMethod(a, methodCache.method, new object[] { a, o });
                        }
                        finally
                        {
                            a.fieldValue = pFieldValue;
                        }
                        //if (errorCount != a.appPressResponse.Count(t => t.appPressResponseType == AppPressResponseType.FormError || t.appPressResponseType == AppPressResponseType.FieldError || t.appPressResponseType == AppPressResponseType.AlertMessage))
                        //    throw new AppPressException();
                    }
                }
                if (doTrans)
                {
                    a.CommitTrans();
                }
            }
            catch (AppPressException)
            {
                if (doTrans)
                    a.RollbackTrans();
                throw;
            }
            catch (Exception e)
            {
                // to alert for error
                if (!e.Message.IsNullOrEmpty())
                    a.appPressResponse.Add(AppPressResponse.AlertMessage(e.Message));
                if (doTrans)
                    a.RollbackTrans();
                throw;
            }
        }


        private static System.Object lockUpdateMovedRows = new System.Object();

        public static List<Option> GetApplicationManagerOptions(AppPress a, string id)
        {
            var options = new List<Option>();
            switch (a.fieldValue.formField.fieldName)
            {
                case "FormType":
                    foreach (var FormType in (FormType[])Enum.GetValues(typeof(FormType)))
                        options.Add(new Option { id = ((int)FormType).ToString(), value = FormType.ToString() });
                    break;
                case "Type":
                    foreach (var FieldType in (FormDefFieldType[])Enum.GetValues(typeof(FormDefFieldType)))
                        if ((int)FieldType != 0)
                            options.Add(new Option { id = ((int)FieldType).ToString(), value = FieldType.ToString() });
                    break;
                case "Encryption":
                    foreach (var EncryptionType in (EncryptionType[])Enum.GetValues(typeof(EncryptionType)))
                        if (EncryptionType != AppPressFramework.EncryptionType.None)
                            options.Add(new Option { id = ((int)EncryptionType).ToString(), value = EncryptionType.ToString() });
                    break;
                case "Style":
                    switch (a.fieldValue.FormData.formDef.formName)
                    {
                        case "Button":
                            options.Add(new Option { id = ((int)FormDefFieldStyle.Button).ToString(), value = FormDefFieldStyle.Button.ToString() });
                            options.Add(new Option { id = ((int)FormDefFieldStyle.Link).ToString(), value = FormDefFieldStyle.Link.ToString() });
                            break;
                        case "Pickone":
                            options.Add(new Option { id = ((int)FormDefFieldStyle.Radio).ToString(), value = FormDefFieldStyle.Radio.ToString() });
                            options.Add(new Option { id = ((int)FormDefFieldStyle.DropDown).ToString(), value = FormDefFieldStyle.DropDown.ToString() });
                            break;
                        case "PickMultiple":
                            options.Add(new Option { id = ((int)FormDefFieldStyle.Checkboxes).ToString(), value = FormDefFieldStyle.Checkboxes.ToString() });
                            options.Add(new Option { id = ((int)FormDefFieldStyle.DropDown).ToString(), value = FormDefFieldStyle.DropDown.ToString() });
                            break;
                        case "GetContainerRowForms":
                            options.Add(new Option { id = "1", value = "Grid" });
                            options.Add(new Option { id = "2", value = "Tree" });
                            break;
                    }
                    break;

            }

            return options;
        }

        internal static List<FormData> BindEmbeddedForm(AppPress a)
        {
            var rowFormDef = AppPress.FindFormDef(a.fieldValue.formField.FormNameProperty);
            var query = rowFormDef.GetViewQuery(a);
            var fieldName = a.fieldValue.formField.fieldName;
            fieldName = fieldName.Substring(0, fieldName.Length - "Container".Length);
            var id = a.fieldValue.FormData.GetFieldString(fieldName);
            if (a.fieldValue.FormData.IsNew)
                return null;
            if (id == null)
            {
                a.site.BeginTrans();
                try
                {
                    id = a.site.ExecuteIdentityInsert("Insert into `" + rowFormDef.TableName + "`() values()", rowFormDef.TableName).ToString();
                    a.site.ExecuteNonQuery("Update `" + a.fieldValue.FormData.formDef.TableName + "` Set `" + fieldName + "`=" + id + " Where id=" + a.fieldValue.FormData.id);
                    a.site.Commit();
                }
                catch
                {
                    a.site.RollBack();
                    throw;
                }
            }
            query += " and " + a.SQLQuote + rowFormDef.PrimaryKey + a.SQLQuote + "='" + a.EscapeSQLString(id) + "'\n";
            return FormData.ReadFormDatas(a, null, rowFormDef, query);
        }

        internal static void _SaveForm(FormData formData, AppPress a)
        {

            string orginalFormDataId = formData.id;
            FormDef formDef = formData.formDef;
            string insertColumns = "";
            string insertValues = "";
            var updateSet = "";
            var imageUpdateFieldValues = new List<FieldValue>();
            var updatedFieldValues = new List<FieldValue>();
            foreach (FieldValue fieldValue in formData.fieldValues)
            {

                var formField = fieldValue.formField;
                if (formField.fieldName == "SelectRow")
                    continue;
                if (fieldValue.formField.DoNotSaveInDB || fieldValue.DoNotSaveInDB)
                    continue;
                switch (formField.Type)
                {
                    case FormDefFieldType.PickMultiple:
                        if (formField.SaveTableName == null && formField.SaveTableForeignKey == null)
                            break;
                        if (formField.SaveTableName == null || formField.SaveTableForeignKey == null)
                            throw new Exception(formField.GetDescription() + " needs to have SaveTableName and SaveTableForeignKey properties");
                        var oldValue = a.ExecuteStringList("Select " + a.SQLQuote + formField.fieldName + a.SQLQuote + " From " + a.SQLQuote + formField.SaveTableName + a.SQLQuote + " Where " + a.SQLQuote + formField.SaveTableForeignKey + a.SQLQuote + "=" + formData.id);
                        var pmFieldValue = new PickFieldValue();
                        pmFieldValue.formField = fieldValue.formField;
                        pmFieldValue.fieldDefId = fieldValue.fieldDefId;
                        pmFieldValue.Value = fieldValue.Value;
                        pmFieldValue.FormData = fieldValue.FormData;
                        if (oldValue != null)
                            pmFieldValue.Title = string.Join(",", oldValue); // cludge 
                        updatedFieldValues.Add(pmFieldValue);
                        break;
                    case FormDefFieldType.FormContainerDynamic:
                        // call domain function with a.calltype = beforeSaving
                        var pFieldValue = a.fieldValue;
                        var pServerFunction = a.serverFunction;
                        try
                        {
                            a.fieldValue = fieldValue;
                            var domainFunctions = fieldValue.formField.GetFieldFunctions(FunctionType.Domain);
                            foreach (var domainFunction in domainFunctions)
                            {
                                a.serverFunction = domainFunction;
                                var method = Util.GetMethod(a, domainFunction.FunctionName, new Type[] { AppPress.Settings.ApplicationAppPress, typeof(FormCallType) });
                                if (method != null)
                                    Util.InvokeMethod(a, method, new object[] { a, FormCallType.BeforeSaving });
                            }
                        }
                        finally
                        {
                            a.serverFunction = pServerFunction;
                            a.fieldValue = pFieldValue;
                        }
                        break;
                    case FormDefFieldType.Password:
                        throw new Exception("Password cannot be saved using AppPress. Mark it as DoNotSaveInDB in Form and save it from from Logic");
                    case FormDefFieldType.FileUpload:
                        imageUpdateFieldValues.Add(fieldValue);
                        goto case FormDefFieldType.Number;
                    case FormDefFieldType.ForeignKey:
                        if (formDef.TableName == null)
                            continue;
                        if (!formData.IsNew)
                            // dont need to save contained id in update as it will be same as before
                            // this was giving concurency error in case add a new row in identityDetail in EmployeeProfile followed by Save Page
                            continue;
                        goto case FormDefFieldType.TextArea;
                    case FormDefFieldType.TextArea:
                    case FormDefFieldType.Text:
                    case FormDefFieldType.Pickone:
                    case FormDefFieldType.Checkbox:
                    case FormDefFieldType.DateTime:
                    case FormDefFieldType.Number:

                        string NewValue = fieldValue.Value;
                        if (formField.Type != FormDefFieldType.FileUpload)
                            switch (formField.EncryptionType)
                            {
                                case AppPressFramework.EncryptionType.AES:
                                    {
                                        NewValue = APCrypto.EncryptStringAES(fieldValue.Value);
                                        break;
                                    }
                                case AppPressFramework.EncryptionType.DES:
                                    {
                                        NewValue = Util.EncryptDES(fieldValue.Value);
                                        break;
                                    }
                            }
                        if (formData.IsNew)
                        {
                            insertColumns += "," + a.SQLQuote + fieldValue.formField.fieldName + a.SQLQuote;
                            if (NewValue == null)
                                insertValues += ",null";
                            else
                                insertValues += ",N'" + a.EscapeSQLString(NewValue) + "'";
                        }
                        else
                        {
                            updateSet += "," + a.SQLQuote + fieldValue.formField.fieldName + a.SQLQuote + "=";
                            if (NewValue == null)
                                updateSet += "null";
                            else
                                updateSet += "N'" + a.EscapeSQLString(NewValue) + "'";
                        }
                        updatedFieldValues.Add(fieldValue);
                        break;

                }
            }
            try
            {
                var tableName = formDef.TableName;
                try
                {
                    if (formData.IsNew)
                    {
                        //if (tableName == null && !insertValues.IsNullOrEmpty())
                        //    throw new Exception("For Form: " + formDef.formName + " Table name cannot be blank as it is has Fields (" + insertColumns.Substring(1) + ") that needs to be saved in Database.");
                        if (tableName != null)
                        {
                            var oid = formData.id;
                            var idColumnName = formData.NewId == null ? (a.site.databaseType == DatabaseType.SqlServer ? "" : (formDef.PrimaryKey + ",")) : (formDef.PrimaryKey + ",");
                            var idValue = formData.NewId == null ? (a.site.databaseType == DatabaseType.SqlServer ? "" : "DEFAULT,") : (formData.NewId + ",");
                            formData.id = a.ExecuteIdentityInsert("Insert Into " + a.SQLQuote + tableName + a.SQLQuote + "(" + idColumnName + "" + insertColumns.Substring(1) + ") Values(" + idValue + "" + insertValues.Substring(1) + ")", tableName).ToString();
                            if (idValue == (formData.NewId + ","))
                                formData.id = idValue.Replace(",", "");
                            if (AppPress.TryGetSessionData() != null)
                                a.SaveDBAudit(AuditType.InsertRow, tableName, formDef.PrimaryKey, formData, updatedFieldValues);
                            // Refresh Containers where the Id has changed
                            if (formData.containerFieldValue != null)
                                formData.ChangeIDInClient(a, oid, formData.id);
                        }
                    }
                    else if (!updateSet.IsNullOrEmpty())
                    {
                        if (tableName == null)
                            throw new Exception("For Form: " + formDef.formName + " Table name cannot be blank as it is has Fields that needs to be saved in Database.");
                        var timeStamp = (long?)a.ExecuteScalar("Select TimeStamp From Application_Audit Where rowId='" + formData.id + "' and TableName='" + tableName + "' and id<=" + a.lastApplicationAuditId);
                        if (timeStamp != null)
                        {
                            if (timeStamp > a.PageTimeStamp)
                                throw new AppPressException("Concurrency error: This Form is already updated by another user.");
                        }
                        a.SaveDBAudit(AuditType.UpdateRow, tableName, formDef.PrimaryKey, formData, updatedFieldValues);
                        a.ExecuteNonQuery("Update " + a.SQLQuote + tableName + a.SQLQuote + " Set " + updateSet.Substring(1) + " Where " + a.SQLQuote + formDef.PrimaryKey + a.SQLQuote + "='" + formData.id + "'");
                    }
                    if (tableName != null)
                    {
                        var optionCache = AppPress.optionsCacheTables.FindAll(t => t.tableName.ToLower() == tableName.ToLower());
                        if (optionCache != null && optionCache.Count > 0)
                        {
                            for (int i = 0; i < optionCache.Count; i++)
                            {
                                optionCache[i].optionFormField.BuildOptions(a, optionCache[i].formDef);
                                if (optionCache[i].optionFormField.optionsCache != null && optionCache[i].optionFormField.Type == FormDefFieldType.Pickone && optionCache[i].optionFormField.Style == FormDefFieldStyle.DropDown)
                                    a.JsStr.Append(optionCache[i].optionFormField.BuildOptionsForJS());
                            }
                            AppPress.startTime = DateTime.UtcNow.Ticks;
                            AppPressHandler.AppPressJSStr = null;
                        }
                    }
                }
                catch (System.Data.SqlClient.SqlException ex)
                {
                    if (ex.Number == 2601)
                    {
                        a.AlertMessage("Found Duplicate: ");
                        throw new AppPressException();
                    }
                }
                catch (MySqlException ex)
                {
                    if (ex.Number == 1048) // Column cannot be null
                    {
                        // Show this field is required error
                        var fieldName = ex.Message.Replace("Column '", "").Replace("' cannot be null", "");
                        var fieldValue = formData.GetFieldValue(fieldName);
                        if (fieldValue != null && fieldValue.Hidden == FieldHiddenType.None)
                        {
                            a.appPressResponse.Add(AppPressResponse.FieldError(fieldValue, AppPress.GetLocalizationKeyValue("LAKey_RequiredMsg")));
                            throw new AppPressException();
                        }
                    }
                    if (ex.Number == 1406) // Data too Long
                    {
                        var fieldName = ex.Message.Replace("Data too long for column '", "").Replace("' at row 1", "");
                        var fieldValue = formData.GetFieldValue(fieldName);
                        if (fieldValue != null && fieldValue.Hidden == FieldHiddenType.None)
                        {
                            a.appPressResponse.Add(AppPressResponse.FieldError(fieldValue, "Data too long"));
                            throw new AppPressException();
                        }
                    }
                    if (ex.Number == 1062) // Unique
                    {
                        var indexes = a.ExecuteStringList(@"
                            SELECT distinct(Index_name) FROM information_schema.statistics 
                            WHERE table_schema = '" + a.site.dbName + @"' AND table_name = '" + tableName + "' AND NON_UNIQUE=0 and INDEX_NAME<>'PRIMARY'");
                        var indexNameIndex = indexes.FindIndex(t => ex.Message.IndexOf(t) != -1);
                        if (indexNameIndex != -1)
                        {
                            var columns = a.ExecuteStringList(@"
                                SELECT COLUMN_NAME FROM information_schema.statistics 
                                WHERE table_schema = '" + a.site.dbName + @"' AND table_name = '" + tableName + "' AND NON_UNIQUE=0 and INDEX_NAME<>'PRIMARY' and INDEX_NAME='" + indexes[indexNameIndex] + "' Order By SEQ_IN_INDEX");
                            for (int i = 0; i < columns.Count(); ++i)
                            {
                                var formField = formData.formDef.formFields.Find(t => t.fieldName == columns[i]);
                                if (formField != null)
                                {
                                    if (formField.Type == FormDefFieldType.ForeignKey)
                                    {
                                        columns[i] = null;
                                        continue;
                                    }
                                    columns[i] = formField.GetDisplayName(false);
                                }
                            }
                            columns.RemoveAll(t => t == null);
                            a.AlertMessage("Duplicate: " + string.Join(", ", columns));
                            throw new AppPressException();
                        }
                    }
                    throw;
                }
            }
            catch
            {
                formData.id = orginalFormDataId;
                throw;
            }
            foreach (FieldValue fieldValue in formData.fieldValues)
            {
                var formField = fieldValue.formField;
                switch (formField.Type)
                {
                    case FormDefFieldType.FormContainerDynamic:
                        // call domain function with a.calltype = afterSaving
                        var pFieldValue = a.fieldValue;
                        var pServerFunction = a.serverFunction;
                        try
                        {
                            a.fieldValue = fieldValue;
                            var domainFunctions = fieldValue.formField.GetFieldFunctions(FunctionType.Domain);
                            foreach (var domainFunction in domainFunctions)
                            {
                                a.serverFunction = domainFunction;
                                var method = Util.GetMethod(a, domainFunction.FunctionName, new Type[] { AppPress.Settings.ApplicationAppPress, typeof(FormCallType) });
                                if (method != null)
                                    Util.InvokeMethod(a, method, new object[] { a, FormCallType.AfterSaving });
                            }
                        }
                        finally
                        {
                            a.serverFunction = pServerFunction;
                            a.fieldValue = pFieldValue;
                        }
                        break;
                    case FormDefFieldType.PickMultiple:
                        if (!formField.DoNotSaveInDB && formField.SaveTableName != null)
                        {
                            if (formField.SaveTableForeignKey == null)
                                throw new AppPressException(formField.GetDescription() + " Foreign Key should not be blank.");
                            a.ExecuteNonQuery("Delete From " + a.SQLQuote + formField.SaveTableName + a.SQLQuote + " Where " + a.SQLQuote + formField.SaveTableForeignKey + a.SQLQuote + "=" + formData.id);
                            if (fieldValue.Value != null)
                            {
                                var values = fieldValue.Value.Split(new char[] { ',' });
                                foreach (var value in values)
                                    a.ExecuteIdentityInsert("Insert Into " + a.SQLQuote + formField.SaveTableName + a.SQLQuote + "(" + a.SQLQuote + formField.SaveTableForeignKey + a.SQLQuote + "," + a.SQLQuote + formField.fieldName + a.SQLQuote + ") Values(" + formData.id + ",'" + value + "')", formField.TableName);
                            }
                        }
                        break;
                }
            }
        }

        internal static void SendEmails()
        {
            while (true)
            {
                Thread.Sleep(10000); // 1 sec
                var site = new DAOBasic();
                try
                {
                    List<EmailDetails> lstPendingEmails = new List<EmailDetails>();
                    IDataReader dr = site.ExecuteQuery("Select * From Application_sendmails Where Error is null Order By Id");
                    Dictionary<Int64, string> dcFileIds = new Dictionary<long, string>();
                    try
                    {
                        while (dr.Read())
                        {
                            EmailDetails oEmailDetails = new EmailDetails();
                            oEmailDetails.FileAttachments = new List<string>();
                            oEmailDetails.FileBytesAttachments = new List<FileDetails>();

                            oEmailDetails.Subject = dr["Subject"].ToString();
                            oEmailDetails.Body = dr["content"].ToString();
                            oEmailDetails.ToEmail = dr["ReplyTo"].ToString();

                            var smtpSection = (SmtpSection)ConfigurationManager.GetSection("system.net/mailSettings/smtp");
                            oEmailDetails.FromEmail = smtpSection.From;

                            oEmailDetails.FromEmailName = dr["From"].ToString();
                            oEmailDetails.CC = dr["cc"].ToString();
                            oEmailDetails.BCC = Convert.ToString(dr["bcc"]).ToUnEscapeString();
                            oEmailDetails.IsHtml = dr.GetInt16(dr.GetOrdinal("isHtml")) == 1;
                            oEmailDetails.RowId = (Int64)dr["Id"];

                            var filePath = dr["FileName"].ToString();
                            if (filePath != null && filePath.Length > 0)
                                oEmailDetails.FileAttachments.Add(filePath);

                            var fileIds = dr["UploadFileIds"].ToString();
                            if (fileIds != null && fileIds.Length > 0)
                            {
                                string[] ids = fileIds.Split(',');
                                foreach (var id in ids)
                                {
                                    if (id.Trim().Length == 0)
                                        continue;
                                    var file = AppPress.GetFile(Convert.ToInt32(id)); // do not have files in local db
                                    if (file != null)
                                        oEmailDetails.FileBytesAttachments.Add(file);
                                }
                            }
                            lstPendingEmails.Add(oEmailDetails);
                        }
                    }
                    finally { dr.Close(); }

                    foreach (var emailDetails in lstPendingEmails)
                    {
                        var error = Util.SendEmail(emailDetails);
                        if (error == null)
                        {
                            var fileIds = site.ExecuteString("Select UploadFileIds From Application_sendmails Where id = " + emailDetails.RowId);
                            site.ExecuteNonQuery("Delete From Application_sendmails Where id =" + emailDetails.RowId);
                            if (fileIds != null)
                                site.ExecuteNonQuery("Delete From Application_Files Where id in (" + fileIds + ")");
                        }
                        else
                            site.ExecuteNonQuery("Update Application_sendmails Set Error='" + site.EscapeSQLString(error) + "' Where ID =" + emailDetails.RowId);
                    }
                }
                catch (Exception e)
                {
                    string errorMsg = e.Message;
                    while (e.InnerException != null)
                    {
                        errorMsg += "; InnerEx: " + e.InnerException.Message;
                        e = e.InnerException;
                    }
                    //AppPress.SendEmail(site, AppPress.Settings.SupportEmail, null, "Error occured in sending email", errorMsg, null, null, false);
                    // ignore errors as this thread has to run foreever
                }
                finally
                {
                    site.Close();
                }
            }

        }
        internal static void RequiredValidation(AppPress a)
        {
            var formField = a.fieldValue.formField;
            var fieldValue = a.fieldValue;
            bool reqValid = true;
            if (formField.Static || fieldValue.Hidden == FieldHiddenType.Hidden || formField.Hidden || fieldValue.ReadOnly == FieldReadonlyType.Readonly)
                return;
            // See if any container is Hidden.
            var containerFieldValue = fieldValue.FormData.containerFieldValue;
            while (containerFieldValue != null)
                if (containerFieldValue.Hidden == FieldHiddenType.Hidden)
                    return;
                else
                    containerFieldValue = containerFieldValue.FormData.containerFieldValue;

            switch (formField.Type)
            {
                case FormDefFieldType.Text:
                case FormDefFieldType.TextArea:
                case FormDefFieldType.Password:
                    if (fieldValue.Value == null)
                    {
                        reqValid = false;
                    }
                    break;
                case FormDefFieldType.Number:
                case FormDefFieldType.FileUpload:
                    if (fieldValue.Value == null || fieldValue.Value == "")
                    {
                        reqValid = false;
                    }
                    break;
                case FormDefFieldType.Pickone:
                    if (fieldValue == null || fieldValue.Value == null)
                    {
                        reqValid = false;
                    }
                    break;
                case FormDefFieldType.DateTime:
                    if (String.IsNullOrEmpty(fieldValue.Value))
                    {
                        reqValid = false;
                    }
                    break;
                case FormDefFieldType.PickMultiple:
                    if (fieldValue.Value == null)
                    {
                        reqValid = false;
                    }
                    break;
            }
            if (!reqValid)
            {
                a.appPressResponse.Add(AppPressResponse.FieldError(a.fieldValue, AppPress.GetLocalizationKeyValue("LAKey_RequiredMsg")));
            }
        }
        internal static void RangeValidation(AppPress a)
        {
            var rangeValid = true;
            var formField = a.fieldValue.formField;
            var fieldValue = a.fieldValue;
            switch (formField.Type)
            {
                case FormDefFieldType.Number:
                    if (!String.IsNullOrEmpty(fieldValue.Value))
                    {
                        try
                        {
                            if (formField.MinimumValue != null)
                                rangeValid = Decimal.Parse(fieldValue.Value) >= formField.MinimumValue;
                            if (rangeValid && formField.MaximumValue != null)
                                rangeValid = Decimal.Parse(fieldValue.Value) <= formField.MaximumValue;
                        }
                        catch
                        {
                            a.appPressResponse.Add(AppPressResponse.FieldError(fieldValue, "This Number is not valid"));
                        }
                        if (!rangeValid)
                        {
                            a.appPressResponse.Add(AppPressResponse.FieldError(a.fieldValue, string.Format("Field value should be between {0} and {1}", formField.MinimumValue, formField.MaximumValue)));
                        }
                    }
                    break;
                case FormDefFieldType.DateTime:
                    {
                        string minimumValue = a.TryGetFunctionParameterValue("MinimumValue");
                        string maximumValue = a.TryGetFunctionParameterValue("MaximumValue");
                        if (minimumValue != null)
                            rangeValid = DateTime.Parse(fieldValue.Value, new CultureInfo("en-gb")) >= DateTime.Parse(minimumValue, new CultureInfo("en-gb"));
                        if (rangeValid && maximumValue != null)
                            rangeValid = DateTime.Parse(fieldValue.Value, new CultureInfo("en-gb")) <= DateTime.Parse(maximumValue, new CultureInfo("en-gb"));
                        if (!rangeValid)
                        {
                            a.appPressResponse.Add(AppPressResponse.FieldError(a.fieldValue, string.Format("Field value should be between {0} and {1}", minimumValue, maximumValue)));
                        }
                        break;
                    }
            }
        }
        internal static void EmailValidation(AppPress a)
        {
            var formField = a.fieldValue.formField;
            var fieldValue = a.fieldValue;
            switch (formField.Type)
            {
                case FormDefFieldType.Text:
                    if (fieldValue.Value != null)
                        if (fieldValue.Value.Length > 256)
                        {
                            a.appPressResponse.Add(AppPressResponse.FieldError(a.fieldValue, "Cannot have more than 256 characters"));
                        }
                        else if (fieldValue.Value.Length != 0)
                        {
                            fieldValue.Value = fieldValue.Value.ToLower().Trim();
                            if (!Regex.IsMatch(fieldValue.Value, @"\A(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?)\Z", RegexOptions.IgnoreCase))
                            {
                                a.appPressResponse.Add(AppPressResponse.FieldError(a.fieldValue, "This is not a valid email"));
                            }
                        }
                    break;
                default:
                    throw new Exception("Cannot have Email Validation for non Text fields");
            }
        }

        internal static void RegexValidation(AppPress a, string regexText)
        {
            var rangeValid = true;
            var formField = a.fieldValue.formField;
            var fieldValue = a.fieldValue;
            switch (formField.Type)
            {
                case FormDefFieldType.Text:
                    if (!fieldValue.Value.IsNullOrEmpty())
                    {
                        var regex = new Regex(regexText);
                        rangeValid = regex.IsMatch(fieldValue.Value);
                    }
                    break;
            }
            if (!rangeValid)
            {
                a.appPressResponse.Add(AppPressResponse.FieldError(a.fieldValue, "This field contains invalid characters"));
            }
        }
        // rownum is allowed to be used in the queries. predefine it here
        /*
         * DbCommand cmd = new MySqlCommand("SET @rowNum := -1;", conn, trans);
        dr = cmd.ExecuteReader();
        cmd.Dispose();
        dr.Close();*/

        internal static List<Option> GetOptionsFromQuery(AppPress a, FieldValue fieldValue, string query)
        {
            var options = new List<Option>();

            var dr = a.ExecuteQuery(query);
            if (fieldValue.formField.Style == FormDefFieldStyle.AutoComplete)
            {
                var autoCompleteTerm = ((PickFieldValue)fieldValue).autoCompleteTerm;
                if (autoCompleteTerm != null)
                {
                    var tempquery = "Select ";
                    if (a.site.databaseType == DatabaseType.SqlServer)
                        tempquery += "TOP 7 ";
                    tempquery += " * From (" + query + ") as A Where " + dr.GetName(1) + " like '" + autoCompleteTerm + "%' ";
                    if (a.site.databaseType == DatabaseType.MySql)
                        tempquery += "Limit 7 ";
                    dr.Close();
                    dr = a.ExecuteQuery(tempquery);
                }
            }
            try
            {
                if (dr.FieldCount < 2)
                {
                    var s = fieldValue.GetFieldDescription() + "<br/>Pickone / PickMultiple query should have two columns. First column is Id and second column is DisplayValue";
                    if (AppPress.Settings.developer)
                        s += "<br/>Query:<br/>" + query;
                    throw new Exception(s);
                }
                while (dr.Read())
                {
                    var tempValue = dr.GetString(1);
                    if (!tempValue.IsNullOrEmpty())
                        tempValue = AppPress.GetLocalizationKeyValue(tempValue);
                    if (tempValue == null)
                        dr.GetString(1);
                    options.Add(new Option { id = (dr.IsDBNull(0) ? null : dr[0].ToString()), value = tempValue });
                }
            }
            finally
            {
                dr.Close();
            }
            if (fieldValue.formField.Style == FormDefFieldStyle.DropDown || fieldValue.formField.Style == FormDefFieldStyle.AutoComplete)
            {
                if (fieldValue.Value == null || !fieldValue.formField.Required)
                    if (options.Find(t => t.id == null) == null)
                        options.Insert(0, new Option { id = null, value = "" });
            }
            return options;
        }
        internal static object GetInternalOptions(AppPress a, FormCallType callType)
        {
            switch (callType)
            {
                case FormCallType.GetTableName:
                    return "application.formfields.pickone.options";
            }

            return null;
        }

        internal static List<Option> GetInternalOptions(AppPress a, string id)
        {
            var options = new List<Option>();
            var query = @"
                    SELECT id," + a.SQLQuote + "Option" + a.SQLQuote + @" FROM " + a.SQLQuote + "application.formfields.pickone.options" + a.SQLQuote + @"
                    Where PickoneId=(Select Id From " + a.SQLQuote + "application.formfields.pickone" + a.SQLQuote + " Where SurveyQuestionsId=" + a.fieldValue.formField.dbId + ")";
            var dr = a.ExecuteQuery(query);
            try
            {
                while (dr.Read())
                    options.Add(new Option { id = dr.GetString(0), value = dr.GetString(1) });
            }
            finally
            {
                dr.Close();
            }

            return options;
        }
        internal static void SaveFormInternal(AppPress a)
        {
            a.BeginTrans();
            try
            {
                var surveryFormData = a.fieldValue.FormData;
                surveryFormData.Validate();
                var surveyMasterId = surveryFormData.formDef.dbId;
                var loginUserId = a.sessionData.loginUserId;
                // following will delete answer data also
                a.ExecuteNonQuery("Delete From " + a.SQLQuote + "survey.submit.data" + a.SQLQuote + " Where SurveyMasterId=" + surveyMasterId + " and EmployeeId=" + loginUserId);
                var SurverySubmitDataId = a.ExecuteIdentityInsert(@"
                    INSERT INTO " + a.SQLQuote + "survey.submit.data" + a.SQLQuote + @"(SurveyMasterId,EmployeeId,DateTime)
                    VALUES (" + surveyMasterId + @"," + loginUserId + @",'" + DateTime.Now.ToString(DAOBasic.DBDateTimeFormat) + @"');
                    ", a.SQLQuote + "survey.submit.data" + a.SQLQuote);
                foreach (var fieldValue in surveryFormData.fieldValues)
                {
                    if (fieldValue.formField.dbId < 0)
                        continue;
                    string dateTimeValue = "null", textValue = "null", intValue = "null", decimalValue = "null";
                    if (fieldValue.Value != null)
                    {
                        switch (fieldValue.formField.Type)
                        {
                            case FormDefFieldType.Text:
                            case FormDefFieldType.TextArea:
                                textValue = "'" + a.EscapeSQLString(fieldValue.Value) + "'";
                                break;
                            case FormDefFieldType.DateTime:
                                dateTimeValue = "'" + fieldValue.Value + "'";
                                break;
                            case FormDefFieldType.Pickone:
                            case FormDefFieldType.Checkbox:
                                intValue = fieldValue.Value;
                                break;
                            case FormDefFieldType.Number:
                                decimalValue = fieldValue.Value.ToString();
                                break;
                            case FormDefFieldType.PickMultiple:
                                break;
                        }
                        var id = a.ExecuteIdentityInsert(@"
                            INSERT INTO " + a.SQLQuote + "survey.answer.data" + a.SQLQuote + @"
                            (" + a.SQLQuote + "survey.submit.data.id" + a.SQLQuote + @",SurveyQuestionId,DateTimeValue,TextValue,IntValue,DecimalValue)
                            VALUES(" + SurverySubmitDataId + @"," + fieldValue.formField.dbId + @",
                            " + dateTimeValue + @"," + textValue + @"," + intValue + @"," + decimalValue + @")",
                                "" + a.SQLQuote + "survey.answer.data" + a.SQLQuote + "");
                        if (fieldValue.formField.Type == FormDefFieldType.PickMultiple)
                        {
                            var ids = fieldValue.Value.Split(new char[] { ',' });
                            foreach (var idd in ids)
                                a.ExecuteIdentityInsert(@"
                                    INSERT INTO " + a.SQLQuote + "survey.answer.data.pickmultiple" + a.SQLQuote + @"
                                    (" + a.SQLQuote + "survey.answer.data.id" + a.SQLQuote + "," + a.SQLQuote + "application.formfields.pickone.options.id" + a.SQLQuote + @")
                                    VALUES(" + id + @"," + idd + @")",
                                "" + a.SQLQuote + "survey.answer.data.pickmultiple" + a.SQLQuote + "");
                        }
                    }

                }
                a.ClosePopup();
                a.CommitTrans();
            }
            catch { a.RollbackTrans(); throw; }
        }
        internal static object GenericGetOptionsFromTable(AppPress a, FormCallType callType)
        {
            switch (callType)
            {
                case FormCallType.GetTableName:
                    return a.GetFunctionParameterValue("TableName");
            }

            return null;
        }
        internal static List<Option> GetOptionsFromEnum(AppPress a, string id)
        {
            var options = new List<Option>();
            if (a.fieldValue.formField.Required)
                options.Add(new Option { id = null, value = "" });
            var type = Util.GetType("AppPress.AppLogic+" + a.fieldValue.formField.fieldName);
            foreach (var FormType in Enum.GetValues(type))
                options.Add(new Option { id = ((int)FormType).ToString(), value = AppPress.InsertSpacesBetweenCaps(FormType.ToString()) });
            return options;
        }
        internal static void Redirect(AppPress a)
        {
            var formName = a.serverFunction.Parameters[0].Value;
            var formDef = AppPress.FindFormDef(formName);
            a.Redirect(formDef.id, null, null);
        }
        internal static void GenericGetOptionsFromTableRefreshField(AppPress a)
        {
            a.fieldValue.FormData.GetFieldValue(a.serverFunction.Parameters[0].Value).Value = null;
            RefreshField(a);
        }
        internal static List<Option> GenericGetOptionsFromTable(AppPress a, FieldValue fieldValue)
        {
            var options = new List<Option>();
            var id = fieldValue.Value;
            bool isAutoComplete = fieldValue.formField.Style == FormDefFieldStyle.AutoComplete;// id == null && a.functionCall == "AutoCompleteOptions";
            // check in cache
            var tableName = a.TryGetFunctionParameterValue("TableName");
            var idColumnName = a.TryGetFunctionParameterValue("IdColumnName") ?? "Id";
            var displayColumnName = a.TryGetFunctionParameterValue("DisplayColumnName");

            string FilterClause = null;

            displayColumnName = displayColumnName.Trim();
            if (Regex.IsMatch(displayColumnName, @"^[a-zA-Z]+$"))
                displayColumnName = a.SQLQuote + displayColumnName + a.SQLQuote;
            var sortColumn = a.TryGetFunctionParameterValue("SortColumn") ?? displayColumnName;

            string query = "";
            if (!isAutoComplete || a.autoCompleteTerm != null)
            {
                query += "Select ";
                if (isAutoComplete && a.site.databaseType == DatabaseType.SqlServer)
                    query += " TOP 7 ";
                query += a.SQLQuote + "" + idColumnName + "" + a.SQLQuote + " As AppPressId," + displayColumnName + " From " + a.SQLQuote + "" + tableName + "" + a.SQLQuote + "\n Where 1=1 ";
                if (FilterClause != null)
                {
                    bool foundCall;
                    FilterClause = Util.ExecuteEmbededCalls(a, FilterClause, out foundCall);
                    query += "and (" + FilterClause + ")\n";
                }
                if (isAutoComplete)
                    query += "and " + displayColumnName + " like '" + a.autoCompleteTerm + "%'\n";
                if (isAutoComplete && a.site.databaseType == DatabaseType.MySql)
                    query += "Limit 7\n";
            }
            if (id != null)
            {
                if (!query.IsNullOrEmpty())
                    query += " Union All ";
                query += " Select " + a.SQLQuote + "" + idColumnName + "" + a.SQLQuote + " As AppPressId," + displayColumnName + " From " + a.SQLQuote + "" + tableName + "" + a.SQLQuote + "\n Where " + a.SQLQuote + "" + idColumnName + "" + a.SQLQuote + "= '" + id + "'";
            }
            if (query.IsNullOrEmpty())
                return options;
            query += " Order by " + sortColumn + "\n";
            var dr = a.ExecuteQuery(query);
            try
            {

                while (dr.Read())
                {
                    var s = dr.IsDBNull(1) ? "" : dr[1].ToString();
                    var tempValue = s;
                    if (!tempValue.IsNullOrEmpty())
                        tempValue = AppPress.GetLocalizationKeyValue(tempValue);
                    if (tempValue == null)
                        tempValue = s;
                    options.Add(new Option { id = dr[0].ToString(), value = tempValue });
                }
            }
            finally
            {
                dr.Close();
            }

            return options;
        }

        internal static string GetSavedPickoneValue(AppPress a)
        {
            var formName = a.GetFunctionParameterValue("FormName");
            var pickoneFromgrid = a.GetFunctionParameterValue("PickoneFromgrid");

            var dr = a.ExecuteQuery("select " + pickoneFromgrid + " from " + formName + " where id = (select max(id) from " + formName + ")");
            try
            {
                dr.Read();
                string id = dr.GetString(0);
                var formDef = AppPress.FindFormDef(formName);
                if (a.fieldValue.FormData.id == id && a.fieldValue.Value == "0")
                {
                    var formdata = a.formDatas.Find(t => t.formDefId == formDef.id);
                    formdata.SetFieldValue(pickoneFromgrid, id.ToString());
                    return "1";
                }


            }
            finally
            {
                dr.Close();
            }

            return "0";
        }

        internal static FieldHiddenType HideOnFieldValue(AppPress a)
        {
            string fieldName = a.TryGetFunctionParameterValue("FieldName");
            if (fieldName == null)
                throw new Exception("Could not find Parameter FieldName in FormDef:" + a.fieldValue.FormData.formDef.formName + " Field: " + a.fieldValue.formField.fieldName + " Function:" + a.serverFunction.FunctionName);
            string value = a.TryGetFunctionParameterValue("Value");
            if (value == null)
                throw new Exception("Could not find Parameter Value in FormDef:" + a.fieldValue.FormData.formDef.formName + " Field: " + a.fieldValue.formField.fieldName + " Function:" + a.serverFunction.FunctionName);
            var fieldValue = a.fieldValue.FormData.GetFieldValue(fieldName);
            if (fieldValue == null)
                throw new Exception("Could not find Field: " + fieldName + " in FormDef:" + a.fieldValue.FormData.formDef.formName);
            if (fieldValue.Value == value)
                return FieldHiddenType.Hidden;
            return FieldHiddenType.None;
        }

        internal static Object GetContainerRowForms(AppPress a, FormCallType callType)
        {
            switch (callType)
            {
                case FormCallType.GetContainerRowForm:
                    {
                        long? containerRowFormDefId = null;

                        // Form From XML
                        var containerRowForm = a.TryGetFunctionParameterValue("ContainerRowForm");
                        if (containerRowForm == null)
                            return null;
                        // if formName is Int Value then it was read from DB
                        // TBD: all forms key should be id instead of formName
                        Int64 formId;
                        if (Int64.TryParse(containerRowForm, out formId))
                        {
                            containerRowFormDefId = formId;
                            if (AppPress.LocalInstanceId != 0)
                                containerRowFormDefId = containerRowFormDefId * 10 - AppPress.LocalInstanceId;
                        }
                        else
                        {
                            var formDef = AppPress.FindFormDef(containerRowForm);
                            if (formDef != null)
                                containerRowFormDefId = formDef.id;
                        }
                        return containerRowFormDefId;
                    }
                case FormCallType.GetContainerColumnName:
                    return a.TryGetFunctionParameterValue("ContainerColumnName");
            }
            return null;
        }

        internal static List<FormData> GetContainerRowForms(AppPress a, long formDefId)
        {
            var rowFormDef = AppPress.FindFormDef(formDefId);
            if (rowFormDef == null)
            {
                // when called from GetFormsFromFormField the form may not be defined
                return new List<FormData>();
            }
            var query = rowFormDef.GetViewQuery(a);
            if (query == null)
            {
                // formContainer does not have rows in begining
                return new List<FormData>();
            }

            string containerColumnName = null;
            if (a.serverFunction != null)
                containerColumnName = a.serverFunction.TryGetFunctionParameterValue("ContainerColumnName");
            if (containerColumnName == null)
            {
                var containerFormField = rowFormDef.formFields.Find(t => t.Type == FormDefFieldType.ForeignKey);
                if (containerFormField != null)
                    containerColumnName = containerFormField.fieldName;
            }
            if (containerColumnName != null)
            {
                query += " and " + a.SQLQuote + containerColumnName + a.SQLQuote + "='" + a.EscapeSQLString(a.fieldValue.FormData.id) + "'\n";
            }
            //else
            //{ // Calc will handle if defined
            //    var className = a.fieldValue.formField.GetClassName();
            //    Type fieldType = Util.GetType(className);
            //    if (fieldType != null)
            //    {
            //        var method = Util.GetMethod(a, "Domain", new Type[] { typeof(PageData), fieldType });
            //        if (method != null)
            //            return new List<FormData>();
            //    }

            //}
            // order by Date from if date Range is there as column
            var dateFromColumn = rowFormDef.formFields.Find(t => t.IsDateRange == 1);
            if (dateFromColumn != null)
                query += " Order by " + dateFromColumn.fieldName;
            else if (rowFormDef.PrimaryKey != null)
                query += " Order by " + a.SQLQuote + rowFormDef.PrimaryKey + a.SQLQuote;
            return FormData.ReadFormDatas(a, null, rowFormDef, query);
        }

        internal static List<FormData> GetContainerRowForms(AppPress a)
        {
            var formField = a.fieldValue.formField;
            var formDefId = formField.GetContainerRowForm(a);
            if (formDefId == null)
                throw new Exception("Could not Find Container Row Form for Field: " + formField.fieldName);
            return GetContainerRowForms(a, formDefId.Value);
        }
        internal static Object GetFormsFromFormField(AppPress a, FormCallType callType)
        {
            switch (callType)
            {
                case FormCallType.GetContainerColumnName:
                    return a.TryGetFunctionParameterValue("ContainerColumnName");
            }
            return null;
        }

        internal static List<FormData> GetFormsFromFormField(AppPress a)
        {
            var fieldName = a.GetFunctionParameterValue("FieldName");
            long fieldIdToRefresh;
            FieldValue fieldValue = null;
            if (long.TryParse(fieldName, out fieldIdToRefresh))
                fieldValue = a.fieldValue.FormData.GetFieldValue(fieldIdToRefresh);
            else
                fieldValue = a.fieldValue.FormData.GetFieldValue(fieldName);
            if (fieldValue == null)
                throw new Exception("Could not find Field: :" + fieldName + " in Form: " + a.fieldValue.FormData.formDef.formName + ". The field is used as Parameter in GetFormsFromFormField.");
            if (fieldValue.formField.Type != FormDefFieldType.Pickone)
                throw new Exception("Field: " + fieldName + " used as FieldName can only be of Type Pickone");

            // for saved forms should not change the controlling field
            // if needed can be made non readonly from code
            if (!fieldValue.FormData.IsNew)
                fieldValue.ReadOnly = FieldReadonlyType.Readonly;

            a.AddDependentField(fieldValue, a.fieldValue);

            var formDatas = new List<FormData>();
            if (fieldValue.Value != null)
            {

                var subFormName = ((PickFieldValue)fieldValue).GetOption(a, fieldValue.Value).id;
                if (subFormName != null)
                {
                    var subFormDef = AppPress.FindFormDef(subFormName);
                    if (subFormDef == null)
                    {
                        subFormName = ((PickFieldValue)fieldValue).GetOption(a, fieldValue.Value).value;
                        subFormDef = AppPress.FindFormDef(subFormName);
                    }
                    if (subFormDef != null)
                    {
                        var containerIdFormField = subFormDef.formFields.Find(t => t.Type == FormDefFieldType.ForeignKey);
                        if (subFormDef.TableName != null && containerIdFormField == null)
                            throw new Exception("Could not Find field of Type ForeignKey in Form:" + subFormDef.formName);
                        var subFormDefId = subFormDef.id;
                        formDatas.AddRange(a.formDatas.FindAll(t => t.containerFieldValue == a.fieldValue && t.formDefId == subFormDefId));
                        if (formDatas.Count == 0)
                            formDatas = GetContainerRowForms(a, subFormDefId);
                        if (formDatas.Count == 0)
                        {
                            var formData = FormData.NewFormData(a, subFormDef, a.fieldValue);
                            formDatas.Add(formData);
                        }
                    }
                }
                foreach (var formData in a.formDatas.FindAll(t => t.containerFieldValue == a.fieldValue && t.formDef.formName != subFormName))
                    Util.ApplyOnChildFormDatas(a.formDatas, formData, t => t.IsDeleted = true);

            }
            return formDatas;
        }

        internal static void DeleteFormField(AppPress a, string fieldId, Int64 typeId)
        {
            Int64 existingTypeId = -1;
            Int64 formId = -1;
            string fieldName = string.Empty;
            IDataReader dr = a.ExecuteQuery("Select type, formId, fieldName from apppress_fields where Id = " + fieldId + "");
            try
            {
                if (dr.Read())
                {
                    existingTypeId = dr.GetInt64(0);
                    formId = dr.IsDBNull(1) ? -1 : dr.GetInt64(1);
                    fieldName = dr.GetString(2);
                }
            }
            finally
            {
                dr.Close();
            }
            if (existingTypeId != typeId && existingTypeId != -1)
            {
                string tableName = "appPress_";
                dr = a.ExecuteQuery("Select FieldType from apppress_fieldtype where Id = " + existingTypeId);
                try
                {
                    if (dr.Read())
                        tableName += dr.GetString(0);
                }
                finally
                {
                    dr.Close();
                }
                a.ExecuteNonQuery("Delete from " + tableName + " where FieldId = " + fieldId + "");
            }
        }

        internal static List<Option> GetPickoneFieldNamesOfContainerRowForm(AppPress a, string id)
        {

            var options = new List<Option>();
            options.Add(new Option { id = "0", value = "" });

            return options;
        }

        internal static void RefreshContainer(AppPress a)
        {
            string fieldNameToRefresh = a.fieldValue.formField.containerFormField.fieldName;
            // find fieldName to Refresh in a.fieldValueFormData or any of its parent
            var formData = a.fieldValue.FormData;
            var fieldValue = formData.GetFieldValue(fieldNameToRefresh);
            a.appPressResponse.Add(AppPressResponse.RefreshField(a, fieldValue, true));
        }

        internal static void SortContainer(AppPress a)
        {
            string fieldNameToRefresh = a.fieldValue.formField.containerFormField.fieldName;
            // find fieldName to Refresh in a.fieldValueFormData or any of its parent
            var formData = a.fieldValue.FormData;
            // remove sorting of other fields
            foreach (var fieldValue1 in formData.fieldValues)
                if (fieldValue1.formField.IsSortingControl())
                    if (a.fieldValue.formField.id != fieldValue1.formField.id)
                        fieldValue1.Value = null;
            var fieldValue = formData.GetFieldValue(fieldNameToRefresh);
            a.appPressResponse.Add(AppPressResponse.RefreshField(a, fieldValue, true));
        }

        internal static void RefreshField(AppPress a)
        {
            var fieldName = a.GetFunctionParameterValue("FieldName");
            long fieldIdToRefresh;
            FieldValue fieldValue = null;
            var formData = a.fieldValue.FormData;
            if (long.TryParse(fieldName, out fieldIdToRefresh))
            {
                FormField formField;
                while (true)
                {
                    formField = formData.formDef.formFields.Find(t => t.id == fieldIdToRefresh);
                    if (formField != null)
                        break;
                    if (formData.containerFieldValue == null)
                        throw new Exception("Refresh Field: Could not find Field with Id:" + fieldIdToRefresh);
                    formData = formData.containerFieldValue.FormData;
                }
                fieldValue = formData.GetFieldValue(fieldIdToRefresh);
                if (fieldValue == null)
                {
                    // this field may not be submitted from Client as it is Static. Create a new Field
                    var className = formField.GetClassName();
                    fieldValue = (FieldValue)Util.CreateInstance(className);
                    if (fieldValue == null)
                        fieldValue = new FieldValue();

                    fieldValue.formField = formField;
                    fieldValue.FormData = formData;
                    fieldValue.NotFromClient = true;
                    formData.fieldValues.Add(fieldValue);
                }

            }
            else
                fieldValue = formData.GetFieldValue(fieldName);
            a.appPressResponse.Add(AppPressResponse.RefreshField(a, fieldValue, true));
        }

        internal static void GenericRefreshPage(AppPress a)
        {
            a.appPressResponse.Add(AppPressResponse.PageRefresh());
        }

        internal static void GenericSelectAllRows(AppPress a)
        {
            var gridRowFormDef = a.fieldValue.formField.containerFormField.GetContainerRowFormDef(a);
            a.appPressResponse.Add(AppPressResponse.SetFieldValue((long)gridRowFormDef.id, null, gridRowFormDef.GetFormField(a.fieldValue.formField.containerColumnName), a.fieldValue.Value));
        }

        internal static void UnSelectOthers(AppPress a)
        {
            var gridRowFormDef = a.fieldValue.FormData.formDef;
            // unselect other checked rows
            foreach (var rFormData in a.formDatas.FindAll(t => t.formDefId == gridRowFormDef.id && t.id != a.fieldValue.FormData.id && t.GetFieldInt("SelectRow") == 1))
                a.appPressResponse.Add(AppPressResponse.SetFieldValue(gridRowFormDef.id, rFormData.id, gridRowFormDef.GetFormField("SelectRow"), "0"));
        }

        internal static void SaveSelectionInDB(AppPress a)
        {
            a.BeginTrans();
            try
            {
                SaveForm(a);
                var formContainerFieldValue = a.fieldValue.FormData.GetFieldValue(a.fieldValue.formField.containerFormField.fieldName);
                var selectedFormDatas = a.formDatas.FindAll(t => t.containerFieldValue == formContainerFieldValue && t.GetFieldInt("SelectRow") == 1);
                var fieldNameToSet = a.GetFunctionParameterValue("FieldName");
                var callerFormData = a.fieldValue.FormData;
                var fieldValueToSet = callerFormData.GetFieldValue(fieldNameToSet);
                if (selectedFormDatas != null)
                {
                    foreach (FormData selectedFormData in selectedFormDatas)
                    {
                        //insert data to AppPress_SaveSelectionInDB
                        string qry = "Insert Into AppPress_SaveSelectionInDB(GridId,SelectFieldId) Values (" + a.fieldValue.FormData.id + "," + selectedFormData.id + ")";
                        a.ExecuteNonQuery(qry);
                    }
                }
                a.CommitTrans();
            }
            catch (Exception) { a.RollbackTrans(); throw; }
        }
        internal static string GetFileType(string strFileExt)
        {

            string retval = "";
            switch (strFileExt.ToLower())
            {
                case ".3dm": retval = "x-world/x-3dmf"; break;
                case ".3dmf": retval = "x-world/x-3dmf"; break;
                case ".a": retval = "application/octet-stream"; break;
                case ".aab": retval = "application/x-authorware-bin"; break;
                case ".aam": retval = "application/x-authorware-map"; break;
                case ".aas": retval = "application/x-authorware-seg"; break;
                case ".abc": retval = "text/vnd.abc"; break;
                case ".acgi": retval = "text/html"; break;
                case ".afl": retval = "video/animaflex"; break;
                case ".ai": retval = "application/postscript"; break;
                case ".aif": retval = "audio/aiff"; break;
                case ".aifc": retval = "audio/aiff"; break;
                case ".aiff": retval = "audio/aiff"; break;
                case ".aim": retval = "application/x-aim"; break;
                case ".aip": retval = "text/x-audiosoft-intra"; break;
                case ".ani": retval = "application/x-navi-animation"; break;
                case ".aos": retval = "application/x-nokia-9000-communicator-add-on-software"; break;
                case ".aps": retval = "application/mime"; break;
                case ".arc": retval = "application/octet-stream"; break;
                case ".arj": retval = "application/arj"; break;
                case ".art": retval = "image/x-jg"; break;
                case ".asf": retval = "video/x-ms-asf"; break;
                case ".asm": retval = "text/x-asm"; break;
                case ".asp": retval = "text/asp"; break;
                case ".asx": retval = "video/x-ms-asf"; break;
                case ".au": retval = "audio/basic"; break;
                case ".avi": retval = "video/avi"; break;
                case ".avs": retval = "video/avs-video"; break;
                case ".bcpio": retval = "application/x-bcpio"; break;
                case ".bin": retval = "application/octet-stream"; break;
                case ".bm": retval = "image/bmp"; break;
                case ".bmp": retval = "image/bmp"; break;
                case ".boo": retval = "application/book"; break;
                case ".book": retval = "application/book"; break;
                case ".boz": retval = "application/x-bzip2"; break;
                case ".bsh": retval = "application/x-bsh"; break;
                case ".bz": retval = "application/x-bzip"; break;
                case ".bz2": retval = "application/x-bzip2"; break;
                case ".c": retval = "text/plain"; break;
                case ".c++": retval = "text/plain"; break;
                case ".cat": retval = "application/vnd.ms-pki.seccat"; break;
                case ".cc": retval = "text/plain"; break;
                case ".ccad": retval = "application/clariscad"; break;
                case ".cco": retval = "application/x-cocoa"; break;
                case ".cdf": retval = "application/cdf"; break;
                case ".cer": retval = "application/pkix-cert"; break;
                case ".cha": retval = "application/x-chat"; break;
                case ".chat": retval = "application/x-chat"; break;
                case ".class": retval = "application/java"; break;
                case ".com": retval = "application/octet-stream"; break;
                case ".conf": retval = "text/plain"; break;
                case ".cpio": retval = "application/x-cpio"; break;
                case ".cpp": retval = "text/x-c"; break;
                case ".cpt": retval = "application/x-cpt"; break;
                case ".crl": retval = "application/pkcs-crl"; break;
                case ".crt": retval = "application/pkix-cert"; break;
                case ".csh": retval = "application/x-csh"; break;
                case ".css": retval = "text/css"; break;
                case ".cxx": retval = "text/plain"; break;
                case ".dcr": retval = "application/x-director"; break;
                case ".deepv": retval = "application/x-deepv"; break;
                case ".def": retval = "text/plain"; break;
                case ".der": retval = "application/x-x509-ca-cert"; break;
                case ".dif": retval = "video/x-dv"; break;
                case ".dir": retval = "application/x-director"; break;
                case ".dl": retval = "video/dl"; break;
                case ".doc": retval = "application/msword"; break;
                case ".docm": retval = "application/vnd.ms-word.document.macroEnabled.12"; break;
                case ".docx": retval = "application/vnd.openxmlformats-officedocument.wordprocessingml.document"; break;
                case ".dotm": retval = "application/vnd.ms-word.template.macroEnabled.12"; break;
                case ".dot": retval = "application/msword"; break;
                case ".dotx": retval = "application/vnd.openxmlformats-officedocument.wordprocessingml.template"; break;
                case ".dp": retval = "application/commonground"; break;
                case ".drw": retval = "application/drafting"; break;
                case ".dump": retval = "application/octet-stream"; break;
                case ".dv": retval = "video/x-dv"; break;
                case ".dvi": retval = "application/x-dvi"; break;
                case ".dwf": retval = "model/vnd.dwf"; break;
                case ".dwg": retval = "image/vnd.dwg"; break;
                case ".dxf": retval = "image/vnd.dwg"; break;
                case ".dxr": retval = "application/x-director"; break;
                case ".el": retval = "text/x-script.elisp"; break;
                case ".elc": retval = "application/x-elc"; break;
                case ".env": retval = "application/x-envoy"; break;
                case ".eps": retval = "application/postscript"; break;
                case ".es": retval = "application/x-esrehber"; break;
                case ".etx": retval = "text/x-setext"; break;
                case ".evy": retval = "application/envoy"; break;
                case ".exe": retval = "application/octet-stream"; break;
                case ".f": retval = "text/plain"; break;
                case ".f77": retval = "text/x-fortran"; break;
                case ".f90": retval = "text/plain"; break;
                case ".fdf": retval = "application/vnd.fdf"; break;
                case ".fif": retval = "image/fif"; break;
                case ".fli": retval = "video/fli"; break;
                case ".flo": retval = "image/florian"; break;
                case ".flx": retval = "text/vnd.fmi.flexstor"; break;
                case ".fmf": retval = "video/x-atomic3d-feature"; break;
                case ".for": retval = "text/x-fortran"; break;
                case ".fpx": retval = "image/vnd.fpx"; break;
                case ".frl": retval = "application/freeloader"; break;
                case ".funk": retval = "audio/make"; break;
                case ".g": retval = "text/plain"; break;
                case ".g3": retval = "image/g3fax"; break;
                case ".gif": retval = "image/gif"; break;
                case ".gl": retval = "video/gl"; break;
                case ".gsd": retval = "audio/x-gsm"; break;
                case ".gsm": retval = "audio/x-gsm"; break;
                case ".gsp": retval = "application/x-gsp"; break;
                case ".gss": retval = "application/x-gss"; break;
                case ".gtar": retval = "application/x-gtar"; break;
                case ".gz": retval = "application/x-gzip"; break;
                case ".gzip": retval = "application/x-gzip"; break;
                case ".h": retval = "text/plain"; break;
                case ".hdf": retval = "application/x-hdf"; break;
                case ".help": retval = "application/x-helpfile"; break;
                case ".hgl": retval = "application/vnd.hp-hpgl"; break;
                case ".hh": retval = "text/plain"; break;
                case ".hlb": retval = "text/x-script"; break;
                case ".hlp": retval = "application/hlp"; break;
                case ".hpg": retval = "application/vnd.hp-hpgl"; break;
                case ".hpgl": retval = "application/vnd.hp-hpgl"; break;
                case ".hqx": retval = "application/binhex"; break;
                case ".hta": retval = "application/hta"; break;
                case ".htc": retval = "text/x-component"; break;
                case ".htm": retval = "text/html"; break;
                case ".html": retval = "text/html"; break;
                case ".htmls": retval = "text/html"; break;
                case ".htt": retval = "text/webviewhtml"; break;
                case ".htx": retval = "text/html"; break;
                case ".ice": retval = "x-conference/x-cooltalk"; break;
                case ".ico": retval = "image/x-icon"; break;
                case ".idc": retval = "text/plain"; break;
                case ".ief": retval = "image/ief"; break;
                case ".iefs": retval = "image/ief"; break;
                case ".iges": retval = "application/iges"; break;
                case ".igs": retval = "application/iges"; break;
                case ".ima": retval = "application/x-ima"; break;
                case ".imap": retval = "application/x-httpd-imap"; break;
                case ".inf": retval = "application/inf"; break;
                case ".ins": retval = "application/x-internett-signup"; break;
                case ".ip": retval = "application/x-ip2"; break;
                case ".isu": retval = "video/x-isvideo"; break;
                case ".it": retval = "audio/it"; break;
                case ".iv": retval = "application/x-inventor"; break;
                case ".ivr": retval = "i-world/i-vrml"; break;
                case ".ivy": retval = "application/x-livescreen"; break;
                case ".jam": retval = "audio/x-jam"; break;
                case ".jav": retval = "text/plain"; break;
                case ".java": retval = "text/plain"; break;
                case ".jcm": retval = "application/x-java-commerce"; break;
                case ".jfif": retval = "image/jpeg"; break;
                case ".jfif-tbnl": retval = "image/jpeg"; break;
                case ".jpe": retval = "image/jpeg"; break;
                case ".jpeg": retval = "image/jpeg"; break;
                case ".jpg": retval = "image/jpeg"; break;
                case ".jps": retval = "image/x-jps"; break;
                case ".js": retval = "application/x-javascript"; break;
                case ".jut": retval = "image/jutvision"; break;
                case ".kar": retval = "audio/midi"; break;
                case ".ksh": retval = "application/x-ksh"; break;
                case ".la": retval = "audio/nspaudio"; break;
                case ".lam": retval = "audio/x-liveaudio"; break;
                case ".latex": retval = "application/x-latex"; break;
                case ".lha": retval = "application/octet-stream"; break;
                case ".lhx": retval = "application/octet-stream"; break;
                case ".list": retval = "text/plain"; break;
                case ".lma": retval = "audio/nspaudio"; break;
                case ".log": retval = "text/plain"; break;
                case ".lsp": retval = "application/x-lisp"; break;
                case ".lst": retval = "text/plain"; break;
                case ".lsx": retval = "text/x-la-asf"; break;
                case ".ltx": retval = "application/x-latex"; break;
                case ".lzh": retval = "application/octet-stream"; break;
                case ".lzx": retval = "application/octet-stream"; break;
                case ".m": retval = "text/plain"; break;
                case ".m1v": retval = "video/mpeg"; break;
                case ".m2a": retval = "audio/mpeg"; break;
                case ".m2v": retval = "video/mpeg"; break;
                case ".m3u": retval = "audio/x-mpequrl"; break;
                case ".man": retval = "application/x-troff-man"; break;
                case ".map": retval = "application/x-navimap"; break;
                case ".mar": retval = "text/plain"; break;
                case ".mbd": retval = "application/mbedlet"; break;
                case ".mc$": retval = "application/x-magic-cap-package-1.0"; break;
                case ".mcd": retval = "application/mcad"; break;
                case ".mcf": retval = "text/mcf"; break;
                case ".mcp": retval = "application/netmc"; break;
                case ".me": retval = "application/x-troff-me"; break;
                case ".mht": retval = "message/rfc822"; break;
                case ".mhtml": retval = "message/rfc822"; break;
                case ".mid": retval = "audio/midi"; break;
                case ".midi": retval = "audio/midi"; break;
                case ".mif": retval = "application/x-mif"; break;
                case ".mime": retval = "message/rfc822"; break;
                case ".mjf": retval = "audio/x-vnd.audioexplosion.mjuicemediafile"; break;
                case ".mjpg": retval = "video/x-motion-jpeg"; break;
                case ".mm": retval = "application/base64"; break;
                case ".mme": retval = "application/base64"; break;
                case ".mod": retval = "audio/mod"; break;
                case ".moov": retval = "video/quicktime"; break;
                case ".mov": retval = "video/quicktime"; break;
                case ".movie": retval = "video/x-sgi-movie"; break;
                case ".mp2": retval = "audio/mpeg"; break;
                case ".mp3": retval = "audio/mpeg"; break;
                case ".mpa": retval = "audio/mpeg"; break;
                case ".mpc": retval = "application/x-project"; break;
                case ".mpe": retval = "video/mpeg"; break;
                case ".mpeg": retval = "video/mpeg"; break;
                case ".mpg": retval = "video/mpeg"; break;
                case ".mpga": retval = "audio/mpeg"; break;
                case ".mpp": retval = "application/vnd.ms-project"; break;
                case ".mpt": retval = "application/vnd.ms-project"; break;
                case ".mpv": retval = "application/vnd.ms-project"; break;
                case ".mpx": retval = "application/vnd.ms-project"; break;
                case ".mrc": retval = "application/marc"; break;
                case ".ms": retval = "application/x-troff-ms"; break;
                case ".mv": retval = "video/x-sgi-movie"; break;
                case ".my": retval = "audio/make"; break;
                case ".mzz": retval = "application/x-vnd.audioexplosion.mzz"; break;
                case ".nap": retval = "image/naplps"; break;
                case ".naplps": retval = "image/naplps"; break;
                case ".nc": retval = "application/x-netcdf"; break;
                case ".ncm": retval = "application/vnd.nokia.configuration-message"; break;
                case ".nif": retval = "image/x-niff"; break;
                case ".niff": retval = "image/x-niff"; break;
                case ".nix": retval = "application/x-mix-transfer"; break;
                case ".nsc": retval = "application/x-conference"; break;
                case ".nvd": retval = "application/x-navidoc"; break;
                case ".o": retval = "application/octet-stream"; break;
                case ".oda": retval = "application/oda"; break;
                case ".omc": retval = "application/x-omc"; break;
                case ".omcd": retval = "application/x-omcdatamaker"; break;
                case ".omcr": retval = "application/x-omcregerator"; break;
                case ".p": retval = "text/x-pascal"; break;
                case ".p10": retval = "application/pkcs10"; break;
                case ".p12": retval = "application/pkcs-12"; break;
                case ".p7a": retval = "application/x-pkcs7-signature"; break;
                case ".p7c": retval = "application/pkcs7-mime"; break;
                case ".p7m": retval = "application/pkcs7-mime"; break;
                case ".p7r": retval = "application/x-pkcs7-certreqresp"; break;
                case ".p7s": retval = "application/pkcs7-signature"; break;
                case ".part": retval = "application/pro_eng"; break;
                case ".pas": retval = "text/pascal"; break;
                case ".pbm": retval = "image/x-portable-bitmap"; break;
                case ".pcl": retval = "application/vnd.hp-pcl"; break;
                case ".pct": retval = "image/x-pict"; break;
                case ".pcx": retval = "image/x-pcx"; break;
                case ".pdb": retval = "chemical/x-pdb"; break;
                case ".pdf": retval = "application/pdf"; break;
                case ".pfunk": retval = "audio/make"; break;
                case ".pgm": retval = "image/x-portable-greymap"; break;
                case ".pic": retval = "image/pict"; break;
                case ".pict": retval = "image/pict"; break;
                case ".pkg": retval = "application/x-newton-compatible-pkg"; break;
                case ".pko": retval = "application/vnd.ms-pki.pko"; break;
                case ".pl": retval = "text/plain"; break;
                case ".plx": retval = "application/x-pixclscript"; break;
                case ".pm": retval = "image/x-xpixmap"; break;
                case ".pm4": retval = "application/x-pagemaker"; break;
                case ".pm5": retval = "application/x-pagemaker"; break;
                case ".png": retval = "image/png"; break;
                case ".pnm": retval = "application/x-portable-anymap"; break;
                case ".pot": retval = "application/vnd.ms-powerpoint"; break;
                case ".potm": retval = "application/vnd.ms-powerpoint.template.macroEnabled.12"; break;
                case ".potx": retval = "application/vnd.openxmlformats-officedocument.presentationml.template"; break;
                case ".pov": retval = "model/x-pov"; break;
                case ".ppa": retval = "application/vnd.ms-powerpoint"; break;
                case ".ppam": retval = "application/vnd.ms-powerpoint.addin.macroEnabled.12"; break;
                case ".ppm": retval = "image/x-portable-pixmap"; break;
                case ".pps": retval = "application/vnd.ms-powerpoint"; break;
                case ".ppsm": retval = "application/vnd.ms-powerpoint.slideshow.macroEnabled.12"; break;
                case ".ppsx": retval = "application/vnd.openxmlformats-officedocument.presentationml.slideshow"; break;
                case ".ppt": retval = "application/vnd.ms-powerpoint"; break;
                case ".pptm": retval = "application/vnd.ms-powerpoint.presentation.macroEnabled.12"; break;
                case ".pptx": retval = "application/vnd.openxmlformats-officedocument.presentationml.presentation"; break;
                case ".ppz": retval = "application/vnd.ms-powerpoint"; break;
                case ".pre": retval = "application/x-freelance"; break;
                case ".prt": retval = "application/pro_eng"; break;
                case ".ps": retval = "application/postscript"; break;
                case ".psd": retval = "application/octet-stream"; break;
                case ".pvu": retval = "paleovu/x-pv"; break;
                case ".pwz": retval = "application/vnd.ms-powerpoint"; break;
                case ".py": retval = "text/x-script.phyton"; break;
                case ".pyc": retval = "applicaiton/x-bytecode.python"; break;
                case ".qcp": retval = "audio/vnd.qcelp"; break;
                case ".qd3": retval = "x-world/x-3dmf"; break;
                case ".qd3d": retval = "x-world/x-3dmf"; break;
                case ".qif": retval = "image/x-quicktime"; break;
                case ".qt": retval = "video/quicktime"; break;
                case ".qtc": retval = "video/x-qtc"; break;
                case ".qti": retval = "image/x-quicktime"; break;
                case ".qtif": retval = "image/x-quicktime"; break;
                case ".ra": retval = "audio/x-pn-realaudio"; break;
                case ".ram": retval = "audio/x-pn-realaudio"; break;
                case ".ras": retval = "application/x-cmu-raster"; break;
                case ".rast": retval = "image/cmu-raster"; break;
                case ".rexx": retval = "text/x-script.rexx"; break;
                case ".rf": retval = "image/vnd.rn-realflash"; break;
                case ".rgb": retval = "image/x-rgb"; break;
                case ".rm": retval = "application/vnd.rn-realmedia"; break;
                case ".rmi": retval = "audio/mid"; break;
                case ".rmm": retval = "audio/x-pn-realaudio"; break;
                case ".rmp": retval = "audio/x-pn-realaudio"; break;
                case ".rng": retval = "application/ringing-tones"; break;
                case ".rnx": retval = "application/vnd.rn-realplayer"; break;
                case ".roff": retval = "application/x-troff"; break;
                case ".rp": retval = "image/vnd.rn-realpix"; break;
                case ".rpm": retval = "audio/x-pn-realaudio-plugin"; break;
                case ".rt": retval = "text/richtext"; break;
                case ".rtf": retval = "text/richtext"; break;
                case ".rtx": retval = "text/richtext"; break;
                case ".rv": retval = "video/vnd.rn-realvideo"; break;
                case ".s": retval = "text/x-asm"; break;
                case ".s3m": retval = "audio/s3m"; break;
                case ".saveme": retval = "application/octet-stream"; break;
                case ".sbk": retval = "application/x-tbook"; break;
                case ".scm": retval = "application/x-lotusscreencam"; break;
                case ".sdml": retval = "text/plain"; break;
                case ".sdp": retval = "application/sdp"; break;
                case ".sdr": retval = "application/sounder"; break;
                case ".sea": retval = "application/sea"; break;
                case ".set": retval = "application/set"; break;
                case ".sgm": retval = "text/sgml"; break;
                case ".sgml": retval = "text/sgml"; break;
                case ".sh": retval = "application/x-sh"; break;
                case ".shar": retval = "application/x-shar"; break;
                case ".shtml": retval = "text/html"; break;
                case ".sid": retval = "audio/x-psid"; break;
                case ".sit": retval = "application/x-sit"; break;
                case ".skd": retval = "application/x-koan"; break;
                case ".skm": retval = "application/x-koan"; break;
                case ".skp": retval = "application/x-koan"; break;
                case ".skt": retval = "application/x-koan"; break;
                case ".sl": retval = "application/x-seelogo"; break;
                case ".smi": retval = "application/smil"; break;
                case ".smil": retval = "application/smil"; break;
                case ".snd": retval = "audio/basic"; break;
                case ".sol": retval = "application/solids"; break;
                case ".spc": retval = "text/x-speech"; break;
                case ".spl": retval = "application/futuresplash"; break;
                case ".spr": retval = "application/x-sprite"; break;
                case ".sprite": retval = "application/x-sprite"; break;
                case ".src": retval = "application/x-wais-source"; break;
                case ".ssi": retval = "text/x-server-parsed-html"; break;
                case ".ssm": retval = "application/streamingmedia"; break;
                case ".sst": retval = "application/vnd.ms-pki.certstore"; break;
                case ".step": retval = "application/step"; break;
                case ".stl": retval = "application/sla"; break;
                case ".stp": retval = "application/step"; break;
                case ".sv4cpio": retval = "application/x-sv4cpio"; break;
                case ".sv4crc": retval = "application/x-sv4crc"; break;
                case ".svf": retval = "image/vnd.dwg"; break;
                case ".svr": retval = "application/x-world"; break;
                case ".swf": retval = "application/x-shockwave-flash"; break;
                case ".t": retval = "application/x-troff"; break;
                case ".talk": retval = "text/x-speech"; break;
                case ".tar": retval = "application/x-tar"; break;
                case ".tbk": retval = "application/toolbook"; break;
                case ".tcl": retval = "application/x-tcl"; break;
                case ".tcsh": retval = "text/x-script.tcsh"; break;
                case ".tex": retval = "application/x-tex"; break;
                case ".texi": retval = "application/x-texinfo"; break;
                case ".texinfo": retval = "application/x-texinfo"; break;
                case ".text": retval = "text/plain"; break;
                case ".tgz": retval = "application/x-compressed"; break;
                case ".tif": retval = "image/tiff"; break;
                case ".tiff": retval = "image/tiff"; break;
                case ".tr": retval = "application/x-troff"; break;
                case ".tsi": retval = "audio/tsp-audio"; break;
                case ".tsp": retval = "application/dsptype"; break;
                case ".tsv": retval = "text/tab-separated-values"; break;
                case ".turbot": retval = "image/florian"; break;
                case ".txt": retval = "text/plain"; break;
                case ".uil": retval = "text/x-uil"; break;
                case ".uni": retval = "text/uri-list"; break;
                case ".unis": retval = "text/uri-list"; break;
                case ".unv": retval = "application/i-deas"; break;
                case ".uri": retval = "text/uri-list"; break;
                case ".uris": retval = "text/uri-list"; break;
                case ".ustar": retval = "application/x-ustar"; break;
                case ".uu": retval = "application/octet-stream"; break;
                case ".uue": retval = "text/x-uuencode"; break;
                case ".vcd": retval = "application/x-cdlink"; break;
                case ".vcs": retval = "text/x-vcalendar"; break;
                case ".vda": retval = "application/vda"; break;
                case ".vdo": retval = "video/vdo"; break;
                case ".vew": retval = "application/groupwise"; break;
                case ".viv": retval = "video/vivo"; break;
                case ".vivo": retval = "video/vivo"; break;
                case ".vmd": retval = "application/vocaltec-media-desc"; break;
                case ".vmf": retval = "application/vocaltec-media-file"; break;
                case ".voc": retval = "audio/voc"; break;
                case ".vos": retval = "video/vosaic"; break;
                case ".vox": retval = "audio/voxware"; break;
                case ".vqe": retval = "audio/x-twinvq-plugin"; break;
                case ".vqf": retval = "audio/x-twinvq"; break;
                case ".vql": retval = "audio/x-twinvq-plugin"; break;
                case ".vrml": retval = "application/x-vrml"; break;
                case ".vrt": retval = "x-world/x-vrt"; break;
                case ".vsd": retval = "application/x-visio"; break;
                case ".vst": retval = "application/x-visio"; break;
                case ".vsw": retval = "application/x-visio"; break;
                case ".w60": retval = "application/wordperfect6.0"; break;
                case ".w61": retval = "application/wordperfect6.1"; break;
                case ".w6w": retval = "application/msword"; break;
                case ".wav": retval = "audio/wav"; break;
                case ".wb1": retval = "application/x-qpro"; break;
                case ".wbmp": retval = "image/vnd.wap.wbmp"; break;
                case ".web": retval = "application/vnd.xara"; break;
                case ".wiz": retval = "application/msword"; break;
                case ".wk1": retval = "application/x-123"; break;
                case ".wmf": retval = "windows/metafile"; break;
                case ".wml": retval = "text/vnd.wap.wml"; break;
                case ".wmlc": retval = "application/vnd.wap.wmlc"; break;
                case ".wmls": retval = "text/vnd.wap.wmlscript"; break;
                case ".wmlsc": retval = "application/vnd.wap.wmlscriptc"; break;
                case ".wmv": retval = "video/x-ms-wmv"; break;
                case ".word": retval = "application/msword"; break;
                case ".wp": retval = "application/wordperfect"; break;
                case ".wp5": retval = "application/wordperfect"; break;
                case ".wp6": retval = "application/wordperfect"; break;
                case ".wpd": retval = "application/wordperfect"; break;
                case ".wq1": retval = "application/x-lotus"; break;
                case ".wri": retval = "application/mswrite"; break;
                case ".wrl": retval = "application/x-world"; break;
                case ".wrz": retval = "x-world/x-vrml"; break;
                case ".wsc": retval = "text/scriplet"; break;
                case ".wsrc": retval = "application/x-wais-source"; break;
                case ".wtk": retval = "application/x-wintalk"; break;
                case ".xbm": retval = "image/x-xbitmap"; break;
                case ".xdr": retval = "video/x-amt-demorun"; break;
                case ".xgz": retval = "xgl/drawing"; break;
                case ".xif": retval = "image/vnd.xiff"; break;
                case ".xl": retval = "application/excel"; break;
                case ".xla": retval = "application/vnd.ms-excel"; break;
                case ".xlam": retval = "application/vnd.ms-excel.addin.macroEnabled.12"; break;
                case ".xlb": retval = "application/vnd.ms-excel"; break;
                case ".xlc": retval = "application/vnd.ms-excel"; break;
                case ".xld": retval = "application/vnd.ms-excel"; break;
                case ".xlk": retval = "application/vnd.ms-excel"; break;
                case ".xll": retval = "application/vnd.ms-excel"; break;
                case ".xlm": retval = "application/vnd.ms-excel"; break;
                case ".xls": retval = "application/vnd.ms-excel"; break;
                case ".xlsb": retval = "application/vnd.ms-excel.sheet.binary.macroEnabled.12"; break;
                case ".xlsm": retval = "application/vnd.ms-excel.sheet.macroEnabled.12"; break;
                case ".xlt": retval = "application/vnd.ms-excel"; break;
                case ".xlv": retval = "application/vnd.ms-excel"; break;
                case ".xlw": retval = "application/vnd.ms-excel"; break;
                case ".xlsx": retval = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"; break;
                case ".xltm": retval = "application/vnd.ms-excel.template.macroEnabled.12"; break;
                case ".xltx": retval = "application/vnd.openxmlformats-officedocument.spreadsheetml.template"; break;
                case ".xm": retval = "audio/xm"; break;
                case ".xml": retval = "application/xml"; break;
                case ".xmz": retval = "xgl/movie"; break;
                case ".xpix": retval = "application/x-vnd.ls-xpix"; break;
                case ".xpm": retval = "image/xpm"; break;
                case ".x-png": retval = "image/png"; break;
                case ".xsr": retval = "video/x-amt-showrun"; break;
                case ".xwd": retval = "image/x-xwd"; break;
                case ".xyz": retval = "chemical/x-pdb"; break;
                case ".z": retval = "application/x-compressed"; break;
                case ".zip": retval = "application/zip"; break;
                case ".zoo": retval = "application/octet-stream"; break;
                case ".zsh": retval = "text/x-script.zsh"; break;
                default: retval = "application/octet-stream"; break;
            }
            return retval;
        }

        internal static void MultiFileUploadDownloadFile(AppPress a)
        {
            var imageId = a.fieldValue.FormData.fieldValues.Find(t => t.formField.Hidden && t.formField.OriginalType == (int)FormDefFieldType.MultiFileUpload).Value;
            a.DownloadFile(int.Parse(imageId));
        }
        internal static void MultiFileUploadPreview(AppPress a)
        {
            var fileIDFormField = a.fieldValue.FormData.fieldValues.Find(t => t.formField.Hidden && t.formField.OriginalType == (int)FormDefFieldType.MultiFileUpload);
            var imageId = fileIDFormField.Value;
            var fileType = a.ExecuteString("Select FileType From application_files Where Id=" + imageId);
            if (fileType != null && fileType.ToLower().Contains("image"))
                a.fieldValue.FieldLabel = "<img src='" + a.GetFileUploadImageUrl(int.Parse(imageId)) + "' class='AppPressFilePreview'></img>";
            else
            {
                var fileName = a.ExecuteString("Select FileName From application_files Where Id=" + imageId);
                a.fieldValue.FieldLabel = fileName;
            }
        }

        internal static void CheckDateRangeNonOverlappingInDB(AppPress a, FormData formData, FormField dateFromFormField, FormField dateToFormField)
        {
            if (dateFromFormField.IsDateRange == 0)
                throw new Exception(dateFromFormField.fieldName + " should be of type DateRange");
            var rowFormDef = formData.formDef;
            var containerIdFormField = rowFormDef.formFields.Find(t => t.Type == FormDefFieldType.ForeignKey);
            var dr = a.ExecuteQuery(
                @"Select Id, " + dateFromFormField.fieldName + @", " + dateToFormField.fieldName + @" From " + rowFormDef.TableName + @" AS M 
                  Where " + containerIdFormField.fieldName + @"=" + formData.GetFieldString(containerIdFormField.fieldName) +
                @" and  (
                    Select IfNull(Count(*),0) From " + rowFormDef.TableName + @" as A Where A." + containerIdFormField.fieldName + @"=M." + containerIdFormField.fieldName +
                  @" and M." + dateFromFormField.fieldName + @" between  A." + dateFromFormField.fieldName + @" and  Date_Add(IfNUll(A." + dateToFormField.fieldName + @",Date_Add(Current_Date,interval 1 day)), interval -1 day)
                    and A.Id<>M.id
                    )<>0
                  Order by DateFrom");
            try
            {
                if (dr.Read())
                {
                    throw new AppPressException(dateFromFormField.GetDescription() + " is overlapping with another Date Range.");
                }
            }
            finally
            {
                dr.Close();
            }
        }
        internal static void CheckDateRangeContiguousInDB(AppPress a, FormData formData, FormField dateFromFormField, FormField dateToFormField)
        {
            if (dateFromFormField.IsDateRange == 0)
                throw new Exception(dateFromFormField.fieldName + " should be of type DateRange");
            DateTime? lastDateTo = null;
            var rowFormDef = formData.formDef;
            var containerIdFormField = rowFormDef.formFields.Find(t => t.Type == FormDefFieldType.ForeignKey);
            // As we allow DateFrom = DateTo. Delete and update may make DateFrom-1=DateTo. Correct this
            var nRows = a.ExecuteNonQuery("Update " + a.SQLQuote + "" + rowFormDef.TableName + "" + a.SQLQuote + " Set " + dateToFormField.fieldName + "=" + dateFromFormField.fieldName + " Where DateDiff(" + dateFromFormField.fieldName + "," + dateToFormField.fieldName + ")=1 and " + containerIdFormField.fieldName + "=" + formData.GetFieldString(containerIdFormField.fieldName));
            var dr = a.ExecuteQuery("Select Id, " + dateFromFormField.fieldName + ", " + dateToFormField.fieldName + " From " + a.SQLQuote + "" + rowFormDef.TableName + "" + a.SQLQuote + " Where " + containerIdFormField.fieldName + "=" + formData.GetFieldString(containerIdFormField.fieldName) + " Order by " + dateFromFormField.fieldName + ",IfNull(" + dateToFormField.fieldName + ",'2100-1-1')");
            try
            {
                int nullDateTo = 0;
                while (dr.Read())
                {
                    var id = dr.GetInt64(0);
                    var dateFrom = dr.GetDateTime(1);
                    DateTime? dateTo = dr.IsDBNull(2) ? (DateTime?)null : dr.GetDateTime(2);
                    if (dateTo != null && dateFrom > dateTo)
                        throw new AppPressException(dateFromFormField.fieldName + ": " + dateFrom.ToString(AppPress.Settings.NetDateFormat, System.Globalization.CultureInfo.InvariantCulture) + " should be less than " + dateToFormField.fieldName + ":" + dateTo.Value.ToString(AppPress.Settings.NetDateFormat, System.Globalization.CultureInfo.InvariantCulture));
                    if (lastDateTo != null)
                        if (dateFrom <= lastDateTo)
                            throw new AppPressException(dateFromFormField.fieldName + ": " + dateFrom.ToString(AppPress.Settings.NetDateFormat, System.Globalization.CultureInfo.InvariantCulture) + " should be day after " + dateToFormField.fieldName + ":" + lastDateTo.Value.ToString(AppPress.Settings.NetDateFormat, System.Globalization.CultureInfo.InvariantCulture));
                    lastDateTo = dateTo;
                    if (dateTo == null)
                        nullDateTo++;
                }
                if (nullDateTo > 1)
                    throw new AppPressException("There should one " + rowFormDef.formName + " with Blank Date To");
            }
            finally
            {
                dr.Close();
            }

        }


    }
}
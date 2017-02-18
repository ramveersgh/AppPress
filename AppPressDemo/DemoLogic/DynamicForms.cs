/* Copyright SysMates Technologies Pte. Ltd. */
using System.Collections.Generic;
using System.Linq;
using ApplicationClasses;
using AppPressFramework;

namespace Application
{
    public partial class AppLogic
    {
        static int EvaluationFormDefInsertIdx = -1, EvaluationFormDefInsertCount = 1;
        static void Init(AppPressDemo a, DynamicFormsClass DynamicForms)
        {
            DynamicForms.Description.val = @"<span style='font-size:32px'>Forms Created and or Modified at Runtime <a href='https://docs.google.com/document/d/1goW6BxoZp1WK12ZCcmg8RqVgADifn8yYZSZoGjpUeRo/edit#heading=h.fwfcqg9o7ckr' target='_blank'><i class=""fa fa-question-circle""></i></a></span>";
            DynamicForms.DynamicForms.Help = "In grid below add or modify forms and or fields. Click on Test button to see the Form. Fields of this form are inserted into a existing form with Name DynamicForm";

        }
        public static void OnClick(AppPressDemo a, DynamicFormsClass.TestFieldClass Test)
        {
            var form = Test.FormData.DynamicForms.GetSingleSelection();
            LoadDynamicForms(a, form.id);
            var evaluationForm = new DynamicFormClass(a);
            evaluationForm.Popup(a, null);
        }

        // Load Forms from Database
        internal static void LoadDynamicForms(AppPress a, string formId)
        {
            var query = @"
                Select ""demo.DynamicForms.Fields"".FormId,""demo.DynamicForms.Fields"".Id, FieldType,fieldName
                From ""demo.DynamicForms"" 
                Left Outer Join ""demo.DynamicForms.Fields"" On ""demo.DynamicForms.Fields"".FormId=""demo.DynamicForms"".Id
                Where ""demo.DynamicForms"".id=" + formId + @"
                ";
            var dr = a.ExecuteQuery(query);
            try
            {
                var formFields = new List<FormField>();
                while (dr.Read())
                {

                    var id = dr.GetInt32(1);
                    var fieldType = (FormDefFieldType)dr.GetInt32(2);
                    var fieldName = dr.GetString(3);
                    var formFieldM = new FormField(fieldName, fieldType);
                    formFieldM.dbId = id;
                    formFields.Add(formFieldM);
                }

                var EvaluationFormDef = AppPress.FindSingleFormDef("DynamicForm");
                if (EvaluationFormDefInsertIdx == -1)
                {
                    EvaluationFormDefInsertIdx = EvaluationFormDef.formFields.FindIndex(t => t.fieldName == "ReplaceWithDynamicFields");
                }
                EvaluationFormDef.formFields.RemoveRange(EvaluationFormDefInsertIdx, EvaluationFormDefInsertCount);
                EvaluationFormDefInsertCount = formFields.Count();
                EvaluationFormDef.formFields.InsertRange(EvaluationFormDefInsertIdx, formFields);
                // Add the FormDef
                a.AddFormDef(EvaluationFormDef);
            }
            finally
            {
                dr.Close();
            }
        }
    }
}

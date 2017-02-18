/* Copyright SysMates Technologies Pte. Ltd. */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data;
using ApplicationClasses;
using AppPressFramework;

namespace Application
{
    public partial class AppLogic
    {
        public static void Init(AppPressDemo p, UIElementsClass UIElements)
        {
            UIElements.Save.Help = "Used CSSClass in Field XML to move Button to right<br/>Templates for BeforeSave and AfterSave functions";
        }
        public static void Init(AppPressDemo p, PickoneClass Pickone)
        {
            Pickone.Help.val = "<a href='https://docs.google.com/document/d/1goW6BxoZp1WK12ZCcmg8RqVgADifn8yYZSZoGjpUeRo/edit#heading=h.ny7l7j6f9jrz' target='_blank'><i class=\"fa fa-question-circle\"></i></a>";
        }
        public static void Init(AppPressDemo p, TextClass Text)
        {
            Text.Help.val = "<a href='https://docs.google.com/document/d/1goW6BxoZp1WK12ZCcmg8RqVgADifn8yYZSZoGjpUeRo/edit#heading=h.a24q3ds594fi' target='_blank'><i class=\"fa fa-question-circle\"></i></a>";
            Text.TextWithValidation.Help = "Regex, TitleCase, MinChars and MaxChars for First Name";
        }
        public static void Init(AppPressDemo p, TextAreaClass TextArea)
        {
            TextArea.Help.val = "<a href='https://docs.google.com/document/d/1goW6BxoZp1WK12ZCcmg8RqVgADifn8yYZSZoGjpUeRo/edit#heading=h.a24q3ds594fi' target='_blank'><i class=\"fa fa-question-circle\"></i></a>";
        }
        public static void Init(AppPressDemo p, HTMLClass HTML)
        {
            HTML.Help.val = "<a href='https://docs.google.com/document/d/1goW6BxoZp1WK12ZCcmg8RqVgADifn8yYZSZoGjpUeRo/edit#heading=h.9815q1w8j2iw' target='_blank'><i class=\"fa fa-question-circle\"></i></a>";
            HTML.HTML.val = "<span style='font-size:24px'>Display Text in HTML</span>";
        }
        public static void Init(AppPressDemo a, ButtonClass Button)
        {
            Button.Help.val = "<a href='https://docs.google.com/document/d/1goW6BxoZp1WK12ZCcmg8RqVgADifn8yYZSZoGjpUeRo/edit#heading=h.9mk6rk14f2u' target='_blank'><i class=\"fa fa-question-circle\"></i></a>";
            Button.Button.Help = "Prints the Content of Text Area Rich Field above as PDF<br/>Look at the implementation to find how to get the Text Area Rich Field";
        }
        public static void Init(AppPressDemo a, NumberClass Number)
        {
            Number.Help.val = "<a href='https://docs.google.com/document/d/1goW6BxoZp1WK12ZCcmg8RqVgADifn8yYZSZoGjpUeRo/edit#heading=h.f1to8wbjulwc' target='_blank'><i class=\"fa fa-question-circle\"></i></a>";
            Number.Number.Help = "Number between " + Number.Number.GetMinimumValue() + " and " + Number.Number.GetMaximumValue();
        }
        public static void Init(AppPressDemo a, CheckboxClass Checkbox)
        {
            Checkbox.Help.val = "<a href='https://docs.google.com/document/d/1goW6BxoZp1WK12ZCcmg8RqVgADifn8yYZSZoGjpUeRo/edit#heading=h.i6olsqd92njc' target='_blank'><i class=\"fa fa-question-circle\"></i></a>";
        }
        public static void Init(AppPressDemo a, PickMultipleClass PickMultiple)
        {
            PickMultiple.Help.val = "<a href='https://docs.google.com/document/d/1goW6BxoZp1WK12ZCcmg8RqVgADifn8yYZSZoGjpUeRo/edit#heading=h.rdvm7wmtxbtb' target='_blank'><i class=\"fa fa-question-circle\"></i></a>";
            PickMultiple.PickMultipleCheckbox.Help = "Saved in Database";
            PickMultiple.PickMultiple.Help = "Not Saved in Database";
        }
        public static void Init(AppPressDemo a, DateTimeClass DateTime)
        {
            DateTime.Help.val = "<a href='https://docs.google.com/document/d/1goW6BxoZp1WK12ZCcmg8RqVgADifn8yYZSZoGjpUeRo/edit#heading=h.lpfptpg924y2' target='_blank'><i class=\"fa fa-question-circle\"></i></a>";
        }
        public static void Init(AppPressDemo a, DateRangeClass DateRange)
        {
            DateRange.Help.val = "<a href='https://docs.google.com/document/d/1goW6BxoZp1WK12ZCcmg8RqVgADifn8yYZSZoGjpUeRo/edit#heading=h.iz1u31vo4rnh' target='_blank'><i class=\"fa fa-question-circle\"></i></a>";
        }
        public static void Init(AppPressDemo a, FileUploadClass FileUpload)
        {
            FileUpload.Help.val = "<a href='https://docs.google.com/document/d/1goW6BxoZp1WK12ZCcmg8RqVgADifn8yYZSZoGjpUeRo/edit#heading=h.3d5sox4cyqjf' target='_blank'><i class=\"fa fa-question-circle\"></i></a>";
        }
        public static void Init(AppPressDemo a, MultiFileUploadClass FileUpload)
        {
            FileUpload.Help.val = "<a href='https://docs.google.com/document/d/1goW6BxoZp1WK12ZCcmg8RqVgADifn8yYZSZoGjpUeRo/edit#heading=h.747zh1srsgmy' target='_blank'><i class=\"fa fa-question-circle\"></i></a>";
        }

        public static string Options(AppPressDemo a, PickoneClass.PickoneAutoCompleteFieldClass PickoneAutoComplete)
        {
            return "Select Id,Value From \"demo.lookup.pickvalues\"";
        }
        public static List<FormData> Domain(AppPressDemo a, UIElementsClass.FieldsRowClass.FieldFieldClass Field)
        {
            switch ((FormDefFieldType)int.Parse(Field.FormData.FieldType.val))
            {
                case FormDefFieldType.Text:
                    return Field.BindSingleForm(a, typeof(TextClass));
                case FormDefFieldType.TextArea:
                    return Field.BindSingleForm(a, typeof(TextAreaClass));
                case FormDefFieldType.Pickone:
                    return Field.BindSingleForm(a, typeof(PickoneClass));
                case FormDefFieldType.Number:
                    return Field.BindSingleForm(a, typeof(NumberClass));
                case FormDefFieldType.Checkbox:
                    return Field.BindSingleForm(a, typeof(CheckboxClass));
                case FormDefFieldType.PickMultiple:
                    return Field.BindSingleForm(a, typeof(PickMultipleClass));
                case FormDefFieldType.DateTime:
                    return Field.BindSingleForm(a, typeof(DateTimeClass));
                case FormDefFieldType.DateRange:
                    return Field.BindSingleForm(a, typeof(DateRangeClass));
                case FormDefFieldType.FileUpload:
                    return Field.BindSingleForm(a, typeof(FileUploadClass));
                case FormDefFieldType.Button:
                    return Field.BindSingleForm(a, typeof(ButtonClass));
                case FormDefFieldType.HTML:
                    return Field.BindSingleForm(a, typeof(HTMLClass));
                case FormDefFieldType.MultiFileUpload:
                    return Field.BindSingleForm(a, typeof(MultiFileUploadClass));
            }
            return null;
        }
        public static void BeforeSave(AppPressDemo a, UIElementsClass UIElements)
        {
            // do any extra validation here
        }
        public static void OnClick(AppPressDemo a, UIElementsClass.SaveFieldClass Save)
        {
            // Will save the form and all child forms
            Save.FormData.Save(a);
            a.AlertMessage("Form Saved Successfully.");
        }
        public static void AfterSave(AppPressDemo a, UIElementsClass UIElements)
        {
            // Do any extra database updation here
            // use UIElements.IsOriginalNew to check if a New Form was saved
            // This a called within a database transaction. Any Exception thrown will undo all changes to Database
        }

        public static void OnClick(AppPressDemo a, ButtonClass.ButtonFieldClass Button)
        {
            var fieldsRowClass = (UIElementsClass.FieldsRowClass)Button.FormData.FormDataContainer;
            var uiElementsClass = (UIElementsClass)(fieldsRowClass.FormDataContainer);
            var fieldsRowTextAreaClass = (UIElementsClass.FieldsRowClass)uiElementsClass.Fields.GetContainedFormDatas(a).Find(t => ((UIElementsClass.FieldsRowClass)t).FieldType.val == ((int)FormDefFieldType.TextArea).ToString());
            var textAreaClass = (TextAreaClass)fieldsRowTextAreaClass.Field.GetContainedFormDatas(a)[0];
            var content = textAreaClass.TextAreaRich.val;
            if (content == null)
            {
                a.AlertMessage("Text Area Rich Field Should not be empty");
                return;
            }
            a.DownloadPDF("Select '" + a.EscapeSQLString(content) + "'", "Rich Text Area", null);
        }
    }
}

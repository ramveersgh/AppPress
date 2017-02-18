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
        public static List<FormData> Domain(AppPressDemo a, FormContainerDynamicClass.FieldDetailsFieldClass FieldDetails)
        {
            if (FieldDetails.FormData.FieldType.val == null)
                return null;
            switch ((FormDefFieldType)int.Parse(FieldDetails.FormData.FieldType.val))
            {
                case FormDefFieldType.Text:
                    FieldDetails.FormData.FieldType.Help = "Field to enter Single line Text";
                    return FieldDetails.BindSingleForm(a, typeof(TextClass));
                case FormDefFieldType.TextArea:
                    FieldDetails.FormData.FieldType.Help = "Field to enter Multiline Text";
                    return FieldDetails.BindSingleForm(a, typeof(TextAreaClass));
                case FormDefFieldType.Pickone:
                    FieldDetails.FormData.FieldType.Help = "Field to Choose one Value from List of Values";
                    return FieldDetails.BindSingleForm(a, typeof(PickoneClass));
                case FormDefFieldType.Number:
                    FieldDetails.FormData.FieldType.Help = "Field to Enter a decimal Number";
                    return FieldDetails.BindSingleForm(a, typeof(NumberClass));
                case FormDefFieldType.Checkbox:
                    FieldDetails.FormData.FieldType.Help = "Field with Checkbox";
                    return FieldDetails.BindSingleForm(a, typeof(CheckboxClass));
                case FormDefFieldType.PickMultiple:
                    FieldDetails.FormData.FieldType.Help = "Field to Choose multiple Values from List of Values";
                    return FieldDetails.BindSingleForm(a, typeof(PickMultipleClass));
                case FormDefFieldType.DateTime:
                    FieldDetails.FormData.FieldType.Help = "Field to Enter Date";
                    return FieldDetails.BindSingleForm(a, typeof(DateTimeClass));
                case FormDefFieldType.DateRange:
                    FieldDetails.FormData.FieldType.Help = "Field to Enter Date Range";
                    return FieldDetails.BindSingleForm(a, typeof(DateRangeClass));
                case FormDefFieldType.FileUpload:
                    FieldDetails.FormData.FieldType.Help = "Field to Upload file";
                    return FieldDetails.BindSingleForm(a, typeof(FileUploadClass));
                case FormDefFieldType.Button:
                    FieldDetails.FormData.FieldType.Help = "Field for action Button";
                    return FieldDetails.BindSingleForm(a, typeof(ButtonClass));
                case FormDefFieldType.HTML:
                    FieldDetails.FormData.FieldType.Help = "Field to show HTML Content";
                    return FieldDetails.BindSingleForm(a, typeof(HTMLClass));
            }
            return null;
        }

    }
}

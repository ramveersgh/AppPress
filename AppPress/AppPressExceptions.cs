using System;
using System.Collections.Generic;

namespace AppPressFramework
{
    public class AppPressException : Exception
    {
        public string GetMessage(AppPress a)
        {
            var message = "";
            foreach (var item in a.appPressResponse)
            {
                if (item.appPressResponseType == AppPressResponseType.FormError)
                {
                    var formDef = AppPress.FindFormDef(item.formDefId);
                    message += formDef.formName + "- " + item.message + "\n";
                }
                if (item.appPressResponseType == AppPressResponseType.FieldError)
                {
                    var formDef = AppPress.FindFormDef(item.formDefId);
                    message += formDef.formName + ": " + formDef.GetFormField(item.fieldDefId).fieldName + " - " + item.message + "\n";
                }
            }
            return message;
        }
        internal AppPressResponse clientAction = null;
        public AppPressException()
        {
        }
        internal AppPressException(AppPressResponse clientAction)
            : base(clientAction.message)
        {
            this.clientAction = clientAction;
        }
        public AppPressException(string message)
            : base(message)
        {
            this.clientAction = AppPressResponse.AlertMessage(message);

        }
    }
    public class SessionExpiredException : Exception
    {
    }
    public class ForeignKeyConstraintException : AppPressException
    {
        public ForeignKeyConstraintException(string message)
            : base(message)
        {

        }
    }
}
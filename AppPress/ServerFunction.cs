using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Runtime.Serialization;

namespace AppPressFramework
{
    [DataContract]
    internal class ServerFunction
    {
        public ServerFunction() { }
        public ServerFunction(AppPress a, FunctionType functionType, string functionName)
        {
            this.ServerFunctionType = functionType;
            this.FunctionName = functionName;
            SetMethod(a);
        }
        internal void SetMethod(AppPress a)
        {

            switch (ServerFunctionType)
            {
                case FunctionType.Options:
                    method = Util.GetMethod(a, FunctionName, new Type[] { typeof(AppPress), typeof(FieldValue) });
                    if (method == null)
                        method = Util.GetMethod(a, FunctionName, new Type[] { AppPress.Settings.ApplicationAppPress, typeof(FieldValue) });
                    break;
                default:
                    method = Util.GetMethod(a, FunctionName, new Type[] { typeof(AppPress) });
                    break;
            }
            if (method == null)
                throw new Exception("Could not find Function: " + FunctionName);
            switch (ServerFunctionType)
            {
                case FunctionType.Options:
                    if (method.ReturnParameter.ParameterType.Name != typeof(List<Option>).Name && method.ReturnParameter.ParameterType.Name != typeof(string).Name)
                        throw new Exception("Function: " + FunctionName + " used as a " + ServerFunctionType + " Function does not return List<Option>.");
                    break;
                case FunctionType.Domain:
                    break;
                default:
                    if (method.ReturnParameter.ParameterType.Name != typeof(void).Name)
                        throw new Exception("Function: " + FunctionName + " used as a " + ServerFunctionType + " Function does not return void.");
                    break;
            }

        }
        internal string GetFunctionParameterValue(string parameterName)
        {
            var returnValue = TryGetFunctionParameterValue(parameterName);
            if (returnValue == null)
                throw new Exception("Could not find Parameter:" + parameterName + " in Function:" + FunctionName);
            return returnValue.Trim();

        }
        internal List<string> GetFunctionParameterValues(string parameterName)
        {
            var values = new List<String>();
            foreach (var parameter in Parameters.FindAll(t => t.Name.ToEqual(parameterName)))
                values.Add(parameter.Value);
            return values;

        }
        internal string TryGetFunctionParameterValue(string parameterName)
        {
            var parameter = Parameters.Find(t => t.Name.ToEqual(parameterName));
            if (parameter == null)
                return null;
            return parameter.Value.Trim();
        }
        [DataMember]
        public FunctionType ServerFunctionType;
        [DataMember]
        public string FunctionName;
        [DataMember]
        public List<ServerFunctionParameter> Parameters = new List<ServerFunctionParameter>();
        [DataMember]
        public bool ExecuteClientFunctions = false;
        [DataMember]
        public long? parameterFormId = null;

        public MethodInfo method;
    }
}

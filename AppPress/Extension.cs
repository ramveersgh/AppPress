using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Web;
using System.Xml.Linq;
using AppPressFramework;

public static class Extension
{
    public static string ToEscapeString(this string stringValue)
    {
        return Microsoft.JScript.GlobalObject.escape(stringValue);
        //return stringValue.Replace("\\","\\\\").Replace("\'","\\\'");
    }
    public static string ToUnEscapeString(this string stringValue)
    {
        if (!string.IsNullOrEmpty(stringValue))
        {
            return Microsoft.JScript.GlobalObject.unescape(stringValue);
        }
        return stringValue;
    }
    //public static string ToSqlEncodedString(this string stringValue)
    //{
    //    if (!string.IsNullOrEmpty(stringValue))
    //    {
    //        return AppPress.EscapeSQLString(stringValue);
    //    }
    //    return stringValue;
    //}
    public static bool IsNullOrEmpty(this string stringValue)
    {
        return string.IsNullOrEmpty(stringValue);
    }
    public static bool ToEqual(this string stringValue, string stringValueToCompare)
    {
        return stringValue.Equals(stringValueToCompare, StringComparison.OrdinalIgnoreCase);
    }

    public static string GetContent(this XElement element)
    {
        if (element != null)
        {
            if (element.HasElements)
            {
                var elementContent = element.ToString();
                var elementName = element.Name;
                return
                    elementContent.Replace("<" + elementName + ">", string.Empty).Replace("</" + elementName + ">",
                                                                                          string.Empty).Trim();
            }
            else
                return element.Value;
        }
        return null;
    }

    public static string ToTruncatedString(this string stringValue, int length)
    {
        if ((!string.IsNullOrEmpty(stringValue)) && stringValue.Length > length)
        {
            return string.Format("{0}...", stringValue.Substring(0, length));
        }
        return stringValue;
    }
    public static T Clone<T>(T source)
    {
        if (!typeof(T).IsSerializable)
        {
            throw new ArgumentException("The type must be serializable.", "source");
        }

        // Don't serialize a null object, simply return the default for that object
        if (Object.ReferenceEquals(source, null))
        {
            return default(T);
        }

        IFormatter formatter = new BinaryFormatter();
        Stream stream = new MemoryStream();
        using (stream)
        {
            formatter.Serialize(stream, source);
            stream.Seek(0, SeekOrigin.Begin);
            return (T)formatter.Deserialize(stream);
        }
    }

}

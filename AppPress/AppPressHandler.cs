using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.IO;
using System.IO.Compression;
using System.CodeDom.Compiler;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Threading;
using System.Collections.Specialized;
using System.Net;
using System.Text.RegularExpressions;


namespace AppPressFramework
{
    public class AppPressHandler
    {
        internal string GetTemporaryDirectory()
        {
            string tempFolder = Path.GetTempFileName();
            File.Delete(tempFolder);
            Directory.CreateDirectory(tempFolder);

            return tempFolder;
        }

        private HttpContext currentContext;
        internal static StringBuilder AppPressJSStr = null;
        public AppPressHandler(Object appLogic)
        {
            Log.Writeln("Request:" + HttpContext.Current.Request.Url.AbsoluteUri);
            if (AppPress.Settings == null)
                throw new Exception("AppPress not initialized. Call InitAppPress on Application load");

            currentContext = HttpContext.Current;

            if (currentContext.Request["getJs"] != null)
            {
                currentContext.Response.ContentType = "text/javascript";
                currentContext.Response.Cache.SetExpires(DateTime.UtcNow.AddYears(1));
                currentContext.Response.Cache.SetCacheability(HttpCacheability.Public);
                if (AppPressJSStr == null)
                {
                    AppPressJSStr = new StringBuilder(FileTexts.AppPressJS).Append("\n");
                    foreach (var formDef in AppPress.formDefs)
                        foreach (var formField in formDef.formFields)
                            if (formField.optionsCache != null && formField.Type == FormDefFieldType.Pickone && formField.Style == FormDefFieldStyle.DropDown)
                                AppPressJSStr.Append(formField.BuildOptionsForJS());
                }
                currentContext.Response.Write(AppPressJSStr);
            }
            else if (currentContext.Request["UploadFile"] != null)
            {
                try
                {
                    if (currentContext.Request.Files.Count > 0)
                    {
                        string response = "";
                        int maxFileSizeInKB = 1024 * 5; // 5MB
                        if (currentContext.Request["MaxFileSizeInKB"] != null)
                            maxFileSizeInKB = int.Parse(currentContext.Request["MaxFileSizeInKB"]);
                        for (int i = 0; i < currentContext.Request.Files.Count; i++)
                        {
                            var file = currentContext.Request.Files[i];
                            var fileName = file.FileName;
                            var inputStream = file.InputStream;
                            var intDocLen = file.ContentLength;
                            var extension = System.IO.Path.GetExtension(fileName).ToLower();
                            var acceptFileTypes = currentContext.Request["AcceptFileTypes"];
                            if (acceptFileTypes != null)
                            {
                                var matched = false;
                                var acceptFileTypesList = acceptFileTypes.Split(',');
                                foreach (var acceptFileType in acceptFileTypesList)
                                {
                                    var aft = acceptFileType.Trim().ToLower();
                                    if (aft.StartsWith(".") && file.ContentType == AppPressLogic.GetFileType(aft))
                                    {
                                        matched = true;
                                        break;
                                    }
                                    else if (aft == "image/*" && file.ContentType.StartsWith("image/"))
                                    {
                                        matched = true;
                                        break;
                                    }
                                    else if (aft == "video/*" && file.ContentType.StartsWith("video/"))
                                    {
                                        matched = true;
                                        break;
                                    }
                                    else if (aft == "sound/*" && file.ContentType.StartsWith("sound/"))
                                    {
                                        matched = true;
                                        break;
                                    }
                                    else if (aft == file.ContentType)
                                    {
                                        matched = true;
                                        break;
                                    }
                                }
                                if (!matched)
                                    throw new AppPressException("Cannot upload file of Type: " + extension);
                            }
                            if (maxFileSizeInKB != -1 && intDocLen > maxFileSizeInKB * 1024)
                                throw new AppPressException("Maximum size of file that can be uploaded is " + maxFileSizeInKB + "KB");
                            if (currentContext.Request["DoNotSaveInDB"] != null)
                            {
                                // Save in Temp Folder
                                var uFileName = GetTemporaryDirectory() + "\\" + fileName;
                                using (var fileStream = new FileStream(uFileName, FileMode.Create, FileAccess.Write))
                                {
                                    inputStream.CopyTo(fileStream);
                                }
                                uFileName = uFileName.Replace('\\', '/');
                                if (response.Length > 0)
                                    response += ",";
                                response += string.Format("{{FileName:'{0}',Id:'{1}',Size:'{2}'}}", fileName, uFileName, (intDocLen / 1024) + " KB");
                            }
                            else
                            {
                                // save in DB
                                long fileId = -1;
                                //long.TryParse(context.Request.Form["Filename"], out fileId);
                                var fileType = AppPressLogic.GetFileType(extension);
                                FileUploadStorageType storageType = FileUploadStorageType.Database;
                                string directory = currentContext.Request["Directory"];
                                if (directory != null)
                                    storageType = FileUploadStorageType.Directory;
                                EncryptionType? encryptionType = null;
                                if (currentContext.Request["EncryptionType"] != null)
                                    encryptionType = (EncryptionType)Convert.ToInt16(currentContext.Request["EncryptionType"]);
                                var docbuffer = new byte[intDocLen];
                                inputStream.Read(docbuffer, 0, intDocLen);
                                fileId = Util.SaveFile(storageType, directory, fileName, docbuffer, fileType, intDocLen, encryptionType, currentContext.Request["NonSecure"] != null);
                                var uQuery = currentContext.Request["UpdateQuery"];
                                if (uQuery != null)
                                {
                                    var site = new DAOBasic();
                                    try
                                    {
                                        site.ExecuteNonQuery(uQuery.Replace("%%FileID%%", fileId.ToString()));
                                    }
                                    finally
                                    {
                                        site.Close();
                                    }
                                }
                                if (response.Length > 0)
                                    response += ",";
                                response += string.Format("{{FileName:'{0}',Id:{1},Size:'{2}'}}", fileName.Replace("'", "\\'"), fileId, (intDocLen / 1024) + " KB");
                            }
                        }
                        currentContext.Response.Write("[" + response + "]");
                    }
                }
                catch (Exception exception)
                {
                    currentContext.Response.Write(string.Format("{{Error:'{0}'}}", HttpUtility.JavaScriptStringEncode(exception.Message)));

                    // throw new Exception("<script>alert(" + exception.Message + ")</script>");
                }
                currentContext.Response.StatusCode = 200;
            }
            else if (currentContext.Request["GetPDF"] != null)
            {
                try
                {
                    Thread.CurrentThread.Priority = ThreadPriority.Lowest;
                    var GetPDFParams = currentContext.Request["GetPDFParams"].Split(new string[] { AppPress.QuerySeperator }, StringSplitOptions.None);
                    string tempFolder = null;
                    if (GetPDFParams.Count() > 1)
                    {
                        tempFolder = GetTemporaryDirectory();
                        currentContext.Response.ContentType = "Application/zip";
                    }
                    else
                        currentContext.Response.ContentType = "application/pdf";
                    foreach (var GetPDFParam in GetPDFParams)
                    {
                        var pdfParams = GetPDFParam.Split(new string[] { AppPress.QuerySeperator1 }, StringSplitOptions.None);
                        byte[] pdf;
                        string fileName;
                        if (pdfParams[0] == "F")
                        {
                            var formName = AppPress.formDefs.Find(t => t.id == long.Parse(pdfParams[1])).formName;
                            var id = pdfParams[2];
                            fileName = pdfParams[3];
                            if (fileName.IsNullOrEmpty())
                                fileName = formName;
                            else
                                fileName = HttpUtility.UrlDecode(fileName);
                            pdf = Util.GeneratePDF(formName, id, currentContext.Request, appLogic, int.Parse(pdfParams[4]), pdfParams[5]);
                        }
                        else //if (pdfParams[0] == "Q")
                        {
                            //style = "font-size:72px"
                            fileName = HttpUtility.UrlDecode(pdfParams[2]);
                            PDFPageSettings pageSettings;
                            if (pdfParams[3].IsNullOrEmpty())
                                pageSettings = new PDFPageSettings(PageSizes.A4, false);
                            else
                                pageSettings = (PDFPageSettings)FormDef.Deserialize(HttpUtility.UrlDecode(pdfParams[3]), typeof(PDFPageSettings));
                            var site = new DAOBasic();
                            var html = site.ExecuteString(HttpUtility.UrlDecode(pdfParams[1]));
                            pdf = new AppPress().HtmlToPDF(html, fileName, pageSettings);
                        }
                        if (tempFolder == null)
                        {
                            currentContext.Response.AddHeader("content-disposition", "attachment; filename=\"" + fileName + ".pdf\"");
                            currentContext.Response.BinaryWrite(pdf);
                        }
                        else
                            File.WriteAllBytes(tempFolder + "\\" + fileName + ".pdf", pdf);
                    }
                    if (tempFolder != null)
                    {
                        var zipfileName = Path.GetTempFileName();
                        File.Delete(zipfileName);
                        ZipFile.CreateFromDirectory(tempFolder, zipfileName);
                        currentContext.Response.AddHeader("content-disposition", "attachment; filename=\"" + Path.GetFileNameWithoutExtension(zipfileName) + ".zip\"");
                        currentContext.Response.BinaryWrite(File.ReadAllBytes(zipfileName));
                    }
                    currentContext.Response.AddHeader("Cache-Control", "no-store; no-cache");
                    HttpContext.Current.ApplicationInstance.CompleteRequest();
                    currentContext.Response.Flush();
                    currentContext.Response.Close();
                }
                catch (Exception ex)
                {
                    currentContext.Response.Clear();
                    currentContext.Response.ClearHeaders();
                    currentContext.Response.ContentType = "text/plain";
                    currentContext.Response.Write("Error Occured in PDF Generation: " + ex.Message);
                }
                finally
                {
                    //currentContext.Response.Redirect(currentContext.Request.UrlReferrer.ToString(), true);
                }
            }
            else if (currentContext.Request["GetLocalization"] != null)
            {
                currentContext.Response.Clear();
                currentContext.Response.ClearHeaders();
                currentContext.Response.ContentType = "text/html";
                var formDefs = AppPress.formDefs;

                var LExitingKeys = new Dictionary<string, string>();
                foreach (var formDef in formDefs)
                {
                    var LKeys = formDef.GenerateLocalizationKey(LExitingKeys);
                    foreach (var key in LKeys.Keys)
                    {
                        if (!AppPress.LocalizationData.ContainsKey(key))
                            LExitingKeys.Add(key, LKeys[key]);
                    }
                }
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("<table cellpadding=\"0\" cellspacing=\"3\" border=\"1\"><tr><th>Key</th><th>English</th></tr>");
                foreach (var key in LExitingKeys.Keys)
                {
                    sb.AppendLine("<tr><td>" + key + "</td><td>" + LExitingKeys[key] + "</td></tr>");

                }
                sb.AppendLine("</table>");
                currentContext.Response.Write(sb.ToString());
            }
            else if (currentContext.Request["GetFile"] != null)
            {
                var dbName = currentContext.Request["DBName"];
                var FilePath = currentContext.Request["FilePath"];
                var fileId = currentContext.Request["Id"];
                var heightStr = currentContext.Request["height"];
                var widthStr = currentContext.Request["width"];
                var downloadStr = currentContext.Request["Download"];
                var responseInstanceId = currentContext.Request["ResponseInstanceId"];
                var download = !string.IsNullOrWhiteSpace(downloadStr);
                var fileContentType = currentContext.Request["ContentType"];
                var fileName = currentContext.Request["FileName"];
                FileDetails fileDetails = new FileDetails();
                var instanceId = currentContext.Request["InstanceId"];
                if (instanceId != null)
                {
                    var remoteUrl = AppPress.Settings.Instances.Find(t => t.InstanceId == int.Parse(instanceId)).InstanceBaseUrl + "?GetFile=" + "&ResponseInstanceId=" + AppPress.LocalInstanceId + "&Id=" + fileId;
                    if (FilePath != null)
                        remoteUrl += "&FilePath=" + FilePath;
                    if (heightStr != null)
                        remoteUrl += "&height=" + heightStr;
                    if (widthStr != null)
                        remoteUrl += "&width=" + widthStr;
                    if (downloadStr != null)
                        remoteUrl += "&Download=" + downloadStr;

                    WebClient client = new WebClient();
                    var fileResponseStr = System.Text.Encoding.UTF8.GetString(client.UploadValues(new Uri(remoteUrl), new NameValueCollection()));
                    fileDetails = (FileDetails)FormDef.Deserialize(fileResponseStr, typeof(FileDetails));
                }
                else
                {
                    var url = HttpUtility.UrlDecode(currentContext.Request.Url.ToString());
                    var sData = AppPress.TryGetSessionData();
                    var site = new DAOBasic();
                    try
                    {
                        if (dbName == null)
                            dbName = site.dbName;

                        DateTime uploadTime;

                        if (fileId != null)
                        {
                            if (site.ExecuteInt("Select Used From " + site.QuoteDBName(dbName) + ".Application_Files Where Id=" + fileId) == 0)
                                throw new Exception("File with Id: " + fileId + " is marked as not Used. So cannot be served");
                            if (sData == null || sData.formDefIdAndFormIds.Find(t => t == url) == null)
                                if (fileId == null || site.ExecuteInt("Select NonSecure From " + site.QuoteDBName(dbName) + ".Application_Files Where Id=" + fileId) == 0)
                                    // File Request for Employee Photo come from URL from PDF and Org Chart
                                    throw new Exception("Security Error: Invalid access"); string f;
                            fileDetails.FileBytes = Util.GetFile(site, dbName, int.Parse(fileId), out f, out fileContentType, out uploadTime);
                            if (fileName == null)
                                fileName = f;
                            if (sData.loginUserId != null)
                                site.ExecuteNonQuery(@"
                                Insert into Application_Audit(UserName,Time,AuditType,TableName,RowId,LoginUserId," + site.SQLQuote + @"Change" + site.SQLQuote + @",TimeStamp)
                                Values ('" + sData.email + @"','" + DateTime.Now.ToString(DAOBasic.DBDateTimeFormat) + "'," + (int)AuditType.DownloadFile + @",''," + fileId + "," + sData.loginUserId + @",''," + DateTime.UtcNow.Ticks + @")");
                        }
                        else
                        {
                            if (fileName == null)
                                fileName = Path.GetFileName(FilePath);
                            uploadTime = File.GetCreationTime(FilePath);
                            fileDetails.FileBytes = File.ReadAllBytes(FilePath);
                        }
                        fileDetails.FileName = fileName;
                        fileDetails.UploadTime = uploadTime;
                        fileDetails.ContentType = fileContentType ?? "";
                    }
                    finally
                    {
                        site.Close();
                    }
                    byte[] newbuffer = fileDetails.FileBytes;
                    if (!download && fileDetails.ContentType != null && fileDetails.ContentType.ToLower().Contains("image"))
                    {
                        int height = int.Parse(heightStr ?? "-1");
                        int width = int.Parse(widthStr ?? "-1");

                        if (width == -1 && height == -1)
                            newbuffer = fileDetails.FileBytes;
                        else
                        {
                            MemoryStream ms = new MemoryStream(fileDetails.FileBytes);
                            Image img = Image.FromStream(ms);
                            if (width == -1)
                                width = (int)(height * ((double)img.Width / img.Height));
                            if (height == -1)
                                height = (int)(width * ((double)img.Height / img.Width));
                            using (Bitmap newImage = new Bitmap(width * 2, height * 2, PixelFormat.Format32bppArgb))
                            {
                                using (Graphics canvas = Graphics.FromImage(newImage))
                                {
                                    canvas.SmoothingMode = SmoothingMode.AntiAlias;
                                    canvas.InterpolationMode = InterpolationMode.HighQualityBicubic;
                                    canvas.PixelOffsetMode = PixelOffsetMode.HighQuality;
                                    canvas.Clear(System.Drawing.Color.Transparent);
                                    canvas.DrawImage(img, new Rectangle(new Point(0, 0), new Size(width * 2, height * 2)));
                                    MemoryStream m = new MemoryStream();
                                    newImage.Save(m, ImageFormat.Jpeg);
                                    newbuffer = m.ToArray();
                                }
                            }
                        }
                    }
                    fileDetails.FileBytes = newbuffer;
                }
                if (fileDetails.FileBytes != null)
                {
                    //currentContext.Response.Clear(); RAM???:Giviing access denied errro from remote.
                    if (responseInstanceId == null)
                    {
                        currentContext.Response.Clear();
                        currentContext.Response.BinaryWrite(fileDetails.FileBytes);
                        currentContext.Response.ContentType = download ? "application/force-download" : fileDetails.ContentType;
                        currentContext.Response.Cache.SetCacheability(HttpCacheability.Public);
                        currentContext.Response.Cache.SetLastModified(fileDetails.UploadTime);
                        currentContext.Response.AddHeader("Content-Disposition", "attachment; filename=\"" + fileDetails.FileName.Replace("\"", "\\\"") + "\"");
                    }
                    else
                    {
                        var s = FormDef.Serialize(fileDetails, typeof(FileDetails));
                        currentContext.Response.Write(s);
                    }
                }
            }
            else if (currentContext.Request["getCSV"] != null)
            {
                var site = new DAOBasic();
                try
                {
                    currentContext.Response.ContentType = "application/CSV";
                    currentContext.Response.AddHeader("content-disposition", "attachment; filename=\"" + currentContext.Request["getCSV"] + ".csv\"");
                    var viewQuery = currentContext.Request["viewQuery"];

                    string csv = "";
                    if (viewQuery.Trim().StartsWith("Exec", StringComparison.InvariantCultureIgnoreCase) ||
                        viewQuery.Trim().StartsWith("Call", StringComparison.InvariantCultureIgnoreCase))
                    {
                        var spDr = site.ExecuteQuery(viewQuery);
                        try
                        {
                            for (int i = 0; i < spDr.FieldCount; i++)
                            {
                                csv += @"""" + spDr.GetName(i).Replace("\"", "\"\"") + @""",";
                            }
                            csv = csv.Substring(0, csv.Length - 1) + "\r\n";
                            while (spDr.Read())
                            {
                                for (int i = 0; i < spDr.FieldCount; i++)
                                {
                                    csv += @"""" + spDr[i].ToString().Replace("\"", "\"\"") + @""",";
                                }
                                csv = csv.Substring(0, csv.Length - 1) + "\r\n";
                            }
                        }
                        finally
                        {
                            spDr.Close();
                        }
                    }
                    else
                    {
                        if (viewQuery.Contains(" "))
                            viewQuery = "(" + viewQuery + ")";
                        //add column Name
                        var qry = " Select * from " + viewQuery + " as bb";
                        var dr = site.ExecuteQuery(qry);
                        var selectColumns = "";
                        dr.Read();
                        try
                        {
                            string ifNull = "IfNull";
                            if (site.databaseType == DatabaseType.SqlServer)
                                ifNull = "IsNUll";
                            for (int i = 0; i < dr.FieldCount; i++)
                            {
                                selectColumns += "" + ifNull + "(" + site.SQLQuote + "" + dr.GetName(i) + "" + site.SQLQuote + ",''),";
                                csv += @"""" + dr.GetName(i).Replace("\"", "\"\"") + @""",";
                            }
                            selectColumns = selectColumns.Substring(0, selectColumns.Length - 1);
                            csv = csv.Substring(0, csv.Length - 1) + "\r\n";
                        }
                        finally
                        {
                            dr.Close();
                        }

                        qry = "Select " + selectColumns + " from " + viewQuery + " as bb";
                        dr = site.ExecuteQuery(qry);
                        try
                        {
                            while (dr.Read())
                            {
                                for (int i = 0; i < dr.FieldCount; i++)
                                {
                                    var s = dr[i].ToString().Replace("\"", "\"\"");
                                    if (!Regex.Match(s, "^[0-9]*$").Success)
                                        csv += @"""" + s + @""",";
                                    else
                                        csv += @"=""" + s + @""",";
                                }
                                csv = csv.Substring(0, csv.Length - 1) + "\r\n";
                            }
                        }
                        finally
                        {
                            dr.Close();
                        }
                    }
                    currentContext.Response.Write(csv);
                }
                finally
                {
                    site.Close();
                }
            }
            else if (currentContext.Request["getHTMLTable"] != null)
            {
                var url = HttpUtility.UrlDecode(currentContext.Request.Url.ToString());
                var sData = AppPress.TryGetSessionData();
                if (sData == null || sData.formDefIdAndFormIds.Find(t => t == url) == null)
                    throw new Exception("Security Error: Invalid access");
                var site = new DAOBasic();
                AppPress a = new AppPress(site);
                var NumericZeroAsBlank = false;
                if (currentContext.Request["NumericZeroAsBlank"] != null)
                    NumericZeroAsBlank = int.Parse(currentContext.Request["NumericZeroAsBlank"]) == 1;
                a.Request = currentContext.Request;
                currentContext.Response.ContentType = "text/HTML";
                var qry = currentContext.Request["viewQuery"];
                if (qry == null)
                    throw new Exception("Internal Error: Could not viewQuery in Post in ViewHTML");
                var header = currentContext.Request["header"];
                var cssUrl = AppPress.GetBaseUrl() + "Resources/css/common.css?t=" + AppPress.startTime;
                var reportHeader = currentContext.Request["getHTMLTable"];
                string html = @"<html><head><link rel=""stylesheet"" type=""text/css"" href=""" + cssUrl + @"""></head><body><div class=""appPressReport"">" + reportHeader;

                html += a.GetHtmlTableFromQuery(qry, NumericZeroAsBlank, header);
                html += "<div></body></html>";
                currentContext.Response.Write(html);
            }
            else if (currentContext.Request["EncryptTableColumn"] != null)
            {
                // Index.aspx?EncryptionType=DES&EncryptTableColumn=employee:email|employee:personalEmail&RemoveEncryption=&DBName=xxx
                // Index.aspx?EncryptionType=AES&EncryptTableColumn=employee:MobileNumber&RemoveEncryption=&DBName=xxx
                //if (!AppPress.Settings.developer)
                //    throw new Exception("This Command will work only in Debug Mode");
                var EncryptionType = (EncryptionType)Enum.Parse(typeof(EncryptionType), currentContext.Request["EncryptionType"]);
                var site = new DAOBasic();
                try
                {
                    site.BeginTrans();
                    var s = currentContext.Request["EncryptTableColumn"];
                    var tableColumns = s.Split('|');
                    foreach (var tableColumn in tableColumns)
                    {
                        var ss = tableColumn.Split(':');
                        var source = EncryptionType.None;
                        var dest = EncryptionType;
                        if (currentContext.Request["RemoveEncryption"] != null)
                        {
                            var t = source;
                            source = dest;
                            dest = t;
                        }
                        Util.ChangeColumnEncryption(site, ss[0], ss[1], source, dest, currentContext.Request["DBName"]);
                    }
                    site.Commit();
                }
                catch (Exception)
                {
                    site.RollBack();
                    throw;
                }
                finally
                {
                    site.Close();
                }
            }
            else if (AppPress.Settings.developer && currentContext.Request["GetCode"] != null)
            {
                currentContext.Response.Clear();
                currentContext.Response.ClearHeaders();
                currentContext.Response.ContentType = "text/html";
                var formDef = AppPress.FindFormDef(long.Parse(currentContext.Request["GetCode"]));
                var code = formDef.GenerateCode();
                currentContext.Response.Write(code);
            }
            else if (AppPress.Settings.developer && currentContext.Request["GetSkin"] != null)
            {
                AppPress a = new AppPress();
                a.Request = currentContext.Request;
                var oFormDefs = AppPress.formDefs;
                try
                {
                    AppPress.formDefs = (List<FormDef>)FormDef.Deserialize(FormDef.Serialize(AppPress.formDefs, typeof(List<FormDef>)), typeof(List<FormDef>));
                    currentContext.Response.Clear();
                    string str = "";
                    FormDef formDef = null;
                    long formDefId;
                    if (long.TryParse(currentContext.Request["GetSkin"], out formDefId))
                        formDef = AppPress.FindFormDef(formDefId);
                    else
                        formDef = AppPress.FindFormDef(currentContext.Request["GetSkin"]);
                    for (int i = 0; i < AppPress.formDefs.Count(); ++i)
                        AppPress.formDefs[i] = Util.InitializeFormDef(a, AppPress.formDefs[i], AppPress.formDefs);
                    bool popup = currentContext.Request["Popup"] != null;
                    str += formDef.GenerateSkinTop(a, false, popup);
                    str += formDef.GenerateSkin(a, false, null);
                    str += formDef.GenerateSkinBottom(popup);
                    //try
                    //{
                    //    str = System.Xml.Linq.XElement.Parse("<AppPressHTMLRootNode>" + str + "</AppPressHTMLRootNode>").ToString();
                    //    str = str.Replace("<AppPressHTMLRootNode>", "").Replace("</AppPressHTMLRootNode>", "");
                    //}
                    //catch
                    //{
                    //    // isn't well-formed xml
                    //}
                    currentContext.Response.ContentType = "text/plain";
                    currentContext.Response.Write(str);

                }
                finally
                {
                    AppPress.formDefs = oFormDefs;
                }

            }
            else if (AppPress.Settings.developer && currentContext.Request["GetFO"] != null)
            {
                var site = new DAOBasic();
                AppPress a = new AppPress(site);

                a.Request = currentContext.Request;
                try
                {

                    currentContext.Response.Clear();
                    string str = "";
                    var formDef = AppPress.FindFormDef(long.Parse(currentContext.Request["GetFO"]));
                    a.skinType = SkinType.FO;
                    str += Util.FoHeaderStr + formDef.GenerateSkin(a, false, null) + Util.FoEndTag;
                    currentContext.Response.ContentType = "text/plain";
                    currentContext.Response.Write(str);

                }
                catch (Exception)
                {
                    site.Close();
                }

            }
            else if (HttpContext.Current.Request["RemoteForm"] != null)
            {
                var site = new DAOBasic();
                var formName = HttpContext.Current.Request["FormName"];
                AppPress a = new AppPress(site);
                AppPressResponse popup = null;
                try
                {
                    try
                    {
                        var formDef = AppPress.FindFormDef(formName);
                        if (formDef == null)
                            throw new Exception("Could not Find FormDef: " + formName);
                        var formDataId = HttpContext.Current.Request["FormDataId"];
                        PopupParams popupParams = null;
                        var popupParamsStr = HttpContext.Current.Request["PopupParams"];
                        if (popupParamsStr != null)
                            popupParams = (PopupParams)FormDef.Deserialize(popupParamsStr, typeof(PopupParams));
                        else
                            popupParams = new PopupParams();
                        a.remoteLoginUserId = HttpContext.Current.Request["RemoteLoginUserId"];
                        a.remoteInstanceId = a.instanceId = int.Parse(HttpContext.Current.Request["RemoteInstanceId"]);
                        a.remoteData = popupParams.remoteData;

                        popupParams.forRedirect = HttpContext.Current.Request["Redirect"] != null;
                        popup = AppPressResponse.Popup(a, formDef, formDataId, popupParams);
                    }
                    catch (Exception ex)
                    {
                        if (!ex.Message.IsNullOrEmpty()) // use case. Session Expired from remote popup was showing 2 error messages
                        {
                            // if no alert message. 
                            popup = new AppPressResponse();
                            popup.appPressResponseType = AppPressResponseType.AlertMessage;
                            popup.message = ex.Message;
                        }
                    }
                    if (popup != null)
                        a.appPressResponse.Add(popup);
                    var formDatas = a.formDatas;
                    var newFormDatas = new List<FormData>();
                    a.remoteFormDefs = new List<FormDef>();
                    for (int i = 0; i < formDatas.Count(); ++i)
                    {
                        var rFormDef = formDatas[i].formDef;
                        newFormDatas.Add(new FormData(formDatas[i]));
                        a.remoteFormDefs.Add(rFormDef);
                    }
                    int checkedFormDefs = 0;
                    var fCount = a.remoteFormDefs.Count();
                    while (checkedFormDefs != fCount)
                    {
                        for (int i = checkedFormDefs; i < fCount; ++i)
                            if (a.remoteFormDefs[i].ContainerFormField != null)
                            {
                                var cFormDef = a.remoteFormDefs[i].ContainerFormField.formDef;
                                a.remoteFormDefs[i].ContainerFormField.formDefId = cFormDef.id;
                                if (a.remoteFormDefs.Find(t => t.id == cFormDef.id) == null)
                                    a.remoteFormDefs.Add(cFormDef);
                            }
                        checkedFormDefs = fCount;
                        fCount = a.remoteFormDefs.Count();
                    }
                    a.formDatas = new List<FormData>();
                    foreach (var formData in newFormDatas)
                    {
                        if (formData.IsDeleted && formData.IsNew)
                            continue;
                        var fieldValues = formData.SerializableFields(a);
                        if (fieldValues.Count() == 0)
                            if (formData.formDef.formFields.Find(t => t.Type == FormDefFieldType.FormContainerDynamic || t.Type == FormDefFieldType.Button) == null)
                                continue;
                        formData.fieldValues = fieldValues;
                        a.formDatas.Add(formData);
                    }
                    HttpContext.Current.Response.Write(FormDef.Serialize(a, AppPress.Settings.ApplicationAppPress));
                    a.remoteFormDefs = null;
                    a.formDatas = formDatas;
                }
                finally
                {
                    site.Close();
                }
            }
            else if (HttpContext.Current.Request["functionCall"] != null)
            {
                Util.AjaxCallHandler(currentContext.Request, currentContext.Response, appLogic); // Ajax Call Handler ---
            }
            else // Form
            {
                var site = new DAOBasic();
                var formName = currentContext.Request["Form"] ?? AppPress.Settings.DefaultForm;
                if (formName == null)
                    throw new Exception("Invalid URL");
                var id = currentContext.Request["id"] == null ? AppPress.GetUniqueId().ToString() : currentContext.Request["id"];
                try
                {
                    var formDef = FormDef.FindFormDef(AppPress.formDefs, formName);
                    if (formDef == null)
                        throw new AppPressException("Could not Find Form: " + formName);
                    var a = (AppPress)Activator.CreateInstance(AppPress.Settings.ApplicationAppPress, new object[] { site, !formDef.NonSecure });
                    try
                    {
                        a.CreateAppPress(currentContext.Request, currentContext.Response, formDef, id, SkinType.HTML);
                        a.ignoreSkin = currentContext.Request["ignoreSkin"] != null;
                        var SkinFileName = currentContext.Request["SkinFileName"];
                        if (SkinFileName != null)
                        {
                            a.URLFormName = formName;
                            a.SkinFileName = SkinFileName;
                        }
                        var rootFormDatas = a.formDatas.FindAll(t => t.formDef.formName == formName);
                        if (rootFormDatas.Count() == 0)
                            throw new Exception("Could not find FormData with Form Name: " + formName);
                        else if (rootFormDatas.Count() > 1)
                            throw new Exception("Found more than 1 FormData with Form Name: " + formName);
                        var rootFormData = rootFormDatas[0];
                        if (currentContext.Request["_AppPressAlertMessage"] != null)
                            a.AlertMessage(currentContext.Request["_AppPressAlertMessage"]);
                        if (currentContext.Request["_AppPressFormError"] != null)
                            rootFormData.Error = currentContext.Request["_AppPressFormError"];
                        if (rootFormData.formDef.MasterFormName != null)
                        {
                            rootFormDatas = a.formDatas.FindAll(t => t.formDefId == AppPress.FindFormDef(rootFormData.formDef.MasterFormName).id);
                            if (rootFormDatas.Count() != 1)
                                throw new Exception("Found 0 or more than 1 Forms: " + rootFormData.formDef.MasterFormName);
                            rootFormData = rootFormDatas[0];
                        }
#if DEBUG
                        a.fieldsNotGenerated = new List<FieldValue>();
#endif

                        var formUIStr = rootFormData.GetHtml(a, false, false, null, SkinType.HTML);
                        formUIStr = formUIStr.Replace("AppPressHTMLHeader", a.GetHtmlHeader());
                        formUIStr = Util.RemoveScripts(a, formUIStr);
                        currentContext.Response.Clear();
                        currentContext.Response.ContentType = "text/html";
                        currentContext.Response.Write(formUIStr);
                    }
                    catch (SessionExpiredException)
                    {
                        throw;
                    }
                    catch (ThreadAbortException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        if (formName != "ErrorForm" && formName != "BlankMasterErrorForm" && AppPress.formDefs != null)
                        {
                            CustomizeException xEx = new CustomizeException();
                            xEx.Message = ex.Message;
                            xEx.StackTrace = ex.StackTrace;
                            var message = ex.Message;
                            var stackTrace = ex.StackTrace;
                            if (ex.InnerException != null)
                            {
                                xEx.InnerExMessage1 = ex.InnerException.Message;
                                xEx.InnerExStackTrace1 = ex.InnerException.StackTrace;
                                message = ex.InnerException.Message;
                                stackTrace = ex.InnerException.StackTrace;
                            }
                            if (ex.InnerException != null && ex.InnerException.InnerException != null)
                            {
                                xEx.InnerExMessage2 = ex.InnerException.InnerException.Message;
                                xEx.InnerExStackTrace2 = ex.InnerException.InnerException.StackTrace;
                                message = ex.InnerException.InnerException.Message;
                                stackTrace = ex.InnerException.InnerException.StackTrace;
                            }
                            string loginUserId = null;
                            if (a.sessionData != null)
                                loginUserId = a.sessionData.loginUserId;
                            a.SendEmail(AppPress.Settings.DebugEmail, null, "Error in AppPress", "URL: " + HttpContext.Current.Request.Url.AbsoluteUri + "\nIP: "+ HttpContext.Current.Request.UserHostAddress + "\n Login User: " + loginUserId + "\n FormId: " + id + "\n Error: " + message + "\n Stack: " + stackTrace, null, null, false);

                            a = new AppPress();
                            var errorFormName = "BlankMasterErrorForm";
                            if (formDef != null && !formDef.NonSecure && currentContext.Request["s"] == null)
                                errorFormName = "ErrorForm";
                            var url = a.GetUrl(errorFormName, null, null, null);
                            if (a.Request != null)
                            {
                                System.Collections.Specialized.NameValueCollection tempPageUrl = null;
                                if (a.Request.UrlReferrer != null)
                                    tempPageUrl = HttpUtility.ParseQueryString(a.Request.UrlReferrer.Query);
                                tempPageUrl = HttpUtility.ParseQueryString(a.Request.Url.Query);
                            }
                            if (id == "l")
                                id = a.sessionData.loginUserId;
                            //url += "&Message: " + HttpUtility.UrlEncode(ex.Message);
                            var stempData = AppPress.TryGetSessionData();
                            if (stempData == null)
                                throw;
                            a.sessionData.UserData[AppPressKeys.ErrorFormException] = xEx;
                            a.sessionData.AddSecureUrl(url);
                            a.Response.Redirect(url);
                        }
                        else
                            if (ex.InnerException != null)
                            throw new Exception(ex.InnerException.Message + "\n\nStack Trace:\n" + ex.InnerException.StackTrace);
                        else
                            throw;

                    }
                }
                finally
                {
                    site.Close();
                }
            }
            Log.Writeln("Response: " + currentContext.Request.Url.AbsoluteUri);
        }


    }
}

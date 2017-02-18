using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using System.Net.Mail;


namespace AppPressFramework
{
    internal class MailProvider
    {
        SmtpClient objSmtpClient = null;

        #region PrivateVeriable   //Declare all Local Veriables related to email

        //========== Message Related ==========
        private string _MailFrom;
        private string _MailFromName;
        private List<string> _MailTo = new List<string>();
        private List<string> _MailCC = new List<string>();
        private List<string> _MailBCC = new List<string>();

        private List<Attachment> _MailAttachments = new List<Attachment>();

        private string _Subject;
        private string _Message;
        private bool _IsBodyHtml = true;
        private bool _EnableSSL = false;

        private MailPriority _MailPriority = MailPriority.Normal;

        private System.Text.Encoding _MessageEncoding = System.Text.Encoding.UTF8;
        //=====================================
        #endregion


        #region Properties   //Declare all Properties related to email

        /// <summary>
        ///  Add Sender Address 
        /// </summary>
        internal string MailFrom
        {
            get
            {
                return _MailFrom;
            }
            set
            {
                _MailFrom = value;
            }
        }
        internal string MailFromName
        {
            get
            {
                return _MailFromName;
            }
            set
            {
                _MailFromName = value;
            }
        }

        /// <summary>
        /// Add Receipient Address (TO) 
        /// </summary>
        public List<string> MailTo
        {
            get
            {
                return _MailTo;
            }
            set
            {
                _MailTo = value;
            }
        }

        /// <summary>
        ///  Add Receipient Address (CC)
        /// </summary>
        public List<string> MailCC
        {
            get
            {
                return _MailCC;
            }
            set
            {
                _MailCC = value;
            }
        }

        /// <summary>
        ///  Add Receipient Address (BCC) 
        /// </summary>
        public List<string> MailBCC
        {
            get
            {
                return _MailBCC;
            }
            set
            {
                _MailBCC = value;
            }
        }

        /// <summary>
        ///  Subject 
        /// </summary>
        public string Subject
        {
            get
            {
                return _Subject;
            }
            set
            {
                _Subject = value;
            }
        }

        /// <summary>
        ///  Message Body 
        /// </summary>
        public string Message
        {
            get
            {
                return _Message;
            }
            set
            {
                _Message = value;
            }
        }

        /// <summary>
        ///  Message Attachment (Optional) 
        ///  File Path of the attachment(file)
        ///  Server.MapPath
        /// </summary>
        public List<Attachment> MailAttachments
        {
            get
            {
                return _MailAttachments;
            }
            set
            {
                _MailAttachments = value;
            }
        }

        /// <summary>
        /// SSL required to send mail
        /// </summary>
        public bool EnableSSL
        {
            get
            {
                return _EnableSSL;
            }
            set
            {
                _EnableSSL = value;
            }


        }

        /// <summary>
        ///  Message Body Html or Text 
        /// </summary>
        public bool IsBodyHtml
        {
            get
            {
                return _IsBodyHtml;
            }
            set
            {
                _IsBodyHtml = value;
            }


        }

        /// <summary>
        /// Specifies the priority of a System.Net.Mail.MailMessage.
        /// </summary>
        public MailPriority Priority
        {
            get
            {
                return _MailPriority;
            }
            set
            {
                _MailPriority = value;
            }
        }


        /// <summary>
        /// Specifies the Message Encoding of a System.Net.Mail.MailMessage.
        /// Default encoding UTF-8
        /// System.Text.Encoding
        /// </summary>
        public System.Text.Encoding MessageEncoding
        {
            get
            {
                return _MessageEncoding;
            }
            set
            {
                MessageEncoding = value;
            }
        }

        /// <summary>
        /// Send Email
        /// </summary>
        #endregion


        #region Constructor   //Declare Constructor related to email

        public MailProvider()
        {
            try
            {

                objSmtpClient = new SmtpClient();
                _EnableSSL = bool.Parse(ConfigurationManager.AppSettings["emailssl"].ToLower());

            }
            catch
            {

                _EnableSSL = false;
            }
        }
        public MailProvider(string SMTPHost)
        {

            try
            {

                objSmtpClient = new SmtpClient();
                objSmtpClient.Host = SMTPHost;
                objSmtpClient.Port = 25;
                _EnableSSL = bool.Parse(ConfigurationManager.AppSettings["emailssl"].ToLower());

            }
            catch
            {

                _EnableSSL = false;
            }
        }
        internal MailProvider(string SMTPHost, int SMTPPort)
        {

            try
            {

                objSmtpClient = new SmtpClient();
                objSmtpClient.Host = SMTPHost;
                objSmtpClient.Port = SMTPPort;
                _EnableSSL = bool.Parse(ConfigurationManager.AppSettings["emailssl"].ToLower());

            }
            catch
            {

                _EnableSSL = false;
            }
        }
        internal MailProvider(string SMTPHost, int SMTPPort, string UserName, string Password)
        {


            try
            {

                objSmtpClient = new SmtpClient();
                objSmtpClient.Host = SMTPHost;
                objSmtpClient.Port = SMTPPort;
                objSmtpClient.Credentials = new NetworkCredential(UserName, Password);
                if (ConfigurationManager.AppSettings["emailssl"].IsNullOrEmpty())
                    _EnableSSL = true;
                else
                _EnableSSL = bool.Parse(ConfigurationManager.AppSettings["emailssl"].ToLower());
            }
            catch
            {
                _EnableSSL = false;
            }

        }


        #endregion


        #region Methods //Declare methods related to mail

        /// <summary>
        /// send email pass the SendingStatus parameter with this methods
        /// O for failed, 1 for sucess
        /// </summary>
        /// <param name="SendingStatus"></param>
        internal string SendMail(out EmailSendStatus SendingStatus)
        {
            MailMessage objMailMessage = new System.Net.Mail.MailMessage();

            try
            {
                #region //Add Sender Address
                if (!string.IsNullOrEmpty(_MailFrom))
                    objMailMessage.From = _MailFromName == null ? new MailAddress(_MailFrom) : new MailAddress(_MailFrom, _MailFromName);

                #endregion

                #region //Add Receipient Address (TO)

                foreach (string to in _MailTo)
                {
                    if (to.Contains(";"))
                    {
                        foreach (string toadd in to.Split(';'))
                        {
                            if (!string.IsNullOrEmpty(toadd))
                            {
                                objMailMessage.To.Add(toadd);
                            }
                        }
                    }
                    else if (to.Contains(","))
                    {
                        foreach (string toadd in to.Split(','))
                        {
                            if (!string.IsNullOrEmpty(toadd))
                            {
                                objMailMessage.To.Add(toadd);
                            }
                        }
                    }
                    else if (!string.IsNullOrEmpty(to))
                        objMailMessage.To.Add(to);
                }
                #endregion

                #region //Add Receipient Address (CC)
                foreach (string cc in _MailCC)
                {

                    if (cc.Contains(";"))
                    {
                        foreach (string add in cc.Split(';'))
                        {
                            if (!string.IsNullOrEmpty(add))
                            {
                                objMailMessage.CC.Add(add);
                            }
                        }
                    }
                    else if (cc.Contains(","))
                    {
                        foreach (string add in cc.Split(','))
                        {
                            if (!string.IsNullOrEmpty(add))
                            {
                                objMailMessage.CC.Add(add);
                            }
                        }
                    }
                    else if (!string.IsNullOrEmpty(cc))
                        objMailMessage.CC.Add(cc);
                }
                #endregion

                #region //Add Receipient Address (BCC)
                foreach (string bcc in _MailBCC)
                {
                    if (bcc.Contains(";"))
                    {
                        foreach (string add in bcc.Split(';'))
                        {
                            if (!string.IsNullOrEmpty(add))
                            {
                                objMailMessage.Bcc.Add(add);
                            }
                        }
                    }
                    else if (bcc.Contains(","))
                    {
                        foreach (string add in bcc.Split(','))
                        {
                            if (!string.IsNullOrEmpty(add))
                            {
                                objMailMessage.Bcc.Add(add);
                            }
                        }
                    }
                    else if (!string.IsNullOrEmpty(bcc))
                        objMailMessage.Bcc.Add(bcc);
                }

                #endregion

                #region //Add Subject
                objMailMessage.Subject = _Subject;
                #endregion

                #region //Message Body Html or Text
                objMailMessage.IsBodyHtml = _IsBodyHtml;
                #endregion

                #region //Message Priority
                objMailMessage.Priority = _MailPriority;
                #endregion

                #region // Message Delivery Notification
                objMailMessage.DeliveryNotificationOptions = DeliveryNotificationOptions.OnFailure;
                #endregion

                #region //======== Message Body ==========
                if (_IsBodyHtml == true)
                {
                    #region // This way prevent message to be SPAM
                    objMailMessage.BodyEncoding = System.Text.Encoding.GetEncoding("utf-8");
                    AlternateView plainView = AlternateView.CreateAlternateViewFromString
                        (System.Text.RegularExpressions.Regex.Replace(_Message, @"<(.|\n)*?>", string.Empty),
                        null, "text/plain");
                    AlternateView htmlView = AlternateView.CreateAlternateViewFromString(_Message, null, "text/html");

                    objMailMessage.AlternateViews.Add(plainView);
                    objMailMessage.AlternateViews.Add(htmlView);
                    #endregion
                }
                else
                    objMailMessage.Body = _Message;
                #endregion

                #region //Message Encoding
                objMailMessage.BodyEncoding = _MessageEncoding;
                #endregion

                #region //Attachment
                foreach (Attachment ObjAttachment in _MailAttachments)
                {
                    objMailMessage.Attachments.Add(ObjAttachment);
                }
                #endregion

                #region //SSL checking
                objSmtpClient.EnableSsl = _EnableSSL;
                #endregion
                #region // Message Sent
               objSmtpClient.Send(objMailMessage);
                #endregion
                SendingStatus = EmailSendStatus.SuccessFullySent;
            }
            catch (SmtpFailedRecipientsException ex)
            {
                if (ex.InnerExceptions.Length > 0)
                {
                    SmtpStatusCode status = ex.InnerExceptions[0].StatusCode;
                    if (status == SmtpStatusCode.MailboxBusy)
                    {
                        SendingStatus = EmailSendStatus.MailboxBusyError;
                    }
                    else if (status == SmtpStatusCode.MailboxUnavailable)
                    {
                        SendingStatus = EmailSendStatus.MailboxUnavailableError;
                    }
                    else
                    {
                        SendingStatus = EmailSendStatus.OtherError;
                    }
                    return ex.InnerExceptions[0].Message;
                }
                else
                {
                    SendingStatus = EmailSendStatus.OtherError;
                    return ex.Message;
                }
            }
            catch (Exception ex)
            {
                SendingStatus = EmailSendStatus.OtherError;
                if (ex.InnerException != null)
                    return ex.InnerException.Message;
                return ex.Message;
            }
            finally
            {
                if (objMailMessage != null)
                    objMailMessage.Dispose();
            }
            return null;

        }


        void smtp_SendCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {

            System.Net.Mail.MailMessage mail = (System.Net.Mail.MailMessage)e.UserState;
            try
            {
                if (e.Error != null)
                {
                    Exception ex = new Exception(e.Error.ToString());
                    try
                    {
                        // throw ex;
                    }
                    finally
                    {
                        ex = null;
                    }

                }
            }
            catch
            {
                if (mail != null)
                    mail.Dispose();
            }
        }
        #endregion


    }


    internal enum EmailSendStatus
    {
        SuccessFullySent,
        MailboxBusyError,
        MailboxUnavailableError,
        OtherError
    }

}
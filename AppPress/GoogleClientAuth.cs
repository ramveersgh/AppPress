using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net;

namespace AppPressFramework
{
	public class RequestHelper
	{
		
		public static string GetGoogleAuthenticationToken(string userName, string password)
		{
         string response = RequestHelper.GetPostResult(
		"https://www.google.com/accounts/clientlogin",
		String.Format("accountType=GOOGLE&Email={0}&Passwd={1}&service=lh2&source=Google-cURL-mail", userName, password));

         if (response.Contains("Auth="))
		{
			foreach (string line in response.Split(Environment.NewLine.ToCharArray()))
			{
				if (line.StartsWith("Auth="))
				{
					response = line.Replace("Auth=", "");
					break;
				}
			}
		}
		else
		{
			response = "Error:" + response;
		}
			return response;
		}
		public static string GetPostResult(string url, string strPost)
		{
			string strResponse = "";
			try
			{
				UTF8Encoding objUTFEncode = new UTF8Encoding();
				byte[] arrRequest;
				Stream objStreamReq;
				StreamReader objStreamRes;
				HttpWebRequest objHttpRequest;
				HttpWebResponse objHttpResponse;
				Uri objUri = new Uri(url);

				objHttpRequest = (HttpWebRequest)HttpWebRequest.Create(objUri);
				objHttpRequest.KeepAlive = false;
				objHttpRequest.Method = "POST";

				objHttpRequest.ContentType = "application/x-www-form-urlencoded";
				arrRequest = objUTFEncode.GetBytes(strPost);
				objHttpRequest.ContentLength = arrRequest.Length;

				objStreamReq = objHttpRequest.GetRequestStream();
				objStreamReq.Write(arrRequest, 0, arrRequest.Length);
				objStreamReq.Close();

				//Get response
				objHttpResponse = (HttpWebResponse)objHttpRequest.GetResponse();
				objStreamRes = new StreamReader(objHttpResponse.GetResponseStream(), Encoding.ASCII);

				strResponse = objStreamRes.ReadToEnd();
				objStreamRes.Close();
			}
			catch(Exception ex)
			{
				strResponse = "Error:" + ex.Message;
                throw new Exception(strResponse);
			}
			return strResponse;
		}
		
	}
}


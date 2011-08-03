using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Linq;
using System.Collections;

using Banking.Contract;
using log4net;

namespace Banking.Provider.LibertyReserve
{
	public static class LibertyReserve
	{
		private static ILog log = log4net.LogManager.GetLogger (typeof(LibertyReserve));
		
		private static string GetAuthToken (string secret)
		{
			// LR specific auth token depending on Secret and current time, returned in HEX
			string token = string.Format (secret + ":{0:yyyy}{0:MM}{0:dd}:{0:HH}",
				                              DateTime.Now.ToUniversalTime ());
			SHA256Managed hasher = new SHA256Managed ();
			byte[] hash = hasher.ComputeHash (Encoding.ASCII.GetBytes (token));
			
			return BitConverter.ToString (hash).Replace ("-", "");
		}

		private static string GetAuth (string apiName, string secret)
		{
			var b = new StringBuilder ();
			b.Append ("<Auth><ApiName>" + apiName + "</ApiName>");
			b.Append ("<Token>" + GetAuthToken (secret) + "</Token></Auth>");
			return b.ToString ();
		}

		public static string GetHistory (LRAccount acc, DateTime startDate,
			DateTime tillDate, string direction = "incoming")
		{
			string startString = string.Format ("{0:yyyy}-{0:dd}-{0:MM} {0:HH}:{0:mm}:{0:ss}", startDate);
			string endString = string.Format ("{0:yyyy}-{0:dd}-{0:MM} {0:HH}:{0:mm}:{0:ss}", tillDate);
			
			var reqString = new StringBuilder ();
			reqString.Append ("<HistoryRequest id=\"999999\">");
			reqString.Append (GetAuth (acc.ApiName, acc.Secret));
			reqString.Append ("<History>");
			reqString.Append ("<CurrencyId>" + acc.Currency.ToString () + "</CurrencyId>");
			reqString.Append ("<AccountId>" + acc.AccountIdentifier + "</AccountId>");
			reqString.Append ("<From>" + startString + "</From>");
			reqString.Append ("<Till>" + endString + "</Till>");
			reqString.Append ("<Direction>" + direction + "</Direction>");
			reqString.Append ("<Pager><PageSize>99999</PageSize></Pager>");
			reqString.Append ("</History></HistoryRequest>");
			
			ServicePointManager.CertificatePolicy = new CertificatePolicy ();
			string Url = "https://api.libertyreserve.com/xml/history.aspx?req=" + reqString.ToString ();

			log.Debug ("retrieving history, from: " + startString + ", to: " + endString);
			log.Debug ("Calling LR API at Url: " + Url);
			HttpWebRequest req = (HttpWebRequest)WebRequest.Create (Url);
			req.Method = "GET";
		
			WebResponse resp = req.GetResponse ();
			var answer = new StreamReader (resp.GetResponseStream ());
			var ret = answer.ReadToEnd ();
			log.Debug (ret);
			return ret;
		}

		public static string GetBalance (LRAccount account)
		{
			var reqString = new StringBuilder ();
			reqString.Append ("<BalanceRequest id=\"999999\">");
			reqString.Append (GetAuth (account.ApiName, account.Secret));
			reqString.Append ("<Balance>");
			reqString.Append ("<CurrencyId>" + account.Currency + "</CurrencyId>");
			reqString.Append ("<AccountId>" + account.AccountIdentifier + "</AccountId>");
			reqString.Append ("</Balance>");
			reqString.Append ("</BalanceRequest>");
			
			ServicePointManager.CertificatePolicy = new CertificatePolicy ();
			string Url = "https://api.libertyreserve.com/xml/balance.aspx?req=" + reqString.ToString ();

			log.Debug ("retrieving balance for account: " + account.AccountIdentifier + ", API: " + account.ApiName);
			log.Debug ("Calling LR API at Url: " + Url);
			HttpWebRequest req = (HttpWebRequest)WebRequest.Create (Url);
			req.Method = "GET";
		
			WebResponse resp = req.GetResponse ();
			var answer = new StreamReader (resp.GetResponseStream ());
			var responseXML = answer.ReadToEnd ();
			log.Debug (responseXML);
			return responseXML;
		}

		// Dummy class to disable SSL Cert checking
		public class CertificatePolicy : ICertificatePolicy
		{
			public bool CheckValidationResult (ServicePoint sp, 
				X509Certificate certificate, WebRequest request, int error)
			{
				return true;
			}
		}
	}
}

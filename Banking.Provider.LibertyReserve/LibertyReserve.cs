using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml.Linq;
using System.Linq;

using log4net;

namespace Banking.Provider.LibertyReserve
{
	public enum AccountCurrency
	{
		LRUSD,
		LREUR,
		LRGLD
	}
	
	class LibertyReserve
	{
		protected ILog log = log4net.LogManager.GetLogger (typeof(LibertyReserve));
		public string AccountNumber;
		public string ApiName;
		public string Secret;
		public AccountCurrency Currency = AccountCurrency.LRUSD;
		
		protected string AuthToken {
			// LR specific auth token depending on Secret and current time, returned in HEX
			get {
				string token = string.Format (Secret + ":{0:yyyy}{0:MM}{0:dd}:{0:HH}",
				                              DateTime.Now.ToUniversalTime ());
				SHA256Managed hasher = new SHA256Managed ();
				byte[] hash = hasher.ComputeHash (Encoding.ASCII.GetBytes (token));
				
				return BitConverter.ToString (hash).Replace ("-", "");
			}
		}

		public LibertyReserve ()
		{
		}

		public LibertyReserve (string AccountNumber, string ApiName, string Secret)
		{
			this.AccountNumber = AccountNumber;
			this.ApiName = ApiName;
			this.Secret = Secret;
		}

		public string GetAuth ()
		{
			var b = new StringBuilder ();
			b.Append ("<Auth><ApiName>" + this.ApiName + "</ApiName>");
			b.Append ("<Token>" + this.AuthToken + "</Token></Auth>");
			return b.ToString ();
		}
		/// <summary>
		///  performs as history check of transaction in the given timerange
		/// </summary>
		/// <param name="StartDate">
		/// A <see cref="DateTime"/>
		/// </param>
		/// <param name="TillDate">
		/// A <see cref="DateTime"/>
		/// </param>
		/// <returns>
		/// returns basic xml (but not formated in an xml envelope due to LR api
		/// A <see cref="System.String"/>
		/// </returns>
		public string GetHistory (DateTime StartDate, DateTime TillDate)
		{
			string startString = string.Format ("{0:yyyy}-{0:dd}-{0:MM} {0:HH}:{0:mm}:{0:ss}", StartDate);
			string endString = string.Format ("{0:yyyy}-{0:dd}-{0:MM} {0:HH}:{0:mm}:{0:ss}", TillDate);
			
			var reqString = new StringBuilder ();
			reqString.Append ("<HistoryRequest id=\"999999\">");
			reqString.Append (GetAuth ());
			reqString.Append ("<History>");
			reqString.Append ("<CurrencyId>" + this.Currency.ToString () + "</CurrencyId>");
			reqString.Append ("<AccountId>" + this .AccountNumber + "</AccountId>");
			reqString.Append ("<From>" + startString + "</From>");
			reqString.Append ("<Till>" + endString + "</Till>");
			reqString.Append ("<Direction>incoming</Direction>");
			reqString.Append ("<Pager><PageSize>99999</PageSize></Pager></History></HistoryRequest>");
			
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

		/// <summary>
		/// be aware that all times are UTC - you might call this function with DateTime.UtcNow
		/// </summary>
		/// <param name="Amount">
		/// A <see cref="System.Single"/>
		/// </param>
		/// <param name="Memo">
		/// A <see cref="System.String"/>
		/// </param>
		/// <param name="start">
		/// A <see cref="DateTime"/>
		/// </param>
		/// <param name="end">
		/// A <see cref="DateTime"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.Boolean"/>
		/// </returns>
		public bool CheckTransaction (float Amount, string Memo, DateTime start, DateTime end)
		{
			TextReader reader = new StringReader (GetHistory (start, end));
			XDocument xdoc = XDocument.Load (reader);
			XNamespace ns = "{http://www.tempuri.org/dsTrans.xsd}";
			
			var transfers = from t in xdoc.Descendants ("Transfer")
				where (string)t.Element ("Memo") == Memo &&
					(float)t.Element ("Amount") >= Amount
				select t;
			
			foreach (XElement el in transfers)
				log.Debug ("Found Transfer from Payee "
				                  + el.Element ("Payer").Value
				                  + " Amount: " + el.Element ("Amount").Value 
				                  + " Memo: " + el.Element ("Memo").Value);


			if (transfers.Count () > 0)
				return true;
			return false;
		}

		public void ListTransactions (DateTime start, DateTime end)
		{
			
			TextReader reader = new StringReader (GetHistory (start, end));
			XDocument xdoc = XDocument.Load (reader);
			XNamespace ns = "{http://www.tempuri.org/dsTrans.xsd}";
			
			var transfers = from t in xdoc.Descendants ("Transfer")
				select t;
			
			foreach (XElement el in transfers)
				log.Debug (el.Element ("Payer").Value
					+ " Amount: " + el.Element ("Amount").Value 
					+ " Memo: " + el.Element ("Memo").Value);
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

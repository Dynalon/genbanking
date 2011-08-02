using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Linq;

using Banking.Contract;
using log4net;
using System.Collections;

namespace Banking.Provider.LibertyReserve
{
	public class LRTransaction : ITransaction
	{
		public float Amount { get; set; }

		public string Currency { get; set; }
		
		public DateTime Date { get; set; }

		public IBankAccount FromAccount {
			get { return (IBankAccount)LRFromAccount; }
			set { LRFromAccount = value as LRAccount; }
		}

		internal LRAccount LRFromAccount;

		public List<string> Purposes { get; set; }

		public IBankAccount ToAccount {
			get { return (IBankAccount)LRToAccount; }
			set { LRToAccount = value as LRAccount; }
		}

		internal LRAccount LRToAccount;

		public DateTime ValutaDate { get; set; }
		
		public LRTransaction ()
		{
			Currency = "LRUSD";
			Purposes = new List<string> ();
			LRFromAccount = new LRAccount ();
			LRToAccount = new LRAccount ();
		}
	}
	
	public class LibertyReserve
	{
		protected ILog log = log4net.LogManager.GetLogger (typeof(LibertyReserve));
		public string AccountNumber;
		public string ApiName;
		public string Secret;
		public string Currency = "LRUSD";
		
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
		protected string GetHistory (DateTime StartDate, DateTime TillDate, string direction = "incoming")
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
			reqString.Append ("<Direction>" + direction + "</Direction>");
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

		public List<ITransaction> ListTransactions (DateTime start, DateTime end)
		{
			TextReader reader = new StringReader (GetHistory (start, end, direction: "any"));
			XDocument xdoc = XDocument.Load (reader);
			var receipts = from t in xdoc.Descendants ("Receipt") select t;
			
			var l = new List<ITransaction> ();
			foreach (XElement rc in receipts) {
				var t = new LRTransaction ();
				
				t.ValutaDate = t.Date = DateTime.Parse (rc.Element ("Date").Value);
				// childnode Transfer
				var el = rc.Element ("Transfer");
				
				t.FromAccount = new LRAccount (){ AccountIdentifier = el.Element("Payer").Value };
				t.FromAccount.OwnerName.Add (rc.Element ("PayerName").Value);
				t.ToAccount = new LRAccount () { AccountIdentifier = el.Element("Payee").Value };
				t.ToAccount.OwnerName.Add (rc.Element ("PayeeName").Value);
				t.Amount = float.Parse (el.Element ("Amount").Value);
				//t.Currency = el.Element ("Currency").Value;
				t.Purposes.Add (el.Element ("Memo").Value);
				
				l.Add (t);
			}
			return l;
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

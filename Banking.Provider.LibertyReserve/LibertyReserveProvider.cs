using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel;
using System.Linq;

using log4net;

using Banking.Contract;
using System.IO;
using System.Xml.Linq;

namespace Banking.Provider.LibertyReserve
{
	[Export(typeof(IBankingProvider))]
	[ExportMetadata("Name", "libertyreserve")]
	public class LRBankingProvider : IBankingProvider
	{
		protected ILog log = log4net.LogManager.GetLogger (typeof(LRBankingProvider));
		
		public IBankAccount GetAccountByIdentifier (string accountIdentifier)
		{
			return Accounts.First ();
		}

		public void Init (ProviderConfig config)
		{
			this.Accounts = new List<IBankAccount> ();
			var acc = new LRAccount ();
		
			this.Config = config;
		
			// retrieve list of accounts is not supported in LR, we only know of
			// one if its specified in config
			if (config.Settings ["Account"] == null)
				throw new Exception ("Account MUST be specified for LR");
			acc.AccountIdentifier = config.Settings ["Account"].Value as string;
	
			if (config.Settings ["ApiName"] == null) 
				throw new Exception ("ApiName is required");
			acc.ApiName = config.Settings ["ApiName"].Value as string;
			
			if (config.Settings ["Secret"] == null)
				throw new Exception ("An API Secret must be set");
			acc.Secret = config.Settings ["Secret"].Value;

			if (config.Settings ["Currency"] != null)
				acc.Currency = config.Settings ["Currency"].Value;

			// end configuration	
			
			Accounts.Add (acc);
		}

		public void Setup (object config)
		{
			throw new System.NotImplementedException ();
		}

		public float GetBalance (IBankAccount account)
		{
			var acc = GetAccountByIdentifier (account.AccountIdentifier) as LRAccount;
		
			var responseXML = LibertyReserve.GetBalance (acc);
			// parse XML response
			var reader = new StringReader (responseXML);
			XDocument xdoc = XDocument.Load (reader);
			var balance =
				(from b in xdoc.Descendants ("Balance")
				where b.Element("AccountId").Value == account.AccountIdentifier
				select float.Parse(b.Element("Value").Value)).First ();
			
			return balance;	
		}

		public List<ITransaction> GetTransactions (IBankAccount account)
		{
			return GetTransactions (account, DateTime.UtcNow - new TimeSpan (14, 0, 0, 0), DateTime.UtcNow);
		}

		public List<ITransaction> GetTransactions (IBankAccount account, DateTime start, DateTime end)
		{
			var lracc = GetAccountByIdentifier (account.AccountIdentifier) as LRAccount;

			TextReader reader = new StringReader (LibertyReserve.GetHistory (lracc, start, end, "any"));
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
			
				//if (el.Element ("Anonymous").Value == "true")
				//	t.ToAccount = t.FromAccount;
				t.ToAccount = new LRAccount () { AccountIdentifier = el.Element("Payee").Value };
				t.ToAccount.OwnerName.Add (rc.Element ("PayeeName").Value);
			
				t.Amount = float.Parse (el.Element ("Amount").Value);
				t.Currency = el.Element ("CurrencyId").Value;
				t.Purposes.Add (el.Element ("Memo").Value);
				
				l.Add (t);
			}
			return l;
		}

		public List<IBankAccount> Accounts { get; set; }

		public ProviderConfig Config { get; set; }

		public void Dispose ()
		{
		}
	}
	
	public class LRAccount : IBankAccount
	{
		public string AccountIdentifier { get; set; }

		public string BankIdentifier { get; set; }

		public string BankName { get; set; }

		public List<string> OwnerName { get; set; }
		
		internal string Secret;
		internal string ApiName;
		internal string Currency = "LRUSD";
		
		public LRAccount ()
		{
			BankName = "Liberty Reserve";
			BankIdentifier = "Liberty Reserve";
			OwnerName = new List<string> ();
		}
	}
	
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
}
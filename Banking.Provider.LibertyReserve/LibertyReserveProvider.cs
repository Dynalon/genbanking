using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel;

using log4net;

using Banking.Contract;

namespace Banking.Provider.LibertyReserve
{
	[Export(typeof(IBankingProvider))]
	[ExportMetadata("Name", "libertyreserve")]
	public partial class LRBankingProvider : IBankingProvider
	{
		protected ILog log;

		public IBankAccount GetAccountByIdentifier (string accountIdentifier)
		{
			return new LRBankAccount () { AccountIdentifier = "U8897283"};
		}

		public void Init (ProviderConfig config)
		{
			SetupLogger ();
			// retrieve list of accounts
			this.Accounts = new List<IBankAccount> ();
			Accounts.Add (new LRBankAccount () { AccountIdentifier = "U8897283" });
		}

		public void Setup (object config)
		{
			throw new System.NotImplementedException ();
		}

		public float GetBalance (IBankAccount account)
		{
			return 0.0f;
		}

		public List<ITransaction> GetTransactions (IBankAccount account)
		{
			var lr = new LibertyReserve (account.AccountIdentifier, "basic", "f00bar");
			lr.GetHistory (DateTime.Now - new TimeSpan (14, 0, 0, 0), DateTime.Now);
			
			return new List<ITransaction> ();
		}

		public List<ITransaction> GetTransactions (IBankAccount account, DateTime start, DateTime end)
		{
			throw new System.NotImplementedException ();
		}

		public List<IBankAccount> Accounts {
			get;
			set;
		}

		public ProviderConfig Config {
			get {
				throw new System.NotImplementedException ();
			}
		}

		public void Dispose ()
		{
		}

		private void SetupLogger ()
		{
			var appender = new log4net.Appender.ConsoleAppender ();
			appender.Layout = new log4net.Layout.PatternLayout ("%-4timestamp %-5level %logger %M %ndc - %message%newline");
			log4net.Config.BasicConfigurator.Configure (appender);	
#if DEBUG
			appender.Threshold = log4net.Core.Level.Debug;
#endif
			this.log = log4net.LogManager.GetLogger (this.GetType ());
		}
	}
	
	public class LRBankAccount : IBankAccount
	{
		public string AccountIdentifier {
			get;
			set;
		}

		public string BankIdentifier {
			get;
			set;
		}

		public string BankName {
			get;
			set;
		}

		public List<string> OwnerName {
			get;
			set;
		}
		
		public LRBankAccount ()
		{
			BankName = "Liberty Reserve";
			BankIdentifier = "Liberty Reserve";
			OwnerName = new List<string> ();
			OwnerName.Add ("Unknown");
		}
	}
}
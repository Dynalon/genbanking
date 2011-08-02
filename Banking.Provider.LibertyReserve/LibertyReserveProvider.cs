using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel;
using System.Linq;

using log4net;

using Banking.Contract;

namespace Banking.Provider.LibertyReserve
{
	[Export(typeof(IBankingProvider))]
	[ExportMetadata("Name", "libertyreserve")]
	public class LRBankingProvider : IBankingProvider
	{
		protected ILog log;
		protected LibertyReserve lr;

		public IBankAccount GetAccountByIdentifier (string accountIdentifier)
		{
			return Accounts.First ();
		}

		public void Init (ProviderConfig config)
		{
			SetupLogger ();
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
			// end configuration	
			
			Accounts.Add (acc);
		}

		public void Setup (object config)
		{
			throw new System.NotImplementedException ();
		}

		public float GetBalance (IBankAccount account)
		{
			throw new System.NotImplementedException ();
		}

		public List<ITransaction> GetTransactions (IBankAccount account)
		{
			return GetTransactions (account, DateTime.Now, DateTime.Now - new TimeSpan (14, 0, 0, 0));
		}

		public List<ITransaction> GetTransactions (IBankAccount account, DateTime start, DateTime end)
		{
			LRAccount lracc = account as LRAccount;
			var lr = new LibertyReserve (lracc.AccountIdentifier, lracc.ApiName, lracc.Secret);
			return lr.ListTransactions (
				DateTime.Now.ToUniversalTime () - new TimeSpan (14, 0, 0, 0),
				DateTime.Now.ToUniversalTime ()
			);
		}

		public List<IBankAccount> Accounts { get; set; }

		public ProviderConfig Config { get; set; }

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
	
	public class LRAccount : IBankAccount
	{
		public string AccountIdentifier { get; set; }

		public string BankIdentifier { get; set; }

		public string BankName { get; set; }

		public List<string> OwnerName { get; set; }
		
		internal string Secret;
		internal string ApiName;
		
		public LRAccount ()
		{
			BankName = "Liberty Reserve";
			BankIdentifier = "Liberty Reserve";
			OwnerName = new List<string> ();
		}
	}
}
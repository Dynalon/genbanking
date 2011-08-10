using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Configuration;
using System.IO;
using System.Linq;

using AqBanking;
using Banking.Contract;
using Banking.Provider;
using Banking.Provider.AqBanking.Gui;
using log4net;

namespace Banking.Provider.AqBanking
{
	[Export(typeof(IBankingProvider))]
	[ExportMetadata("Name", "aqbanking")]
	public partial class AqBankingProvider : AqBase, IBankingProvider
	{
		private SWIGTYPE_p_AB_BANKING abHandle;
		protected bool initDone = false;
		
		public ProviderConfig Config { get; set; }
		
		public IBankAccount GetAccountByIdentifier (string accountIdentifier)
		{
			var acc = from IBankAccount a in Accounts
				where a.AccountIdentifier == accountIdentifier
				select a;
			if (acc.Count () == 0)
				throw new Exception ("Account not found in local list");
			return acc.First ();
		}
		
		public List<IBankAccount> Accounts {
			get {
				// load account from aqbanking
				List<IBankAccount > accs = new List<IBankAccount> ();
				SWIGTYPE_p_AB_ACCOUNT_LIST2 accountList = AB.AB_Banking_GetAccounts (abHandle);
				if (accountList == null || accountList.Equals (IntPtr.Zero))
					throw new Exception ("No account set up");
				uint i = AB.AB_Account_List2_GetSize (accountList);
				
				// iterate over accounts and add them
				while (i>0) {
					SWIGTYPE_p_AB_ACCOUNT acc = AB.AB_Account_List2_GetFront (accountList);
					AqBankAccount account = new AqBankAccount (acc);
					accs.Add (account);
					AB.AB_Account_List2_PopFront (accountList);
					i--;
				}
				// free the account list
				AB.AB_Account_List2_free (accountList);
				return accs;
			}
		}

		public float GetBalance (IBankAccount account)
		{
			var acc = (AqBankAccount)GetAccountByIdentifier (account.AccountIdentifier);

			var job = new AqGetBalanceJob (acc, this.abHandle);
			job.Perform ();
			return job.RequestedBalance;
		}

		public List<ITransaction> GetTransactions (IBankAccount account)
		{
			var start = DateTime.Now - new TimeSpan (14, 0, 0, 0);
			var end = DateTime.Now;
			return GetTransactions (account, start, end);
		}

		public List<ITransaction> GetTransactions (IBankAccount account, DateTime start, DateTime end)
		{
			var acc = (AqBankAccount)GetAccountByIdentifier (account.AccountIdentifier);
			var job = new AqGetTransactionsJob (acc, this.abHandle);
			job.FromTime = start;
			job.ToTime = end;
			job.Perform ();
			return job.Transactions;
		}
		
		public void Init (ProviderConfig config)
		{
			if (initDone)
				return;

			this.Config = config;
			
			// configure aqbanking configuration path, default to $HOME/.aqbanking/
			string configPath = Path.Combine (System.Environment.GetEnvironmentVariable ("HOME"), ".aqbanking");	
			if (config.Settings ["ConfigPath"] != null)
				configPath = config.Settings ["ConfigPath"].Value;
			
			if (!Directory.Exists (configPath))
				throw new Exception ("configPath  " + configPath +
					"does not exist!");
			
			abHandle = AB.AB_Banking_new ("appstring", configPath, 0);
			
			// determine which gui to use
			string guiToUse = "";
			if (config.Settings ["Gui"] != null)
				guiToUse = config.Settings ["Gui"].Value;
			
			switch (guiToUse) {
			case "ManagedConsole":
				// our own simple console, implemented in managed code
				AqGuiHandler.SetGui (abHandle, new ConsoleGui ());
				break;
				
			case "AutoGui":
				// a non-interactive gui which requires pre-saved pin in the config
				var nigui = new AutoGui ();
			
				if (config.Settings ["Pin"] == null)
					throw new Exception ("AutoGui requires a pre-saved pin");

				nigui.Pin = config.Settings ["Pin"].Value;
				AqGuiHandler.SetGui (abHandle, nigui);
				break;
				
			case "CGui": goto default;
			default:
				// default GUI is AqBankings/Gwen internal CGui
				var gui = AB.GWEN_Gui_CGui_new ();
				AB.GWEN_Gui_SetGui (gui);
				AB.AB_Gui_Extend (gui, abHandle);
				break;
			}
			
			// initialise aqbanking
			int errcode = AB.AB_Banking_Init (abHandle);
			if (errcode != 0)
				throw new Exception ("AB_Banking_Init nicht erfoglreich, fehlercode: " + errcode);
			if (!abHandle.Equals (IntPtr.Zero))
				AB.AB_Banking_OnlineInit (abHandle);
			else
				throw new Exception ("Failed to initialize aqBanking");
			initDone = true;
			
			return;
		}

		public AqBankingProvider () : base()
		{
		}

		protected override void CleanUpNativeResource ()
		{
			base.CleanUpNativeResource ();
			AB.AB_Banking_OnlineFini (abHandle);
			AB.AB_Banking_Fini (abHandle);
			AB.AB_Banking_free (abHandle);
		}
	}
	
	/// <summary>
	/// Most of the AqBanking classes hold pointers to native code which sometimes
	/// must be freed explicitly. Thus we implement this helper class using IDisposable
	/// </summary>
	public class NativeDisposable : IDisposable
	{
		protected bool isCleanedUp = false;

		public virtual void Dispose ()
		{
			// if explicitly Disposed, no need to have a finalizer
			// anymore
			// GC.SuppressFinalize (this);

			if (!isCleanedUp)
				CleanUpNativeResource ();
			isCleanedUp = true;	
		}
	
		protected virtual void CleanUpNativeResource ()
		{	
		}

		~NativeDisposable ()
		{
			// 
			// freeing native ressources via the finalizer is currently disabled
			// this WILL LEAD TO MEMORY LEAKS! Right now, we tolerate the leaks
			// because the whole process (including vm) will crash if a finalizer
			// frees any AB_* object AFTER the main abHandle has been freed.
			
			// the right approach would be disable the finalizer AND make sure
			// the .Dispose() is called always on every object in the correct order.
			//
			// CleanUpNativeResource ();
		}
	}

	public class AqBase : NativeDisposable
	{
		protected ILog log;

		public AqBase ()
		{
			log = log4net.LogManager.GetLogger (this.GetType ());
		}
	}
}
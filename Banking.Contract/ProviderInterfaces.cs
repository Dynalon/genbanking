using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;

namespace Banking.Contract
{	
	public interface IBankingProvider : IDisposable
	{
		/// <summary>
		/// List of all available accounts from the provider
		/// </summary>
		List<IBankAccount> Accounts { get; }
		
		ProviderConfig Config { get; }
		
		/// <summary>
		/// Gets an account from the list of the available (self-owned) accounts by
		/// an identifier
		/// </summary>
		/// <param name="accountIdentifier">
		/// An account identifier which has to be a unique string, like i.e. a bank account number
		/// A <see cref="System.String"/>
		/// </param>
		/// <returns>
		/// A <see cref="IBankAccount"/>
		/// </returns>
		IBankAccount GetAccountByIdentifier (string accountIdentifier);

		/// <summary>
		/// performs initialization for the provider. Is called automatically by the framework 
		/// when loading the provider. Should not be called from within the provider.
		/// </summary>
		void Init (ProviderConfig config);
		
		/// <summary>
		/// Setup the specified config.
		/// </summary>
		/// <param name='config'>
		/// Config.
		/// </param>
		void Setup (object config);
		
		/// <summary>
		/// Gets the balance.
		/// </summary>
		/// <returns>
		/// The current balance of a given account
		/// </returns>
		/// <param name='account'>
		/// Account of which to retrieve the balance from.
		/// </param>
		float GetBalance (IBankAccount account);
		
		List<ITransaction> GetTransactions (IBankAccount account);
		
		List<ITransaction> GetTransactions (IBankAccount account, DateTime start, DateTime end);
		
		//void Transfer (float Amount, IBankAccount from, IBankAccount to);
	}
	
	/// <summary>
	/// Must not be implemented by an actual class in the provider, 
	/// but the properties must be specified via [ExportMetadata] on the class implementing <see cref="IBankingProvider">
	/// </summary>
	public interface IBankingProviderMetadata
	{
		/// <summary>
		/// Mandatory. A unique name for the provider.
		/// </summary>
		string Name { get; }
	}
	
	public interface IBankAccount
	{
		string BankIdentifier { get; set; }

		string BankName { get; set; }

		string AccountIdentifier { get; set; }

		List<string> OwnerName { get; set; }
		
	}
	
	public interface ITransaction
	{
		DateTime Date { get; set; }

		DateTime ValutaDate { get; set; }

		IBankAccount FromAccount { get; set; }

		IBankAccount ToAccount { get; set; }

		float Amount { get; set; }

		string Currency { get; set; }

		List<string> Purposes { get; set; }
	}
	
	public class ProviderConfig
	{
		// settings which are set via provider.config
		public KeyValueConfigurationCollection Settings = new KeyValueConfigurationCollection ();
		// custom settings which are generic for every backend
		public string ConfigFile;
		
		public ProviderConfig ()
		{
		}

		public ProviderConfig (string configFile)
		{
			if (!File.Exists (configFile))
				throw new Exception ("configFile: " + configFile + " does not exist!");
			
			ExeConfigurationFileMap configMap = new ExeConfigurationFileMap ();
			configMap.ExeConfigFilename = configFile;
			Configuration config = ConfigurationManager.OpenMappedExeConfiguration (configMap, ConfigurationUserLevel.None);

			this.Settings = config.AppSettings.Settings;
			this.ConfigFile = configFile;
		}
	}
}
using System;
using System.Web;
using System.Web.Services;

using Banking.Contract;
using Banking;
using System.Collections.Generic;
using AutoMapper;
using System.Xml.Serialization;
using System.Web.Script;
using System.Web.Script.Services;
using System.Configuration;
using System.IO;

namespace Banking.Web
{
	[ScriptService]
	[WebService]
	public class Banking : WebService
	{
		protected ProviderConfig config;

		public Banking ()
		{
			var conf = (string)ConfigurationManager.AppSettings ["ConfigFile"];
			config = new ProviderConfig (conf);
		}
		
		[WebMethod]
		[ScriptMethod(ResponseFormat = ResponseFormat.Json)]
		public List<Transaction> GetTransactions (string accountIdentifier)
		{
			using (var banking = new BankingFactory().GetProvider(config)) {
				
				var bAcc = banking.GetAccountByIdentifier (accountIdentifier);
				var transactions = banking.GetTransactions (bAcc);

				return Mapper.Map<List<ITransaction>,List<Transaction>> (transactions);
			}
		}

		[WebMethod]
		[ScriptMethod(ResponseFormat = ResponseFormat.Json)]
		public List<Transaction> GetTransactionsRange (string accountIdentifier, DateTime start, DateTime end)
		{
			using (var banking = new BankingFactory().GetProvider(config)) {
			
				var bAcc = banking.GetAccountByIdentifier (accountIdentifier);
				var transactions = banking.GetTransactions (bAcc);
			
				return Mapper.Map<List<ITransaction>, List<Transaction>> (transactions);
			}
		}
		
		[WebMethod]
		[ScriptMethod(ResponseFormat = ResponseFormat.Json)]
		public float GetBalance (string accountIdentifier)
		{
			using (var banking = new BankingFactory().GetProvider (config)) {
			
				var bAcc = banking.GetAccountByIdentifier (accountIdentifier);
				var balance = banking.GetBalance (bAcc);
				return balance;
			}
		}

		[WebMethod]
		[ScriptMethod(ResponseFormat = ResponseFormat.Json, UseHttpGet = true)]
		public List<BankAccount> GetAccounts ()
		{
			using (var banking = new BankingFactory().GetProvider(config)) {
			
				var l = Mapper.Map<List<IBankAccount>, List<BankAccount>> (banking.Accounts);
				return l;
			}
		}

		internal static void ConfigureAutomapper ()
		{
			Mapper.CreateMap<ITransaction, Transaction> ();
			Mapper.CreateMap<IBankAccount, BankAccount> ();
			Mapper.AssertConfigurationIsValid ();
		}
	}
}
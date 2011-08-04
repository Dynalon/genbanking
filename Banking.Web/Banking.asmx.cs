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
using System.Text;
using System.Security.Cryptography;
using log4net;

namespace Banking.Web
{
	[ScriptService]
	[WebService]
	public class Banking : WebService
	{
		protected enum PasskeyType
		{
			Plaintext,
			SHA1,
			SHA256
		}
		
		protected ProviderConfig config;
		protected string passkey;
		protected PasskeyType passkeyType = PasskeyType.Plaintext;
		protected ILog log = log4net.LogManager.GetLogger (typeof(Banking));
		
		public Banking ()
		{
			var conf = (string)ConfigurationManager.AppSettings ["ConfigFile"];
			config = new ProviderConfig (conf);
			
			passkey = (string)ConfigurationManager.AppSettings ["Passkey"];
			
			// see if we have SHA1 or SHA256 hashes
			try {
				// conversion will NOT yield correct integer result, but
				Convert.ToInt32 (passkey, 16);
				log.Debug (passkey);
				switch (passkey.Length) {
				case 40:
					passkeyType = PasskeyType.SHA1;
					break;
				case 64:
					passkeyType = PasskeyType.SHA256;
					break;
				default:
					passkeyType = PasskeyType.Plaintext;
					break;
				}
			} catch {
				// its not a hex represenation
				log.Debug ("caught exception");
				passkeyType = PasskeyType.Plaintext;
			}
		}

		[WebMethod(EnableSession = true)]
		[ScriptMethod(ResponseFormat = ResponseFormat.Json)]
		public bool Login (string passkey)
		{
			var pass = "";
			var enc = new UTF8Encoding ();
			switch (this.passkeyType) {
			// compute hash if the pass is stored hash in the config
			case PasskeyType.SHA1:
				// calculate the sha1 hash
				var sha1 = new SHA1CryptoServiceProvider ();
				pass = BitConverter.ToString (sha1.ComputeHash (enc.GetBytes (passkey))).Replace ("-", "");
				break;
			case PasskeyType.SHA256:
				var sha256 = new SHA256CryptoServiceProvider ();
				pass = BitConverter.ToString (sha256.ComputeHash (enc.GetBytes (passkey))).Replace ("-", "");
				break;
			default:
				pass = passkey;
				break;
			}
			
			if ((passkeyType == PasskeyType.Plaintext && pass == this.passkey) ||
				(passkeyType != PasskeyType.Plaintext && pass.ToLower () == this.passkey.ToLower ())) {
				Session ["auth"] = "true";
				return true;
			}
			return false;
		}

		[WebMethod(EnableSession = true)]
		[ScriptMethod(ResponseFormat = ResponseFormat.Json)]
		public void Logout ()
		{
			Session.Clear ();
		}

		protected void CheckAuth ()
		{
			if (Session ["auth"] != null && Session ["auth"] as string == "true")
				return;
			throw new Exception ("not logged in, need to call Login() first");
		}
		
		[WebMethod(EnableSession = true)]
		[ScriptMethod(ResponseFormat = ResponseFormat.Json)]
		public List<Transaction> GetTransactions (string accountIdentifier)
		{
			CheckAuth ();
			using (var banking = new BankingFactory().GetProvider(config)) {
				
				var bAcc = banking.GetAccountByIdentifier (accountIdentifier);
				var transactions = banking.GetTransactions (bAcc);

				return Mapper.Map<List<ITransaction>,List<Transaction>> (transactions);
			}
		}

		[WebMethod (EnableSession = true)]
		[ScriptMethod(ResponseFormat = ResponseFormat.Json)]
		public List<Transaction> GetTransactionsRange (string accountIdentifier, DateTime start, DateTime end)
		{
			CheckAuth ();
			using (var banking = new BankingFactory().GetProvider(config)) {
			
				var bAcc = banking.GetAccountByIdentifier (accountIdentifier);
				var transactions = banking.GetTransactions (bAcc);
			
				return Mapper.Map<List<ITransaction>, List<Transaction>> (transactions);
			}
		}
		
		[WebMethod (EnableSession = true)]
		[ScriptMethod(ResponseFormat = ResponseFormat.Json)]
		public float GetBalance (string accountIdentifier)
		{
			CheckAuth ();
			using (var banking = new BankingFactory().GetProvider (config)) {
			
				var bAcc = banking.GetAccountByIdentifier (accountIdentifier);
				var balance = banking.GetBalance (bAcc);
				return balance;
			}
		}

		[WebMethod (EnableSession = true)]
		[ScriptMethod(ResponseFormat = ResponseFormat.Json, UseHttpGet = true)]
		public List<BankAccount> GetAccounts ()
		{
			CheckAuth ();
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
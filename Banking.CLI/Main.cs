using System;
using Banking.Contract;
using System.Collections.Generic;

using Mono.Options;

namespace Banking.CLI
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			// set default options and parameter arguments
			string configfile = "";
			string account = "";
			string task = "";
			bool help = false;
			bool list = false;
			var p = new OptionSet () {
				{ "c=",	"configuration file to use. Default is provider.config in assembly directory",
					v => configfile = v },
				{ "a=", "AccountIdentifier/Number to use", v => account = v },
				{ "t=",	"task that should be performed. Can be getbalance or gettransactions", v => task = v },
				{ "l", "list all available accounts", v => list = v != null },
				{ "h|?|help", "shows this help",  v => help = v != null },
			};
						
			try {
				p.Parse (args);
				if (help) {
					p.WriteOptionDescriptions (Console.Out);
					return;
				}
		
				// init
				using (var banking = new BankingFactory().GetProvider(configfile)) {
					
					// output account overview
					if (list) {
						foreach (var acc in banking.Accounts)
							acc.Print ();
						return;
					}
					// account requests (Balance, Transactions)
					
					// parameter sanitation
					if (string.IsNullOrEmpty (account))
						throw new Exception ("AccountIdentifier needed, specify via -a <acc>");
					
					if (string.IsNullOrEmpty (task))
						throw new Exception ("Task needed, specify via -t <task>");
					
					IBankAccount b = banking.GetAccountByIdentifier (account);
					switch (task) {
					case "gettransactions":		
						List<ITransaction > l = banking.GetTransactions (b);
						foreach (ITransaction t in l)
							t.Print ();
						return;					
					case "getbalance":
						var bal = banking.GetBalance (b);
						Console.WriteLine (bal);
						return;
					}
				}
			} catch (Exception e) {
				Console.WriteLine ("ERROR: " + e.Message);
#if DEBUG
				throw e;
#endif
			}
			p.WriteOptionDescriptions (Console.Out);
			return;
		}
	}
}
using System;

using Banking.Contract;

namespace Banking.CLI
{
	public static class BankingOutput
	{
		public static void PrintAccountInformation (this IBankAccount acc)
		{
			Console.WriteLine ("Konto:\t" + acc.AccountIdentifier +
			                  "\tBLZ: " + acc.BankCode);
			/* if(acc.Users != null){
				Console.WriteLine("Benutzer:");
				foreach(BankAccountUser user in acc.Users){
					Console.WriteLine("CustomerId:\t" + user.CustomerId);
					Console.WriteLine("UserName:\t" + user.UserName);
					Console.WriteLine("UserId:\t" + user.UserId);
					Console.WriteLine();
				}
			}*/
			Console.WriteLine ();
		}
		// formatiert einen Kontoauszug und gibt ihn aus
		public static void Print (this ITransaction trans)
		{
			Console.WriteLine ("*********************************");
			trans.FromAccount.Print ();
			Console.WriteLine (" -> ");
			trans.ToAccount.Print ();
			
			Console.WriteLine ("Valuta:\t\t{0}", trans.ValutaDate);
			Console.WriteLine ("Date:\t\t{0}", trans.Date); 		
			Console.WriteLine ("Amount:\t\t{0}", string.Format ("{0:0.00}", trans.Amount));
			foreach (string purp in trans.Purposes)
				Console.WriteLine (purp);
			
		}

		public static void Print (this IBankAccount acc)
		{
			Console.Write (acc.AccountIdentifier);
			Console.Write (", " + acc.BankCode + ", " + acc.BankName);
			foreach (string owner in acc.OwnerName)
				Console.Write (", " + owner);
	
			Console.WriteLine ();
		}
	}
} /* EONS */
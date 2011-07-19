// We need "dummy" instances with no actual implementation to carry data
// i.e. to send it over the network and so on,
// we then can use Automapper to map between backend Classes and DTOs 
using Banking.Contract;
using System.Collections.Generic;
using System;

namespace Banking.Web
{
	public class BankAccount : IBankAccount
	{
		public BankAccount ()
		{
		}

		public string AccountIdentifier { get; set; }

		public string BankCode { get; set; }

		public string BankName { get; set; }

		public List<string> OwnerName { get; set; }
	}
	
	public class Transaction
	{
		public Transaction ()
		{
		}

		public float Amount { get; set; }

		public string Currency { get; set; }

		public DateTime Date { get; set; }
		
		public List<string> Purposes { get; set; }

		public BankAccount ToAccount { get; set; }

		public BankAccount FromAccount { get; set; }

		public DateTime ValutaDate { get; set; }
	}

}
using System;
using NUnit.Framework;
using Banking.Provider.AqBanking;
using System.Threading;

namespace Banking.Provider.AqBanking.Tests
{
	[TestFixture()]
	public class Tests
	{
		[Test()]
		public void AccountsReady ()
		{			
			//Assert.Greater(2,1);
			var ap = new AqBankingProvider ();
			Assert.Greater (ap.Accounts.Count, 0);
			foreach (var acc in ap.Accounts) {
				Assert.IsNotNull (acc.AccountIdentifier);
				Assert.Greater (acc.AccountIdentifier.Length, 0);
			}
		}

		[Test]
		public void DisposeSafety ()
		{
			// make sure multiple calls to dispose are safe
			var ap = new AqBankingProvider ();
			ap.Dispose ();
			ap.Dispose ();
			
			// single dispose
			ap = new AqBankingProvider ();
			ap.Dispose ();
			
			// make sure if no Dispose is called the
			// finalizers does the work
			ap = new AqBankingProvider ();
		}
	}
	
	[TestFixture]
	public class Stresstests
	{
		[Test()]
		public void AccountsStresstest ()
		{
			// stresstest to find memory leaks	
			Console.WriteLine ("starting stresstest");
			for (int i=0; i<100; i++) {
				using (var ap = new AqBankingProvider ()) {
					Console.WriteLine ("Run {0}", i);
					foreach (var acc in ap.Accounts) {
						Assert.Greater (acc.AccountIdentifier.Length, 0);
					}
				}
			}
		}
	}
}
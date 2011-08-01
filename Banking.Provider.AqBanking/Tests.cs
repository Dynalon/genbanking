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
		[Ignore]
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

		[Test]
		public void Test ()
		{
			Assert.AreEqual (1, 1);
		}
	}
}
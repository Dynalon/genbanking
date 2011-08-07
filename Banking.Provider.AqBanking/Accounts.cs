using System;
using System.Collections.Generic;
using Banking.Contract;
using AqBanking;

namespace Banking.Provider.AqBanking
{
	
	/// <summary>
	/// AqBankAccount which implements the <see cref="IBankAccount"/> 
	/// </summary>
	public class AqBankAccount : AqBase, IBankAccount
	{
		internal SWIGTYPE_p_AB_ACCOUNT AccHandle;
		
		//// <value>
		/// BankIdentifier which identifies the bank in national transactions,
		/// use BIC 
		/// for international transactions
		/// </value>
		public string BankIdentifier {
			get {
				if (!string.IsNullOrEmpty (BIC))
					return this.BIC;
				else
					return this.BLZ;
			}
			set {
				int p;
				if (int.TryParse (value, out p))
					this.BLZ = value;
				else
					this.BIC = value;
			}
		}

		internal string BIC;
		internal string BLZ;
		
		//// <value>
		/// Identification number of the Bankaccount which identifies the account
		/// IBAN/BIC format is prefered over national notation, this can be changed in the settings
		/// </value>
		public string AccountIdentifier {
			get {
				if (!string.IsNullOrEmpty (IBAN))
					return IBAN;
				else
					return AccountNumber;	
			}			
			set {
				if (string.IsNullOrEmpty (value))
					return;
				int p;
				if (int.TryParse (value.Substring (1, 2), out p))
					AccountNumber = value;
				else
					IBAN = value;
			}
		}

		internal string AccountNumber;
		internal string IBAN;
		
		public List<string> OwnerName { get; set; }
		
		/// <value>
		/// String representation of currency used in account
		/// </value>
		public string Currency = "";
		
		/// <value>
		/// Name of Account, often chosen by bank to reflect 
		/// their marketing name or the typ of account like
		/// "Girokonto", Cash account etc. This Property usually
		/// is only filled by backend and need not to be present
		/// </value>
		public string AccountName = "";
		
		/// <value>
		/// Name of the Bank, usually only filled from backend
		/// </value>
		public string BankName { get; set; }
		
		public AqBankAccount ()
		{
			// default values
			BankName = "";
			OwnerName = new List<string> ();
			AccountIdentifier = "";
		}
		/// <summary>
		/// Populates account data from an aqbanking accHandle for local predefined (self-owned) accounts from the
		/// aqbanking config files
		/// </summary>
		/// <param name="accHandle">
		/// A <see cref="SWIGTYPE_p_AB_ACCOUNT"/>
		/// </param>
		public AqBankAccount (SWIGTYPE_p_AB_ACCOUNT accHandle): this ()
		{
			this.AccHandle = accHandle;
			/* directly populates data from	aqbanking accountHandle	*/
			this.BLZ = AB.AB_Account_GetBankCode (accHandle);
			this.BIC = AB.AB_Account_GetBIC (accHandle);
			this.AccountNumber = AB.AB_Account_GetAccountNumber (accHandle);
			this.IBAN = AB.AB_Account_GetIBAN (accHandle);
			this.BankName = AB.AB_Account_GetBankName (accHandle);
			this.AccountName = AB.AB_Account_GetAccountName (accHandle);
			this.Currency = AB.AB_Account_GetCurrency (accHandle);
		}

		protected override void CleanUpNativeResource ()
		{
			base.CleanUpNativeResource ();
			// Account retrieved via AB_GetAccounts() MUST NOT
			// be freed manually - make sure this holds for all account objects?
			//if(AccHandle != null && !AccHandle.Equals(IntPtr.Zero))
			//	AB.AB_Account_free(AccHandle);
		}
	}

	public class AqTransaction : AqBase, ITransaction
	{
		#region ITransaction implementation
		public float Amount { get; set; }

		public string Currency { get; set; }

		public DateTime Date { get; set; }

		public IBankAccount FromAccount {
			get {
				return (IBankAccount)AqFromAccount;
			}
			set {
				AqFromAccount = (AqBankAccount)value;
			}
		}

		public IBankAccount ToAccount {
			get {
				return (IBankAccount)AqToAccount;
			}
			set {
				AqToAccount = (AqBankAccount)value;
			}
		}

		internal AqBankAccount AqToAccount;
		internal AqBankAccount AqFromAccount;

		public List<string> Purposes { get; set; }

		public DateTime ValutaDate { get; set; }

		#endregion
		
		internal SWIGTYPE_p_AB_TRANSACTION aqTransaction;
		
		public AqTransaction (SWIGTYPE_p_AB_TRANSACTION aqTransactionHandle)
		{
			this.aqTransaction = aqTransactionHandle;
			var val = AB.AB_Transaction_GetValue (aqTransaction);

			this.Amount = (float)AB.AB_Value_GetValueAsDouble (val);
			this.Currency = AB.AB_Value_GetCurrency (val);

			this.ValutaDate = AqHelper.fromGwenTimeToDateTime (AB.AB_Transaction_GetValutaDate (aqTransaction));
			this.Date = AqHelper.fromGwenTimeToDateTime (AB.AB_Transaction_GetDate (aqTransaction));	
			
			// populate ToAccount
			this.AqToAccount = new AqBankAccount ();	
			// AccountNumber & IBAN
			this.AqToAccount.IBAN = AB.AB_Transaction_GetRemoteIban (aqTransaction);
			this.AqToAccount.AccountNumber = AB.AB_Transaction_GetRemoteAccountNumber (aqTransaction);		
			// BankCode & BIC
			this.AqToAccount.BLZ = AB.AB_Transaction_GetRemoteBankCode (aqTransaction);
			this.AqToAccount.BIC = AB.AB_Transaction_GetRemoteBic (aqTransaction);
			this.AqToAccount.OwnerName = AqHelper.fromGwenStringList (AB.AB_Transaction_GetRemoteName (aqTransaction));
			this.AqToAccount.BankName = AB.AB_Transaction_GetRemoteBankName (aqTransaction);
			
			// populate FromAccount
			this.AqFromAccount = new AqBankAccount ();
			this.AqFromAccount.AccountNumber = AB.AB_Transaction_GetLocalAccountNumber (aqTransaction);
			this.AqFromAccount.IBAN = AB.AB_Transaction_GetLocalIban (aqTransaction);
			this.AqFromAccount.BLZ = AB.AB_Transaction_GetLocalBankCode (aqTransaction);
			this.AqFromAccount.BIC = AB.AB_Transaction_GetLocalBic (aqTransaction);
			this.AqFromAccount.AccountIdentifier = AB.AB_Transaction_GetLocalAccountNumber (aqTransaction);
			this.AqFromAccount.OwnerName = new List<string> ();
			this.AqFromAccount.OwnerName.Add (AB.AB_Transaction_GetLocalName (aqTransaction));
			
			this.Purposes = AqHelper.fromGwenStringList (AB.AB_Transaction_GetPurpose (aqTransaction));	
		}

		protected override void CleanUpNativeResource ()
		{
			base.CleanUpNativeResource ();
			if (aqTransaction == null || aqTransaction.Equals (IntPtr.Zero))
				AB.AB_Transaction_free (aqTransaction);
		}
	}
	
	public class AqBankAccountUser : AqBase
	{
		public SWIGTYPE_p_AB_USER aqUser;
		public string UserId;
		public string CustomerId;
		public string UserName;

		protected override void CleanUpNativeResource ()
		{
			base.CleanUpNativeResource ();
			AB.AB_User_free (aqUser);
		}
	}
}
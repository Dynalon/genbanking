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
		/* public IntPtr GetAccHandle(){
			return AB.Banking_FindAccount(AqHandler.abHandle,"*","*",
				this.BankCode, this.AccountNumber);
		}*/
		public SWIGTYPE_p_AB_ACCOUNT AccHandle;
		//// <value>
		/// BankCode which identifies the bank in national transactions,
		/// use BIC 
		/// for international transactions
		/// </value>
		public string BankCode { get; set; }
		
		//// <value>
		/// Identification number of the Bankaccount which identifies the account
		/// for national transactions, use 
		/// <see cref="P:IBAN"/> for international transactions 
		/// </value>
		public string AccountIdentifier { get; set; }
		
		/// <value>
		/// Name of Accountholder, may be more than one
		/// (i.e. married-couple account, company account)
		/// </value>
		// TODO see if XmlElement Attrib is actually needed
		//[XmlElement(IsNullable=false)]
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
		
		public List<AqBankAccountUser> ReadUserList ()
		{
			throw new Exception ("is this in use?");
			// get list of users from aqbanking
			SWIGTYPE_p_AB_USER_LIST2 aqUserlist = AB.AB_Account_GetUsers (AccHandle);
			if (aqUserlist.Equals (IntPtr.Zero))
				throw new Exception ("could not retrive userlist from aqbanking");
			
			List<AqBankAccountUser > userlist = new List<AqBankAccountUser> ();
			uint j = AB.AB_User_List2_GetSize (aqUserlist);
			while (j>0) {
				SWIGTYPE_p_AB_USER aqUser = AB.AB_User_List2_GetFront (aqUserlist);
				var user = new AqBankAccountUser ();
				user.CustomerId = AB.AB_User_GetUserId (aqUser);
				user.UserId = AB.AB_User_GetUserName (aqUser);
				user.UserName = AB.AB_User_GetCustomerId (aqUser);
				userlist.Add (user);
				AB.AB_User_List2_PopFront (aqUserlist);
				j--;
			}
			// cleanup
			AB.AB_User_List2_free (aqUserlist);
			return userlist;
		}

		public void ReadAccountLimits ()
		{
			// FIXME re-enable this and find out why i did this
			/* this.Limits = new TransactionLimits[Enum.GetNames(typeof(TransactionType)).Length];
			foreach(TransactionType type in TransactionType.GetValues(typeof(TransactionType))){
				this.Limits[(byte) type] = new TransactionLimits(this, type);
			}*/
		}

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
			// TODO complete this
			this.AccHandle = accHandle;
			/* directly populates data from	aqbanking accountHandle	*/
			this.BankCode = AB.AB_Account_GetBankCode (accHandle);
			this.AccountIdentifier = AB.AB_Account_GetAccountNumber (accHandle);
			this.BankName = AB.AB_Account_GetBankName (accHandle);
			//this.Currency = Marshal.PtrToStringAuto(AB.Account_GetCurrency(accHandle));
			//this.BankCountry = Marshal.PtrToStringAuto(AB.Account_GetCountry(accHandle));
			this.AccountName = AB.AB_Account_GetAccountName (accHandle);	

			//AB.Account_GetAccountName(accHandle));
			
			//this.ReadUserList(accHandle);
			//this.ReadAccountLimits();	
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

		public IBankAccount FromAccount { get; set; }

		public List<string> Purposes { get; set; }

		public IBankAccount ToAccount { get; set; }

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
			this.FromAccount = new AqBankAccount ();
			this.FromAccount.BankCode = AB.AB_Transaction_GetRemoteBankCode (aqTransaction);
			this.FromAccount.AccountIdentifier = AB.AB_Transaction_GetRemoteAccountNumber (aqTransaction);
			this.FromAccount.OwnerName = AqHelper.fromGwenStringList (AB.AB_Transaction_GetRemoteName (aqTransaction));
			this.FromAccount.BankName = AB.AB_Transaction_GetRemoteBankName (aqTransaction);
			
			// populate FromAccount
			this.ToAccount = new AqBankAccount ();
			this.ToAccount.BankCode = AB.AB_Transaction_GetLocalBankCode (aqTransaction);
			this.ToAccount.AccountIdentifier = AB.AB_Transaction_GetLocalAccountNumber (aqTransaction);
			this.ToAccount.OwnerName = new List<string> ();
			this.ToAccount.OwnerName.Add (AB.AB_Transaction_GetLocalName (aqTransaction));
			
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
	// TODO re-enable this class or throw it out
	/*
	public class TransactionLimits
	{
		//	Holds information abount account-specific
		//	limits and abilities for a transaction
			
		// TODO better enum/TransactionType Serialising
		public TransactionType Type;
		
		public int MaxLenRemoteName;
		public int MinLenRemoteName;
		public int MaxLinesRemoteName;
		public int MinLinesRemoteName;
		
		public int MaxLinesPurpose;
		public int MinLinesPurpose;
		public int MaxLenPurpose;
		public int MinLenPurpose;
		
		public int MaxLenRemoteBankCode;
		public int MinLenRemoteBankCode;
		public int MaxLenRemoteAccountNumber;
		public int MinLenRemoteAccountNumber;
		
		// TextKeys - always ensure textkey is supportet
		[XmlElement(Type=typeof(string))]
		public ArrayList TextKeys = new ArrayList();
		
		public TransactionLimits(){
		}
		public TransactionLimits(BankAccount account, TransactionType transType){
			//read according limits for given account 
			//regretably, we need to create a fake job since AqBanking
			//an only get limits from jobs 
			IntPtr aqJob; IntPtr limit;
			limit=IntPtr.Zero;
			IntPtr aqTransaction = AB.Transaction_new();
			AB.Transaction_FillLocalFromAccount(aqTransaction, account.GetAccHandle());
			this.Type = transType;
			switch(transType){
			// TODO add all transaction types	
				case TransactionType.DebitNote:
					aqJob = AqBanking.AB.JobSingleDebitNote_new(account.GetAccHandle());
					AqBanking.AB.Job_CheckAvailability(aqJob);
					limit = AB.JobSingleDebitNote_GetFieldLimits(aqJob);
					break;
				case TransactionType.Transfer:
					aqJob = AqBanking.AB.JobSingleTransfer_new(account.GetAccHandle());
					AqBanking.AB.Job_CheckAvailability(aqJob);
					limit = AB.JobSingleTransfer_GetFieldLimits(aqJob);				
					break;
			}
			if(!limit.Equals(IntPtr.Zero)){
				this.MaxLinesPurpose = AqBanking.AB.TransactionLimits_GetMaxLinesPurpose(limit);
				this.MinLinesPurpose = AqBanking.AB.TransactionLimits_GetMinLinesPurpose(limit);
				this.MaxLinesRemoteName = AqBanking.AB.TransactionLimits_GetMaxLinesRemoteName(limit);
				this.MinLinesRemoteName = AqBanking.AB.TransactionLimits_GetMinLinesRemoteName(limit);
				
				this.MinLenRemoteName = AqBanking.AB.TransactionLimits_GetMinLenRemoteName(limit);
				this.MaxLenRemoteName = AqBanking.AB.TransactionLimits_GetMaxLenRemoteName(limit);
				this.MinLenPurpose = AqBanking.AB.TransactionLimits_GetMinLenPurpose(limit);
				this.MaxLenPurpose = AqBanking.AB.TransactionLimits_GetMaxLenPurpose(limit);
				// TODO add other options
				this.TextKeys = GwenHelper.fromGwenStringListToArrayList(
					AqBanking.AB.TransactionLimits_GetValuesTextKey(limit));
	
			}
		}
	} */
}
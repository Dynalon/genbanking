using System;
using System.Collections.Generic;

using AqBanking;
using Banking.Contract;

namespace Banking.Provider.AqBanking
{
	public class AqJob : AqBase
	{
		public AqBankAccount Account { get; set; }
		//public JobState State { get; set; }
		public string ResultText { get; set; }

		internal SWIGTYPE_p_AB_JOB AqJobHandle { get; set; }

		internal SWIGTYPE_p_AB_BANKING abHandle { get; set; }

		public AqJob (AqBankAccount account, SWIGTYPE_p_AB_BANKING abHandle)
		{
			this.Account = account;
			this.abHandle = abHandle;
		}

		protected override void CleanUpNativeResource ()
		{
			base.CleanUpNativeResource ();
			AB.AB_Job_free (AqJobHandle);
		}
		/*public static JobState JobStateMapper(AqBanking.JobState aqstate){
			switch(aqstate){
				case AqBanking.JobState.Job_StatusError:
					return JobState.Error;
				case AqBanking.JobState.Job_StatusFinished:
					return JobState.Finished;
				case AqBanking.JobState.Job_StatusNew:
					return JobState.New;
				case AqBanking.JobState.Job_StatusUnknown:
					return JobState.Unknown;
				default:
					Helper.DebugMsg("Got unimplemented Job from backend: " + aqstate.ToString());
					return JobState.Unknown;
			}
		} */
	}

	public class AqGetBalanceJob : AqJob
	{
		public float RequestedBalance;

		public AqGetBalanceJob (AqBankAccount account, SWIGTYPE_p_AB_BANKING abHandle) : base(account, abHandle)
		{
			this.AqJobHandle = AB.AB_JobGetBalance_new (Account.AccHandle);
		}

		public void Perform ()
		{
			SWIGTYPE_p_AB_JOB_LIST2 list = AB.AB_Job_List2_new ();
			AB.AB_Job_List2_PushBack (list, this.AqJobHandle);
				
			var ctx = AB.AB_ImExporterContext_new ();
			int rv = AB.AB_Banking_ExecuteJobs (abHandle, list, ctx);
			if (rv < 0)
				throw new Exception ("Aqbanking ExecuteJobs() failed with returncode: " + rv);
			var accinfo = AB.AB_ImExporterContext_GetAccountInfo (ctx, Account.BankCode, Account.AccountIdentifier);
			var accstatus = AB.AB_ImExporterAccountInfo_GetFirstAccountStatus (accinfo);
			var bal = AB.AB_AccountStatus_GetBookedBalance (accstatus);
			var val = AB.AB_Balance_GetValue (bal);
			this.RequestedBalance = (float)AB.AB_Value_GetValueAsDouble (val);
		}
	}
	
	public class AqGetTransactionsJob : AqJob
	{
		public DateTime FromTime = DateTime.Now - new TimeSpan (30, 0, 0, 0);
		public DateTime ToTime = DateTime.Now;
		public List<ITransaction> Transactions = new List<ITransaction> ();
		
		public AqGetTransactionsJob (AqBankAccount account, SWIGTYPE_p_AB_BANKING abHandle) : base(account, abHandle)
		{
			this.AqJobHandle = AB.AB_JobGetTransactions_new (Account.AccHandle);
		}

		public void Perform ()
		{
			// narrow the timespam from which we retrieve the transactions
			AB.AB_JobGetTransactions_SetFromTime (AqJobHandle, AqHelper.convertToGwenTime (FromTime));
			AB.AB_JobGetTransactions_SetToTime (AqJobHandle, AqHelper.convertToGwenTime (ToTime));    
			
			// create a new aqbanking job for transaction retrieving
			SWIGTYPE_p_AB_JOB_LIST2 list = AB.AB_Job_List2_new ();
			AB.AB_Job_List2_PushBack (list, this.AqJobHandle);
			
			
			var ctx = AB.AB_ImExporterContext_new ();
			int rv = AB.AB_Banking_ExecuteJobs (abHandle, list, ctx);
			if (rv < 0)
				throw new Exception ("Aqbanking ExecuteJobs() failed with return code: " + rv);
			
			var accinfo = AB.AB_ImExporterContext_GetAccountInfo (ctx, Account.BankCode, Account.AccountIdentifier);
			
			// fill our transactions list from aqbanking
			var trans = AB.AB_ImExporterAccountInfo_GetFirstTransaction (accinfo);
			while (trans != null) {
				AqTransaction transaction = new AqTransaction (trans);
				this.Transactions.Add ((ITransaction)transaction);
				trans = AB.AB_ImExporterAccountInfo_GetNextTransaction (accinfo);
			}
		}
	}
}
using System;
using System.Runtime.InteropServices;
using AqBanking;
using Banking.Provider.AqBanking;
using System.Collections;
using System.Diagnostics;

namespace Banking.Provider.AqBanking.Gui
{
	static class AqGui
	{
		[DllImport("gwenhywfar",EntryPoint="GWEN_Gui_SetMessageBoxFn")]
		public extern static void GWEN_Gui_SetMessageBoxFn (IntPtr gui, IntPtr fnPtr)

;
		[DllImport("gwenhywfar",EntryPoint="GWEN_Gui_SetInputBoxFn")]
		public extern static void GWEN_Gui_SetInputBoxFn (IntPtr gui, IntPtr fnPtr)

;
		[DllImport("gwenhywfar",EntryPoint="GWEN_Gui_SetShowBoxFn")]
		public extern static void GWEN_Gui_SetShowBoxFn (IntPtr gui, IntPtr fnPtr)

;

		[DllImport("gwenhywfar",EntryPoint="GWEN_Gui_SetSetPasswordStatusFn")]
		public extern static void GWEN_Gui_SetSetPasswordStatusFn (IntPtr gui, IntPtr fnPtr)

;
		[DllImport("gwenhywfar",EntryPoint="GWEN_Gui_SetCheckCertFn")]
		public extern static void GWEN_Gui_SetCheckCertFn (IntPtr gui,IntPtr fnPtr)

;
		[DllImport("gwenhywfar",EntryPoint="GWEN_Gui_SetGetPasswordFn")]
		public extern static void GWEN_Gui_SetGetPasswordFn (IntPtr gui,IntPtr fnPtr);
		
		[DllImport("gwenhywfar",EntryPoint="GWEN_Gui_new")]
		public extern static IntPtr GWEN_Gui_new ()

;
		[DllImport("gwenhywfar",EntryPoint="GWEN_Gui_SetGui")]
		public extern static IntPtr GWEN_Gui_SetGui (IntPtr gui);		
	}

	public enum PasswordStatus
	{
		Bad=-1,
		Unknown,
		Ok,
		Used,
		Unused,
		Remove
	}
		
	// Delegate Definition for Gui callbacks
	public delegate int InputBoxDelegate (IntPtr gui,uint flags,string title,string text, IntPtr buffer,int minlen,int maxlen,uint guiid);

	public delegate uint ShowBoxDelegate (IntPtr gui, uint flags, string title, string text, uint guiid);

	public delegate void HideBoxDelegate (IntPtr gui, uint id);

	public delegate int MessageBoxDelegate (IntPtr gui, uint flags, string title, string text, string b1, string b2, string b3, uint guiid);

	public delegate int GetPasswordDelegate (IntPtr gui, uint flags, string token, string title, string text, IntPtr buffer, int minLen, int maxLen,uint guiid);

	public delegate int SetPasswordStatusDelegate (IntPtr gui, string token, string pin, PasswordStatus status, uint guiid);

	public delegate int CheckCertDelegate (IntPtr gui, IntPtr cert, IntPtr iolayer, uint guiid);
	
	public interface IAqGui
	{
		int InputBox (IntPtr gui, uint flags, string title, string text, IntPtr buffer, int minlen, int maxlen, uint guiid)

;
		uint ShowBox (IntPtr gui, uint flags, string title, string text, uint guiid)

;
		int MessageBox (IntPtr gui, uint flags, string title, string text, string b1, string b2, string b3, uint guiid)

;
		int GetPassword (IntPtr gui, uint flags, string token, string title, string text, IntPtr buffer, int minLen, int maxLen,uint guiid)

;
		int SetPasswordStatus (IntPtr gui, string token, string pin, PasswordStatus status, uint  guiid)

;
		int CheckCert (IntPtr gui, IntPtr cert, IntPtr iolayer,uint guiid);
		// TODO add other GWEN_GUI_fn
	}

	public static class AqGuiHandler
	{
		// function pointer to delegates (for passing to native backend)
		public static IntPtr MessageBoxPtr;
		public static IntPtr InputBoxPtr;
		public static IntPtr ShowBoxPtr;
		public static IntPtr GetPasswordPtr;
		public static IntPtr SetPasswordStatusPtr;
		public static IntPtr CheckCertPtr;
		// the delegates for GetFuncPtr
		private static MessageBoxDelegate msgdel;
		private static InputBoxDelegate inputdel;
		private static ShowBoxDelegate showdel;
		private static SetPasswordStatusDelegate passwdstatusdel;
		private static CheckCertDelegate checkcertdel;
		private static GetPasswordDelegate getpassdel;
		// Gwen Gui Handle
		private static IntPtr gui;
		
		public static void SetGui (SWIGTYPE_p_AB_BANKING abHandle, IAqGui guiobj)
		{
			// retrieve function pointers for callbacks
			msgdel = new MessageBoxDelegate (guiobj.MessageBox);
			MessageBoxPtr = Marshal.GetFunctionPointerForDelegate (msgdel);
			
			inputdel = new InputBoxDelegate (guiobj.InputBox);
			InputBoxPtr = Marshal.GetFunctionPointerForDelegate (inputdel);
			
			showdel = new ShowBoxDelegate (guiobj.ShowBox);
			ShowBoxPtr = Marshal.GetFunctionPointerForDelegate (showdel);
			
			passwdstatusdel = new SetPasswordStatusDelegate (guiobj.SetPasswordStatus);
			SetPasswordStatusPtr = Marshal.GetFunctionPointerForDelegate (passwdstatusdel);
			
			checkcertdel = new CheckCertDelegate (guiobj.CheckCert);
			CheckCertPtr = Marshal.GetFunctionPointerForDelegate (checkcertdel);
			
			getpassdel = new GetPasswordDelegate (guiobj.GetPassword);
			GetPasswordPtr = Marshal.GetFunctionPointerForDelegate (getpassdel);
			
			// register function pointers to callback
			gui = AqGui.GWEN_Gui_new ();
			
			AqGui.GWEN_Gui_SetMessageBoxFn (gui, MessageBoxPtr);
			AqGui.GWEN_Gui_SetInputBoxFn (gui, InputBoxPtr);
			AqGui.GWEN_Gui_SetShowBoxFn (gui, ShowBoxPtr);
			AqGui.GWEN_Gui_SetSetPasswordStatusFn (gui, SetPasswordStatusPtr);
			AqGui.GWEN_Gui_SetCheckCertFn (gui, CheckCertPtr);
			AqGui.GWEN_Gui_SetGetPasswordFn (gui, GetPasswordPtr);
			AqGui.GWEN_Gui_SetGui (gui);
		}
	}

	/// <summary>
	/// simple console based IAqGui implementation which reads/write from stdout/in/err
	/// </summary> 
	public class ConsoleGui : AqBase, IAqGui
	{
		protected Hashtable PasswordBuffer = new Hashtable ();

		public int GetPassword (IntPtr gui, uint flags, string token, string title, string text, IntPtr buffer, int minLen, int maxLen,uint guiid)
		{
			log.DebugFormat ("title: {0}, token: {1}, text; {2}", title, token, text);

			string input;
			if (PasswordBuffer [token] != null) {
				log.Debug ("using buffered password");
				input = (string)PasswordBuffer [token];
			} else {
				Console.WriteLine (title);
				Console.WriteLine (text);
				input = Console.ReadLine ();
				PasswordBuffer [token] = input;
			}
			Marshal.Copy (AqHelper.StringToByteArray (input), 0, buffer, input.Length);
			return 0;
		}

		public int InputBox (IntPtr gui, uint flags, string title, string body, IntPtr buffer, int minlen, int maxlen, UInt32 guiid)
		{
			log.DebugFormat ("flags: {0}, title: {1}, body: {2}, minlen: {3}, maxlen: {4}, guuid: {5}",
				flags, title, body, minlen, maxlen, guiid);
			
			Console.WriteLine (title);
			string input = Console.ReadLine ();
			Marshal.Copy (AqHelper.StringToByteArray (input), 0, buffer, input.Length);
			return 0;
		}

		public uint ShowBox (IntPtr gui, uint flags, string title, string text, UInt32 guiid)
		{		
			Console.WriteLine (title);
			log.Debug ("");
			// implement user confirmation here
			return 0;
		}

		public int MessageBox (IntPtr gui, uint flags, string title, string text, string b1, string b2, string b3,UInt32 guiid)
		{
			log.DebugFormat ("title: {0}, text: {1}, b1: {2}, b2: {3}, b3: {4}, flags: {5}",
				title, text, b1, b2, b3, flags);

			// provide interface to select from a set of options b1-b3
			int i = 1;
			if (b1.Length > 0)
				Console.Write ("(" + i++ + ") " + b1 + " ");
			if (b2.Length > 0)
				Console.Write ("(" + i++ + ") " + b2 + " ");
			if (b3.Length > 0)
				Console.Write ("(" + i++ + ") " + b3 + " ");
			string input = Console.ReadLine ();
			return Int32.Parse (input);
		}

		public int SetPasswordStatus (IntPtr gui, string token, string pin, PasswordStatus status, uint guiid)
		{
			log.DebugFormat ("token: {0}, pin: {1}, status: {2}", token, pin, status);
			// FIXME find out what this function is used for
			return 0;
		}

		public int CheckCert (IntPtr gui, IntPtr cert, IntPtr iolayer, UInt32 guiid)
		{
			Console.WriteLine ("Warning: Accepting cert without checking for validity!");
			log.Debug ("Accepting certs without checking for validity");
			// TODO we acccept all certs
			return 0;
		}
	}
	
	/// <summary>
	/// AutoGui does not really need any user interaction but uses pre-stored Pin
	/// </summary>
	public class AutoGui : AqBase, IAqGui
	{
		public string Pin;
		
		// AqGui stuff
		public int GetPassword (IntPtr gui, uint flags, string token, string title, string text, IntPtr buffer, int minLen, int maxLen,uint guiid)
		{
			log.DebugFormat ("title: {0}, token: {1}, text; {2}", title, token, text);
			
			if (string.IsNullOrEmpty (Pin))
				throw new Exception ("no pin set for noninteractive mode");
					
			if (title == "PIN-Eingabe" || title == "Enter PIN") {
				log.Debug ("Copying pre-saved Pin");
				Marshal.Copy (AqHelper.StringToByteArray (this.Pin), 0, buffer, this.Pin.Length);
			} else
				throw new System.Exception ("unknown GetPassword dialog");
			return 0;
		}

		public int InputBox (IntPtr gui, uint flags, string title, string body, IntPtr buffer, int minlen, int maxlen, UInt32 guiid)
		{
			log.Debug ("");
			return 0;
		}

		public uint ShowBox (IntPtr gui, uint flags, string title, string text, UInt32 guiid)
		{
			log.Debug ("");
			return 0;
		}

		public int MessageBox (IntPtr gui, uint flags, string title, string text, string b1, string b2, string b3,UInt32 guiid)
		{
			log.DebugFormat ("title: {0}, text: {1}, b1: {2}, b2: {3}, b3: {4}, flags: {5}",
				title, text, b1, b2, b3, flags);
			// always continue by default
			if (b1 == "Continue")
				return 1;
			return 2;
		}

		public int SetPasswordStatus (IntPtr gui, string token, string pin, PasswordStatus status, uint guiid)
		{
			log.DebugFormat ("token: {0}, pin: {1}, status: {2}, guiid: {3}", token, pin, status, guiid);
			// if this function is called, sth was wrong with the PIN
			if (status.ToString () != "Ok") {
				throw new Exception ("wrong pin entered - warning: subsequent wrong entries will have your banking blocked");
			}
			return 0;
		}

		public int CheckCert (IntPtr gui, IntPtr cert, IntPtr iolayer, UInt32 guiid)
		{
			// FIXME we accept all certs without checking
			return 0;
		}
	}
} /* EONS */

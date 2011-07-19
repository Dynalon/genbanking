using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Collections.Generic;

using AqBanking;

namespace Banking.Provider.AqBanking
{
	public static class AqHelper
	{
		public static byte[] StringToByteArray (string str)
		{
			System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding ();
			return encoding.GetBytes (str);
		}

		public static string ByteArrayToString (byte[] bytearray)
		{
			System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding ();
			return encoding.GetString (bytearray);
		}

		public static List<string> fromGwenStringList (SWIGTYPE_p_GWEN_STRINGLISTSTRUCT stringlist)
		{
			List<string > l = new List<string> ();
			uint numentries = AB.GWEN_StringList_Count (stringlist);
			for (int i=0; i<=(int) numentries-1; i++) {
				l.Add (AB.GWEN_StringList_StringAt (stringlist, i));
				if (i > 10) // avoid endless loops on failure
					break;
			}
			return l;
		}
		/*public static string[] fromGwenStringListToStringArray(IntPtr stringlist){
			ArrayList arr = new ArrayList();
			arr = fromGwenStringListToArrayList(stringlist);
			return (string[]) arr.ToArray(typeof(string));
		}
		public static IntPtr fromArrayListToGwenStringList(ArrayList list)
		{
			IntPtr sl = Gwen.StringList_new();
			foreach(string str in list){
				IntPtr entry = Gwen.StringListEntry_new(str , 0);
				Gwen.StringList_AppendEntry(sl, entry);
			}
			return sl;
		}
		public static IntPtr fromStringArrayToGwenStringList(string[] list){
			ArrayList arr = new ArrayList();
			foreach(string s in list)
				arr.Add(s);
			return fromArrayListToGwenStringList(arr);
		}
		public static string fromGwenStringListToLineBreakString(IntPtr stringlist)
		{
			// returns a string with each entry in a separate line
			ArrayList l = fromGwenStringListToArrayList(stringlist);
			string newstring = "";
			foreach(string entry in l)
				newstring = newstring + entry + "\n";
			return newstring;
		}
		*/
		public static DateTime fromGwenTimeToDateTime (SWIGTYPE_p_GWEN_TIME gwentime)
		{
			if (gwentime.Equals (IntPtr.Zero)) {			
				return new DateTime (1970, 1, 1, 0, 0, 0, 0);
			}
			DateTime unixepoch = new DateTime (1970, 1, 1, 0, 0, 0, 0);
			DateTime retval = unixepoch.AddSeconds (AB.GWEN_Time_Seconds (gwentime));
			return retval;
		}
		
		public static SWIGTYPE_p_GWEN_TIME convertToGwenTime (DateTime date)
		{
			return AB.GWEN_Time_fromString (date.ToString ("yyyyMMdd HH:mm"), "YYYYMMDD hh:mm");
		}
		/*
		public static void WriteContextToFile(IntPtr ctx, string filename)
		{  
			IntPtr dbCtx;
			dbCtx=Gwen.DB_Group_new("context");
			AB.ImExporterContext_toDb(ctx, dbCtx);
			Gwen.DB_WriteFile(dbCtx, filename, 284688384, 1, 500);
			Gwen.DB_Group_free(dbCtx);
		}*/
	}
}
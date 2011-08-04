using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Web;
using System.Web.SessionState;
using System.Web.Security;
using System.Security.Principal;
using System.Security.Cryptography;
using System.Text;
using System.Linq;
using System.Configuration;
using log4net;

namespace Banking.Web
{
	public class Global : System.Web.HttpApplication
	{
		
		protected virtual void Application_Start (Object sender, EventArgs e)
		{	
			// perform automapper configuration
			Banking.ConfigureAutomapper ();
			
			// setup loggin
			log4net.Appender.ConsoleAppender appender;
			appender = new log4net.Appender.ConsoleAppender ();
			appender.Layout = new log4net.Layout.PatternLayout ("%-4timestamp %-5level %logger %M %ndc - %message%newline");
			log4net.Config.BasicConfigurator.Configure (appender);  
			if (ConfigurationManager.AppSettings ["Debug"] != null
				&& ConfigurationManager.AppSettings ["Debug"] == "true")
				appender.Threshold = log4net.Core.Level.Debug;
			else
				appender.Threshold = log4net.Core.Level.Warn;
		}

		protected virtual void Session_Start (Object sender, EventArgs e)
		{
		}
		
		protected virtual void Application_BeginRequest (Object sender, EventArgs e)
		{
			// enforce secure passwords
			var passkey = ConfigurationManager.AppSettings ["Passkey"];
			if (string.IsNullOrEmpty (passkey) || passkey.Length < 10)
				throw new Exception ("Passkey MUST be 10 characters long at least, please check your Web.config");
		}
		
		protected virtual void Application_EndRequest (Object sender, EventArgs e)
		{
		}
		
		protected virtual void Application_AuthenticateRequest (Object sender, EventArgs e)
		{
		}
		
		protected virtual void Application_Error (Object sender, EventArgs e)
		{
		}
		
		protected virtual void Session_End (Object sender, EventArgs e)
		{
		}
		
		protected virtual void Application_End (Object sender, EventArgs e)
		{
		}
	}
}


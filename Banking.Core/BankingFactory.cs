using System.Collections.Generic;
using System.Linq;
using System;
using System.Reflection;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition;
using System.Configuration;

using Banking.Contract;
using System.IO;
using System.ComponentModel.Composition.Primitives;
using log4net;

namespace Banking
{
	public class BankingFactory : IDisposable
	{
		[Import]
		//public List<Lazy<IBankingProvider, IBankingProviderMetadata>> Provider;
		public IBankingProvider Provider;
		protected ILog log;
		
		public BankingFactory ()
		{
			this.log = log4net.LogManager.GetLogger (this.GetType ());
		}
		
		/// <summary>
		/// Gets the provider with no given config, hence using defaults.
		/// </summary>
		/// <returns>
		/// The provider.
		/// </returns>
		public IBankingProvider GetProvider ()
		{
			// if no config file is given, we set it to the assembly folder,
			// remove file:/ prefix from path and append provider.config
			var assemblyDir = Path.GetDirectoryName (Assembly.GetExecutingAssembly ().CodeBase.Substring (5));
			var configFile = assemblyDir + Path.DirectorySeparatorChar + "provider.config";
			
			if (File.Exists (configFile)) 
				return GetProvider (new ProviderConfig (configFile));
			else
				return GetProvider (new ProviderConfig ());
		}
		
		/// <summary>
		/// Gets the provider and configures it from a given .config file
		/// which must exist.
		/// </summary>
		/// <returns>
		/// The provider.
		/// </returns>
		/// <param name='configPath'>
		/// Config path.
		/// </param>
		public IBankingProvider GetProvider (string configFile)
		{
			if (string.IsNullOrEmpty (configFile))
				return GetProvider ();
			else
				return GetProvider (new ProviderConfig (configFile));
		}

		public IBankingProvider GetProvider (ProviderConfig config)
		{
			var catalogs = new List<ComposablePartCatalog> ();
			// we always search the directory with the running assembly first
			// Note: GetExecutingAssembly() returns a string with 'file:/' prefix
			// which is not removed by GetDirectoryName(). The Path will thus not be valid
			// if file: prefix is not removed first
			var assemblyDir = Path.GetDirectoryName (Assembly.GetExecutingAssembly ().CodeBase.Substring (5));
			catalogs.Add (new DirectoryCatalog (assemblyDir));
		
			// assemble the backends
			var catalog = new AggregateCatalog (catalogs);
			var container = new CompositionContainer (catalog);
			var providerlist = container.GetExports<IBankingProvider, IBankingProviderMetadata> ();
			
			// default provider is aqbanking
			var providerName = "aqbanking";
			
			if (config != null && config.Settings ["provider"] != null)
				providerName = config.Settings ["provider"].Value;
			
			IBankingProvider provider;
			try {
				provider = (from p in providerlist where p.Metadata.Name == providerName select p.Value).First ();
			} catch (Exception e) {
				//log.Fatal (e.Message);
				throw new Exception ("no backend by name '" + providerName + "' could be loaded. Check the backend .dll exists");
			}

			provider.Init (config);
			return provider;
		}
		
		public void Dispose ()
		{
			Provider.Dispose ();
		}
	}
}
/* EONS HBCI */


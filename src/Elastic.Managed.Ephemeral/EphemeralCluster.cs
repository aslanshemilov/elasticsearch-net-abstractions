using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Elastic.Managed.Configuration;
using Elastic.Stack.Artifacts;

namespace Elastic.Managed.Ephemeral
{
	public class EphemeralCluster : EphemeralCluster<EphemeralClusterConfiguration>
	{
		public EphemeralCluster(ElasticVersion version, int numberOfNodes = 1)
			: base(new EphemeralClusterConfiguration(version, ClusterFeatures.None, numberOfNodes: numberOfNodes)) { }

		public EphemeralCluster(EphemeralClusterConfiguration clusterConfiguration) : base(clusterConfiguration) { }
	}

	public abstract class EphemeralCluster<TConfiguration> : ClusterBase<TConfiguration>, IEphemeralCluster<TConfiguration>
		where TConfiguration : EphemeralClusterConfiguration
	{
		protected EphemeralCluster(TConfiguration clusterConfiguration) : base(clusterConfiguration)
		{
			this.Composer = new EphemeralClusterComposer<TConfiguration>(this);
		}

		protected EphemeralClusterComposer<TConfiguration> Composer { get; }

		protected override void OnBeforeStart()
		{
			this.Composer.Install();
			this.Composer.OnBeforeStart();
		}

		protected override void OnDispose() => this.Composer.OnStop();

		protected override void OnAfterStarted() => this.Composer.OnAfterStart();

		public virtual ICollection<Uri> NodesUris(string hostName = null)
		{
			hostName = hostName ?? (this.ClusterConfiguration.HttpFiddlerAware && Process.GetProcessesByName("fiddler").Any()
				? "ipv4.fiddler"
				: "localhost");
			var ssl = this.ClusterConfiguration.EnableSsl ? "s" : "";
			return this.Nodes
				.Select(n=>$"http{ssl}://{hostName}:{n.Port ?? 9200}")
				.Distinct()
				.Select(n => new Uri(n))
				.ToList();
		}

		protected override string SeeLogsMessage(string message)
		{
			var log = Path.Combine(this.FileSystem.LogsPath, $"{this.ClusterConfiguration.ClusterName}.log");
			if (!File.Exists(log) || this.ClusterConfiguration.ShowElasticsearchOutputAfterStarted) return message;
			if (!this.Started) return message;
			using (var fileStream = new FileStream(log, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
			using (var textReader = new StreamReader(fileStream))
			{
				var logContents = textReader.ReadToEnd();
				return message + $" contents of {log}:{Environment.NewLine}" + logContents;
			}
		}

		public bool CachingAndCachedHomeExists()
		{
			if (!this.ClusterConfiguration.CacheEsHomeInstallation) return false;
			var cachedEsHomeFolder = Path.Combine(this.FileSystem.LocalFolder, this.GetCacheFolderName());
			return Directory.Exists(cachedEsHomeFolder);
		}

		public virtual string GetCacheFolderName()
		{
			var config = this.ClusterConfiguration;

			var sb = new StringBuilder();
			sb.Append(EphemeralClusterComposerBase.InstallationTasks.Count());
			sb.Append("-");
			if (config.XPackInstalled) sb.Append("x");
			if (config.EnableSecurity) sb.Append("sec");
			if (config.EnableSsl) sb.Append("ssl");
			if (config.Plugins != null && config.Plugins.Count > 0)
			{
				sb.Append("-");
				foreach (var p in config.Plugins.OrderBy(p=>p.SubProductName))
					sb.Append(p.SubProductName.ToLowerInvariant());
			}
			var name = sb.ToString();

			return CalculateSha1(name, Encoding.UTF8);
		}
		public static string CalculateSha1(string text, Encoding enc)
		{
			var buffer = enc.GetBytes(text);
			var cryptoTransformSha1 = new SHA1CryptoServiceProvider();
			return BitConverter.ToString(cryptoTransformSha1.ComputeHash(buffer))
				.Replace("-", "").ToLowerInvariant().Substring(0,12);
		}

	}
}

/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@qq.com>
 *
 * Copyright (C) 2020-2026 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Externals.Garnet library.
 *
 * The Zongsoft.Externals.Garnet is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Garnet is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Garnet library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.IO;
using System.Globalization;
using System.ComponentModel;
using System.Collections.Generic;

using global::Garnet.server.Auth.Settings;

using Zongsoft.Services;
using Zongsoft.Components;
using Zongsoft.Configuration;

namespace Zongsoft.Externals.Garnet.Configuration;

/// <summary>表示 Garnet 内嵌服务器的连接与运行设置。</summary>
public sealed class GarnetConnectionSettings : ConnectionSettingsBase<GarnetConnectionSettingsDriver>
{
	#region 静态字段
	private static readonly HashSet<string> _paths = new(StringComparer.OrdinalIgnoreCase)
	{
		"logdir",
		"acl-file",
		"file-logger",
		"checkpointdir",
		"cert-file-name",
		"issuer-certificate-path",
		"config-export-path",
		"loadmodulecs",
		"extension-bin-paths",
	};

	private static readonly Dictionary<string, string> _aliases = new(StringComparer.OrdinalIgnoreCase)
	{
		{ "Quiet", "q" },
		{ "QuietMode", "q" },
		{ "ClusterAnnouncePort", "cluster-announce-port" },
		{ "ClusterAnnounceIp", "cluster-announce-ip" },
		{ "ClusterAnnounceHostname", "cluster-announce-hostname" },
		{ "ClusterPreferredEndpointType", "cluster-preferred-endpoint-type" },
		{ "LogMemorySize", "memory" },
		{ "PageSize", "page" },
		{ "PageCount", "pagecount" },
		{ "SegmentSize", "segment" },
		{ "ObjectLogSegmentSize", "object-log-segment" },
		{ "IndexMemorySize", "index" },
		{ "IndexMaxMemorySize", "index-max-size" },
		{ "MutablePercent", "mutable-percent" },
		{ "EnableReadCache", "readcache" },
		{ "ReadCacheMemorySize", "readcache-memory" },
		{ "ReadCachePageSize", "readcache-page" },
		{ "ReadCachePageCount", "readcache-pagecount" },
		{ "EnableStorageTier", "storage-tier" },
		{ "CopyReadsToTail", "copy-reads-to-tail" },
		{ "LogDir", "logdir" },
		{ "CheckpointDirectory", "checkpointdir" },
		{ "Recover", "recover" },
		{ "DisablePubSub", "no-pubsub" },
		{ "PubSubPageSize", "pubsub-pagesize" },
		{ "DisableObjects", "no-obj" },
		{ "EnableCluster", "cluster" },
		{ "CleanClusterConfig", "clean-cluster-config" },
		{ "ParallelMigrateTaskCount", "pmt" },
		{ "FastMigrate", "fast-migrate" },
		{ "AuthenticationMode", "auth" },
		{ "Password", "password" },
		{ "ClusterUsername", "cluster-username" },
		{ "ClusterPassword", "cluster-password" },
		{ "AclFile", "acl-file" },
		{ "AadAuthority", "aad-authority" },
		{ "AadAudiences", "aad-audiences" },
		{ "AadIssuers", "aad-issuers" },
		{ "AuthorizedAadApplicationIds", "aad-authorized-app-ids" },
		{ "AadValidateUsername", "aad-validate-acl-username" },
		{ "EnableAOF", "aof" },
		{ "AofMemorySize", "aof-memory" },
		{ "AofPageSize", "aof-page-size" },
		{ "AofPhysicalSublogCount", "aof-physical-sublog-count" },
		{ "AofReplayTaskCount", "aof-replay-task-count" },
		{ "AofTailWitnessFreqMs", "aof-tail-witness-freq" },
		{ "CommitFrequency", "aof-commit-freq" },
		{ "WaitForCommit", "aof-commit-wait" },
		{ "AofSizeLimit", "aof-size-limit" },
		{ "AofSizeLimitEnforceFrequencySecs", "aof-size-limit-enforce-frequency" },
		{ "CompactionFrequencySecs", "compaction-freq" },
		{ "ExpiredObjectCollectionFrequencySecs", "expired-object-collection-freq" },
		{ "CompactionType", "compaction-type" },
		{ "CompactionForceDelete", "compaction-force-delete" },
		{ "CompactionMaxSegments", "compaction-max-segments" },
		{ "EnableLua", "lua" },
		{ "LuaTransactionMode", "lua-transaction-mode" },
		{ "GossipSamplePercent", "gossip-sp" },
		{ "GossipDelay", "gossip-delay" },
		{ "ClusterTimeout", "cluster-timeout" },
		{ "ClusterConfigFlushFrequencyMs", "cluster-config-flush-frequency" },
		{ "ClusterTlsClientTargetHost", "cluster-tls-client-target-host" },
		{ "ServerCertificateRequired", "server-certificate-required" },
		{ "EnableTLS", "tls" },
		{ "CertificateFileName", "cert-file-name" },
		{ "CertificatePassword", "cert-password" },
		{ "CertSubjectName", "cert-subject-name" },
		{ "CertificateRefreshFrequency", "cert-refresh-freq" },
		{ "ClientCertificateRequired", "client-certificate-required" },
		{ "CertificateRevocationCheckMode", "certificate-revocation-check-mode" },
		{ "IssuerCertificatePath", "issuer-certificate-path" },
		{ "LatencyMonitor", "latency-monitor" },
		{ "CommandStatsMonitor", "commandstats-monitor" },
		{ "SlowLogThreshold", "slowlog-log-slower-than" },
		{ "SlowLogMaxEntries", "slowlog-max-len" },
		{ "MetricsSamplingFrequency", "metrics-sampling-freq" },
		{ "LogLevel", "logger-level" },
		{ "LoggingFrequency", "logger-freq" },
		{ "DisableConsoleLogger", "disable-console-logger" },
		{ "FileLogger", "file-logger" },
		{ "ThreadPoolMinThreads", "minthreads" },
		{ "ThreadPoolMaxThreads", "maxthreads" },
		{ "ThreadPoolMinIOCompletionThreads", "miniothreads" },
		{ "ThreadPoolMaxIOCompletionThreads", "maxiothreads" },
		{ "NetworkConnectionLimit", "network-connection-limit" },
		{ "UseAzureStorage", "use-azure-storage" },
		{ "AzureStorageServiceUri", "storage-service-uri" },
		{ "AzureStorageManagedIdentity", "storage-managed-identity" },
		{ "AzureStorageConnectionString", "storage-string" },
		{ "CheckpointThrottleFlushDelayMs", "checkpoint-throttle-delay" },
		{ "EnableFastCommit", "fast-commit" },
		{ "FastCommitThrottleFreq", "fast-commit-throttle" },
		{ "NetworkSendThrottleMax", "network-send-throttle" },
		{ "EnableScatterGatherGet", "sg-get" },
		{ "ReplicaSyncDelayMs", "replica-sync-delay" },
		{ "ReplicationOffsetMaxLag", "replica-offset-max-lag" },
		{ "MainMemoryReplication", "main-memory-replication" },
		{ "FastAofTruncate", "fast-aof-truncate" },
		{ "OnDemandCheckpoint", "on-demand-checkpoint" },
		{ "ReplicaDisklessSync", "repl-diskless-sync" },
		{ "ReplicaDisklessSyncDelay", "repl-diskless-sync-delay" },
		{ "ReplicaAttachTimeout", "repl-attach-timeout" },
		{ "ReplicaSyncTimeout", "repl-sync-timeout" },
		{ "ReplicaDisklessSyncFullSyncAofThreshold", "repl-diskless-sync-full-sync-aof-threshold" },
		{ "UseAofNullDevice", "aof-null-device" },
		{ "ConfigImportPath", "config-import-path" },
		{ "ConfigImportFormat", "config-import-format" },
		{ "ConfigExportFormat", "config-export-format" },
		{ "UseAzureStorageForConfigImport", "use-azure-storage-for-config-import" },
		{ "ConfigExportPath", "config-export-path" },
		{ "UseAzureStorageForConfigExport", "use-azure-storage-for-config-export" },
		{ "UseNativeDeviceLinux", "use-native-device-linux" },
		{ "DeviceType", "device-type" },
		{ "RevivBinRecordSizes", "reviv-bin-record-sizes" },
		{ "RevivBinRecordCounts", "reviv-bin-record-counts" },
		{ "RevivifiableFraction", "reviv-fraction" },
		{ "EnableRevivification", "reviv" },
		{ "RevivNumberOfBinsToSearch", "reviv-search-next-higher-bins" },
		{ "RevivBinBestFitScanLimit", "reviv-bin-best-fit-scan-limit" },
		{ "RevivInChainOnly", "reviv-in-chain-only" },
		{ "ObjectScanCountLimit", "object-scan-count-limit" },
		{ "EnableDebugCommand", "enable-debug-command" },
		{ "EnableModuleCommand", "enable-module-command" },
		{ "ProtectedMode", "protected-mode" },
		{ "ExtensionBinPaths", "extension-bin-paths" },
		{ "LoadModuleCS", "loadmodulecs" },
		{ "ExtensionAllowUnsignedAssemblies", "extension-allow-unsigned" },
		{ "IndexResizeFrequencySecs", "index-resize-freq" },
		{ "IndexResizeThreshold", "index-resize-threshold" },
		{ "ValueOverflowThreshold", "value-overflow-threshold" },
		{ "FailOnRecoveryError", "fail-on-recovery-error" },
		{ "LuaMemoryManagementMode", "lua-memory-management-mode" },
		{ "LuaScriptMemoryLimit", "lua-script-memory-limit" },
		{ "LuaScriptTimeoutMs", "lua-script-timeout" },
		{ "LuaLoggingMode", "lua-logging-mode" },
		{ "LuaAllowedFunctions", "lua-allowed-functions" },
		{ "UnixSocketPath", "unixsocket" },
		{ "UnixSocketPermission", "unixsocketperm" },
		{ "MaxDatabases", "max-databases" },
		{ "ExpiredKeyDeletionScanFrequencySecs", "expired-key-deletion-scan-freq" },
		{ "ClusterReplicationReestablishmentTimeout", "cluster-replication-reestablishment-timeout" },
		{ "ClusterReplicaResumeWithData", "cluster-replica-resume-with-data" },
		{ "EnableVectorSetPreview", "enable-vector-set-preview" },
		{ "VectorSetReplayTaskCount", "vector-set-replay-task-count" },
		{ "EnableRangeIndexPreview", "enable-range-index-preview" },
	};
	#endregion

	#region 构造函数
	public GarnetConnectionSettings(GarnetConnectionSettingsDriver driver, string settings) : base(driver, settings) { }
	public GarnetConnectionSettings(GarnetConnectionSettingsDriver driver, string name, string settings) : base(driver, name, settings) { }
	#endregion

	#region 公共属性
	/// <summary>获取或设置服务器的 IP 地址和固定端口，例如 127.0.0.1:6379 或 [::1]:6379。</summary>
	[ConnectionSetting(true)]
	public string Address
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	/// <summary>获取或设置服务器认证方式。</summary>
	[Alias("auth")]
	public GarnetAuthenticationMode? AuthenticationMode
	{
		get => this.GetValue<GarnetAuthenticationMode?>();
		set => this.SetValue(value);
	}

	/// <summary>获取或设置认证密码。</summary>
	public string Password
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	/// <summary>获取或设置是否启用 TLS。</summary>
	[Alias("tls")]
	public bool? EnableTLS
	{
		get => this.GetValue<bool?>();
		set => this.SetValue(value);
	}

	/// <summary>获取或设置服务器证书文件路径。</summary>
	[Alias("cert-file-name")]
	public string CertificateFileName
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	/// <summary>获取或设置服务器证书密码。</summary>
	[Alias("cert-password")]
	public string CertificatePassword
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	/// <summary>获取或设置是否要求客户端证书。</summary>
	[Alias("client-certificate-required")]
	public bool? ClientCertificateRequired
	{
		get => this.GetValue<bool?>();
		set => this.SetValue(value);
	}

	/// <summary>获取或设置证书颁发者文件路径。</summary>
	[Alias("issuer-certificate-path")]
	public string IssuerCertificatePath
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	/// <summary>获取或设置证书刷新间隔，零表示禁用刷新。</summary>
	[Alias("cert-refresh-freq")]
	[TypeConverter(typeof(SecondsConverter))]
	public TimeSpan? CertificateRefreshFrequency
	{
		get => this.GetValue<TimeSpan?>();
		set => this.SetValue(value);
	}

	/// <summary>获取或设置是否启用追加日志。</summary>
	[Alias("aof")]
	public bool? EnableAOF
	{
		get => this.GetValue<bool?>();
		set => this.SetValue(value);
	}

	/// <summary>获取或设置追加日志提交间隔，零表示立即提交，负一毫秒表示手动提交。</summary>
	[Alias("aof-commit-freq")]
	[TypeConverter(typeof(MillisecondsConverter))]
	public TimeSpan? CommitFrequency
	{
		get => this.GetValue<TimeSpan?>();
		set => this.SetValue(value);
	}

	/// <summary>获取或设置是否启用 Lua。</summary>
	[Alias("lua")]
	public bool? EnableLua
	{
		get => this.GetValue<bool?>();
		set => this.SetValue(value);
	}

	/// <summary>获取或设置是否恢复持久化数据。</summary>
	public bool? Recover
	{
		get => this.GetValue<bool?>();
		set => this.SetValue(value);
	}

	/// <summary>获取或设置检查点目录。</summary>
	[Alias("checkpointdir")]
	public string CheckpointDirectory
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	/// <summary>获取或设置是否禁止输出服务器标识。</summary>
	[Alias("q")]
	[Alias("quiet")]
	public bool? QuietMode
	{
		get => this.GetValue<bool?>();
		set => this.SetValue(value);
	}
	#endregion

	#region 内部方法
	internal IEnumerable<string> GetArguments()
	{
		var text = this.Address;

		//监听参数必须能由同一份配置中的绑定模板直接读取，不能依赖 SDK 默认值。
		if(string.IsNullOrEmpty(text) || text.Contains('/') || text[0] is '*' or '+' ||
		   !Communication.NetworkAddress.TryParse($"tcp://{text}", out var address) ||
		   address.HostType is not (UriHostNameType.IPv4 or UriHostNameType.IPv6))
			throw new InvalidOperationException(string.Format(global::Zongsoft.Externals.Garnet.Properties.Resources.GarnetConnectionSettings_AddressInvalid_Message, this.Name));

		yield return "--bind";
		yield return address.Zone == null ? address.Host : $"{address.Host}%{address.Zone}";
		yield return "--port";
		yield return address.Port.ToString(CultureInfo.InvariantCulture);

		foreach(var setting in this)
		{
			var key = GetOptionName(setting.Key).ToLowerInvariant();

			if(key == "address")
				continue;

			//外部配置与 Unix socket 不能参与本实例的静态 TCP 声明；进程级命令不能传给内嵌服务器。
			if(key is "bind" or "port" or "unixsocket" or "config-import-path" or "h" or "help" or "version" ||
				IsProcessArgument(setting.Value))
				throw new InvalidOperationException(string.Format(global::Zongsoft.Externals.Garnet.Properties.Resources.GarnetConnectionSettings_OptionUnsupported_Message, key));

			yield return key.Length == 1 ? $"-{key}" : $"--{key}";

			if(!string.IsNullOrWhiteSpace(setting.Value))
			{
				yield return key switch
				{
					"auth" => this.AuthenticationMode?.ToString(),
					"aof-commit-freq" => GetDurationArgument(this.CommitFrequency, TimeSpan.TicksPerMillisecond, -1, key),
					"cert-refresh-freq" => GetDurationArgument(this.CertificateRefreshFrequency, TimeSpan.TicksPerSecond, 0, key),
					_ => _paths.Contains(key) ? GetPath(setting.Value) : setting.Value,
				};
			}
		}
	}
	#endregion

	#region 重写方法
	protected override string GetKey(string key, bool validate = false) => base.GetKey(GetOptionName(key), validate);
	protected override void OnEntryChanged(string key, string value) => base.OnEntryChanged(GetOptionName(key), value);
	#endregion

	#region 私有方法
	private static string GetDurationArgument(TimeSpan? duration, long unit, int minimum, string name)
	{
		if(!duration.HasValue || duration.Value.Ticks % unit != 0 ||
			duration.Value.Ticks / unit < minimum || duration.Value.Ticks / unit > int.MaxValue)
			throw new InvalidOperationException(string.Format(global::Zongsoft.Externals.Garnet.Properties.Resources.GarnetConnectionSettings_DurationInvalid_Message, name));

		return (duration.Value.Ticks / unit).ToString(CultureInfo.InvariantCulture);
	}

	private static bool IsProcessArgument(string value) =>
		value != null && (value.Equals("--help", StringComparison.OrdinalIgnoreCase) ||
			value.Equals("--version", StringComparison.OrdinalIgnoreCase) ||
			value.StartsWith("--help=", StringComparison.OrdinalIgnoreCase) ||
			value.StartsWith("--version=", StringComparison.OrdinalIgnoreCase));

	private static string GetPath(string path)
	{
		if(string.IsNullOrEmpty(path))
			return string.Empty;

		if(Path.IsPathFullyQualified(path))
			return path;

		path = path.Trim(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

		if(path.Length == 0)
			return CurrentDirectory;

		if(path[0] == '~' && (path.Length == 1 || path[1] == Path.DirectorySeparatorChar || path[1] == Path.AltDirectorySeparatorChar))
			return Path.Combine(RootDirectory, path[1..].TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

		return Path.Combine(CurrentDirectory, path);
	}

	private static string GetOptionName(string key) => key != null && _aliases.TryGetValue(key, out var alias) ? alias : key;
	private static string RootDirectory => ApplicationContext.Current?.ApplicationPath ?? AppContext.BaseDirectory;
	private static string CurrentDirectory => field ??= Path.GetDirectoryName(typeof(GarnetServer).Assembly.Location);
	#endregion

	#region 嵌套子类
	private sealed class SecondsConverter() : DurationConverter(TimeSpan.TicksPerSecond) { }
	private sealed class MillisecondsConverter() : DurationConverter(TimeSpan.TicksPerMillisecond) { }

	private abstract class DurationConverter(long unit) : Components.Converters.TimeSpanConverter
	{
		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) =>
			value is string text && long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number) ?
			TimeSpan.FromTicks(checked(number * unit)) :
			base.ConvertFrom(context, culture, value);

		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) =>
			destinationType == typeof(string) && value is TimeSpan duration ?
			duration.ToString("c", CultureInfo.InvariantCulture) :
			base.ConvertTo(context, culture, value, destinationType);
	}
	#endregion
}

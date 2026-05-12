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
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Zongsoft.Services;
using Zongsoft.Configuration;

namespace Zongsoft.Externals.Garnet;

public class GarnetServer(string name = null) : Components.WorkerBase(name)
{
	#region 静态字段
	private static readonly Settings DEFAULT = new("lua=true;");
	private static readonly HashSet<string> _paths = new(StringComparer.OrdinalIgnoreCase)
	{
		"logdir",
		"acl-file",
		"file-logger",
		"unixsocket",
		"checkpointdir",
		"config-import-path",
		"config-export-path",
		"loadmodulecs",
		"extension-bin-paths",
	};

	private static readonly Dictionary<string, string> _aliases = new(StringComparer.OrdinalIgnoreCase)
	{
		{ "Quiet", "q" },
		{ "QuietMode", "q" },
		{ "Address", "bind" },
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
		{ "CheckpointDir", "checkpointdir" },
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
		{ "CommitFrequencyMs", "aof-commit-freq" },
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
		{ "CertFileName", "cert-file-name" },
		{ "CertPassword", "cert-password" },
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

	#region 成员字段
	private global::Garnet.GarnetServer _server;
	#endregion

	#region 公共属性
	/// <summary>获取或设置服务器设置名称。</summary>
	public string Setting { get => string.IsNullOrEmpty(field) ? this.Name : field; set; }
	#endregion

	#region 重写方法
	protected override Task OnStartAsync(string[] args, CancellationToken cancellation)
	{
		var settings = GetSettings(this.Setting);
		var arguments = GetArguments(settings);

		_server = new([.. arguments]);
		_server.Start();

		return Task.CompletedTask;
	}

	protected override Task OnStopAsync(string[] args, CancellationToken cancellation)
	{
		_server?.Dispose();
		return Task.CompletedTask;
	}
	#endregion

	#region 私有方法
	private static Settings GetSettings(string name)
	{
		var settings = ApplicationContext.Current?.Configuration.GetOption<SettingsCollection>("/Externals/Garnet");

		if(settings == null || settings.Count == 0)
			return DEFAULT;

		if(settings.TryGetValue(name ?? string.Empty, out var result))
			return result;

		if(!string.IsNullOrEmpty(name))
			return settings.TryGetValue(string.Empty, out result) ? result : DEFAULT;

		return DEFAULT;
	}

	private static IEnumerable<string> GetArguments(Settings settings)
	{
		if(settings == null || settings.IsEmpty)
			yield break;

		foreach(var setting in settings)
		{
			var key = setting.Key;

			if(_aliases.TryGetValue(key, out var alias))
				key = alias;

			if(key.Length == 1)
				yield return $"-{key}";
			else
				yield return $"--{key}";

			if(!string.IsNullOrWhiteSpace(setting.Value))
			{
				if(_paths.Contains(key))
					yield return GetPath(setting.Value);
				else
					yield return setting.Value;
			}
		}
	}

	private static string GetPath(string path)
	{
		if(string.IsNullOrEmpty(path))
			return string.Empty;

		if(Path.IsPathFullyQualified(path))
			return path;

		path = path
			.Trim(Path.DirectorySeparatorChar)
			.Trim(Path.AltDirectorySeparatorChar);

		if(path[0] == '~' && (path.Length == 1 || path[1] == Path.DirectorySeparatorChar || path[1] == Path.AltDirectorySeparatorChar))
			return Path.Combine(RootDirectory, path[1..].TrimStart(Path.DirectorySeparatorChar).TrimStart(Path.AltDirectorySeparatorChar));

		return Path.Combine(CurrentDirectory, path);
	}

	private static string RootDirectory => field ??= ApplicationContext.Current?.ApplicationPath ?? AppContext.BaseDirectory;
	private static string CurrentDirectory => field ??= Path.GetDirectoryName(typeof(GarnetServer).Assembly.Location);
	#endregion
}

/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
 *
 * The MIT License (MIT)
 * 
 * Copyright (C) 2020-2026 Zongsoft Corporation <http://www.zongsoft.com>
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using System.IO;
using System.Text;
using System.Formats.Tar;
using System.Buffers.Binary;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Cryptography;

using Zongsoft.Terminals;
using Zongsoft.Components;

namespace Zongsoft.Upgrading;

partial class Packager
{
	[CommandOption(NAME_OPTION, typeof(string), Required = true)]
	[CommandOption(VERSION_OPTION, typeof(Version), Required = true)]
	[CommandOption(PLATFORM_OPTION, typeof(Platform), Required = true)]
	[CommandOption(FRAMEWORK_OPTION, typeof(string), Required = true)]
	[CommandOption(SOURCE_OPTION, typeof(string))]
	[CommandOption(FORMAT_OPTION, typeof(InstallFormat), InstallFormat.Tarball)]
	[CommandOption(EDITION_OPTION, typeof(string))]
	[CommandOption(ARCHITECTURE_OPTION, typeof(string), "x64")]
	[CommandOption(OUTPUT_OPTION, typeof(string))]
	[CommandOption(DAEMON_OPTION, typeof(string))]
	[CommandOption(OVERWRITE_OPTION, typeof(bool), false)]
	[CommandOption(TITLE_OPTION, typeof(string))]
	[CommandOption(SUMMARY_OPTION, typeof(string))]
	[CommandOption(DESCRIPTION_OPTION, typeof(string))]
	[CommandOption(SCRIPT_INSTALLING_OPTION, typeof(string))]
	[CommandOption(SCRIPT_INSTALLED_OPTION, typeof(string))]
	[CommandOption(SCRIPT_UNINSTALLING_OPTION, typeof(string))]
	[CommandOption(SCRIPT_UNINSTALLED_OPTION, typeof(string))]
	public sealed class InstallCommand : CommandBase<CommandContext>
	{
		#region 常量定义
		private const string NAME_OPTION = "name";
		private const string TITLE_OPTION = "title";
		private const string SOURCE_OPTION = "source";
		private const string OUTPUT_OPTION = "output";
		private const string FORMAT_OPTION = "format";
		private const string EDITION_OPTION = "edition";
		private const string VERSION_OPTION = "version";
		private const string PLATFORM_OPTION = "platform";
		private const string FRAMEWORK_OPTION = "framework";
		private const string OVERWRITE_OPTION = "overwrite";
		private const string ARCHITECTURE_OPTION = "architecture";
		private const string DAEMON_OPTION = "daemon";
		private const string SUMMARY_OPTION = "summary";
		private const string DESCRIPTION_OPTION = "description";
		private const string SCRIPT_INSTALLING_OPTION = "script.installing";
		private const string SCRIPT_INSTALLED_OPTION = "script.installed";
		private const string SCRIPT_UNINSTALLING_OPTION = "script.uninstalling";
		private const string SCRIPT_UNINSTALLED_OPTION = "script.uninstalled";

		private const string DEFAULT_INSTALL_ROOT = "/usr/local";
		#endregion

		#region 执行方法
		protected override ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
		{
			var name = context.Options.GetValue<string>(NAME_OPTION);
			var edition = context.Options.GetValue<string>(EDITION_OPTION);
			var version = context.Options.GetValue<Version>(VERSION_OPTION);
			var platform = context.Options.GetValue<Platform>(PLATFORM_OPTION);
			var format = context.Options.GetValue<InstallFormat>(FORMAT_OPTION);
			var architecture = GetArchitecture(context.Options.GetValue<string>(ARCHITECTURE_OPTION));

			if(version.IsZero())
			{
				Terminal.WriteLine(CommandOutletColor.Red, $"The version number is invalid.");
				return ValueTask.FromResult<object>(null);
			}

			if(platform != Platform.Windows && platform != Platform.Linux)
				throw new CommandOptionValueException(PLATFORM_OPTION, platform.ToString());

			if((format == InstallFormat.Deb || format == InstallFormat.Rpm) && platform != Platform.Linux)
				Terminal.WriteLine(CommandOutletColor.DarkYellow, $"[Warn] The '{format}' install format is normally used for Linux packages.");

			var runtime = Application.GetRuntimeIdentifier(platform, architecture);
			var packageName = GetPackageName(name, edition);
			var installRoot = $"{DEFAULT_INSTALL_ROOT}/{packageName}";
			var variables = GetVariables(context, architecture, runtime, installRoot);

			if(!Normalizer.Normalize(context.Options.GetValue<string>(SOURCE_OPTION), variables, out var source))
				return ValueTask.FromResult<object>(null);

			if(string.IsNullOrEmpty(source))
				source = Environment.CurrentDirectory;
			else if(!Path.IsPathFullyQualified(source))
				source = Path.Combine(Environment.CurrentDirectory, source);

			if(!Directory.Exists(source))
			{
				Terminal.WriteLine(CommandOutletColor.Red, $"The source directory '{source}' does not exist.");
				return ValueTask.FromResult<object>(null);
			}

			if(!Normalizer.Normalize(context.Options.GetValue<string>(OUTPUT_OPTION), variables, out var output))
				return ValueTask.FromResult<object>(null);

			source = Path.GetFullPath(source);
			output = GetOutputPath(source, output, name, edition, version, runtime, format);

			variables[SOURCE_OPTION] = source;
			variables[OUTPUT_OPTION] = output;

			var metadata = new InstallMetadata(
				name,
				packageName,
				edition,
				version,
				runtime,
				platform,
				architecture,
				context.Options.GetValue<string>(FRAMEWORK_OPTION),
				NormalizeText(context.Options.GetValue<string>(TITLE_OPTION), variables),
				NormalizeText(context.Options.GetValue<string>(SUMMARY_OPTION), variables),
				NormalizeText(context.Options.GetValue<string>(DESCRIPTION_OPTION), variables),
				installRoot);

			var daemonPath = GetDaemonPath(context, source, variables);
			var daemonEntryName = daemonPath == null ? null : GetDaemonEntryName(source, daemonPath);
			var scripts = GetScripts(context, source, variables, metadata, daemonPath, daemonEntryName);
			var entries = GetEntries(source, context.Arguments, variables, GetPackagePrefix(format, metadata));

			if(daemonPath != null)
				AddFile(entries, new HashSet<string>(entries.ConvertAll(entry => entry.EntryName), StringComparer.Ordinal), daemonPath, daemonEntryName, GetPackagePrefix(format, metadata));

			if(entries.Count == 0)
			{
				Terminal.WriteLine(CommandOutletColor.Red, $"The source directory '{source}' does not contain any package entries.");
				return ValueTask.FromResult<object>(null);
			}

			var directory = Path.GetDirectoryName(output);
			if(!string.IsNullOrEmpty(directory))
				Directory.CreateDirectory(directory);

			if(File.Exists(output))
			{
				if(context.Options.Switch(OVERWRITE_OPTION))
					File.Delete(output);
				else
					throw new IOException($"The output file '{output}' already exists.");
			}

			Terminal.WriteLine(CommandOutletColor.DarkCyan, $"Installing package generation in progress, please wait...");
			Terminal.WriteLine();

			switch(format)
			{
				case InstallFormat.Tarball:
					GenerateTarball(output, entries, scripts);
					break;
				case InstallFormat.Deb:
					GenerateDeb(output, metadata, entries, scripts);
					break;
				case InstallFormat.Rpm:
					GenerateRpm(output, metadata, entries, scripts);
					break;
				default:
					throw new CommandOptionValueException(FORMAT_OPTION, format.ToString());
			}

			Terminal.WriteLine(CommandOutletColor.DarkGreen, string.Format(Properties.Resources.PackageGeneratedSuccessfully_Message, output));
			return ValueTask.FromResult<object>(output);
		}
		#endregion

		#region 打包方法
		static void GenerateTarball(string output, IReadOnlyCollection<InstallEntry> entries, InstallScripts scripts)
		{
			using var stream = File.Create(output);
			using var gzip = new GZipStream(stream, CompressionLevel.Optimal);
			using var writer = new TarWriter(gzip, TarEntryFormat.Pax, false);

			foreach(var entry in entries)
				WriteTarEntry(writer, entry);

			WriteTarScript(writer, ".install/installing.sh", scripts.Installing);
			WriteTarScript(writer, ".install/installed.sh", scripts.Installed);
			WriteTarScript(writer, ".install/uninstalling.sh", scripts.Uninstalling);
			WriteTarScript(writer, ".install/uninstalled.sh", scripts.Uninstalled);
		}

		static void GenerateDeb(string output, InstallMetadata metadata, IReadOnlyCollection<InstallEntry> entries, InstallScripts scripts)
		{
			var control = GetDebControl(metadata, entries);
			using var stream = File.Create(output);

			WriteArHeader(stream);
			WriteArEntry(stream, "debian-binary", Encoding.ASCII.GetBytes("2.0\n"));
			WriteArEntry(stream, "control.tar.gz", CreateControlTarball(control, scripts));
			WriteArEntry(stream, "data.tar.gz", CreateDataTarball(entries));
		}

		static void GenerateRpm(string output, InstallMetadata metadata, IReadOnlyCollection<InstallEntry> entries, InstallScripts scripts)
		{
			var payload = CreateCpioPayload(entries, out var archiveSize);
			var header = RpmHeader.Create(metadata, entries, scripts, archiveSize);
			var body = Combine(header, payload);
			var signature = RpmSignature.Create(body);

			using var stream = File.Create(output);
			WriteRpmLead(stream, metadata);
			stream.Write(signature);
			stream.Write(body);
		}
		#endregion

		#region 条目方法
		static List<InstallEntry> GetEntries(string source, IReadOnlyCollection<string> arguments, IDictionary<string, string> variables, string prefix)
		{
			var entries = new List<InstallEntry>();
			var names = new HashSet<string>(StringComparer.Ordinal);

			if(arguments == null || arguments.Count == 0)
			{
				foreach(var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
					AddEntry(entries, names, source, file, Path.GetRelativePath(source, file), prefix);

				return entries;
			}

			foreach(var argument in arguments)
			{
				if(!Normalizer.Normalize(argument, variables, out var text))
					continue;

				var index = text.LastIndexOf(':');

				if(OperatingSystem.IsWindows() && index == 1)
					index = -1;

				var path = index > 0 ? text[..index].Trim() : text;
				var alias = index > 0 ? text[(index + 1)..].Trim() : null;

				AddEntry(entries, names, source, path, alias, prefix);
			}

			return entries;
		}

		static void AddEntry(List<InstallEntry> entries, ISet<string> names, string source, string path, string alias, string prefix)
		{
			if(alias != null)
				alias = alias
					.Trim('~')
					.Trim(Path.DirectorySeparatorChar)
					.Trim(Path.AltDirectorySeparatorChar);
			else if(IsExternal(source, path))
				alias = string.Empty;

			if(!Path.IsPathFullyQualified(path))
				path = Path.Combine(source, path);

			if(path.Contains('*') || path.Contains('?'))
			{
				var working = Path.GetDirectoryName(path);
				var pattern = Path.GetFileName(path);

				if(string.IsNullOrEmpty(working) || !Directory.Exists(working))
				{
					Terminal.WriteLine(CommandOutletColor.DarkYellow, $"[Warn] The source path '{path}' does not exist.");
					return;
				}

				alias ??= Path.GetRelativePath(source, working);

				if(alias == "." || alias.StartsWith(".."))
					alias = string.Empty;

				foreach(var file in Directory.GetFiles(working, pattern))
					AddFile(entries, names, file, Path.Combine(alias, Path.GetFileName(file)), prefix);

				foreach(var directory in Directory.GetDirectories(working, pattern))
					AddDirectory(entries, names, source, directory, Path.Combine(alias, Path.GetFileName(directory)), prefix);
			}
			else
			{
				alias ??= Path.GetRelativePath(source, path);

				if(alias == "." || alias.StartsWith(".."))
					alias = string.Empty;

				if(File.Exists(path))
					AddFile(entries, names, path, alias, prefix);
				else if(Directory.Exists(path))
					AddDirectory(entries, names, source, path, alias, prefix);
				else
					Terminal.WriteLine(CommandOutletColor.DarkYellow, $"[Warn] The source path '{path}' does not exist.");
			}
		}

		static void AddDirectory(List<InstallEntry> entries, ISet<string> names, string source, string path, string alias, string prefix)
		{
			foreach(var file in Directory.GetFiles(path))
				AddFile(entries, names, file, Path.Combine(alias, Path.GetFileName(file)), prefix);

			foreach(var directory in Directory.GetDirectories(path))
				AddDirectory(entries, names, source, directory, Path.Combine(alias, Path.GetFileName(directory)), prefix);
		}

		static void AddFile(List<InstallEntry> entries, ISet<string> names, string source, string entryName, string prefix)
		{
			if(string.IsNullOrEmpty(entryName))
				entryName = Path.GetFileName(source);
			else
			{
				var filename = Path.GetFileName(entryName);

				if(string.IsNullOrEmpty(filename) || filename == ".")
					entryName = Path.Combine(Path.GetDirectoryName(entryName), Path.GetFileName(source));
			}

			entryName = NormalizeEntryName(Path.Combine(prefix ?? string.Empty, entryName));

			if(!names.Add(entryName))
			{
				Terminal.WriteLine(CommandOutletColor.DarkYellow, $"[Warn] The source file '{source}' conflicts with an existing package entry '{entryName}'.");
				return;
			}

			var file = new FileInfo(source);
			entries.Add(new InstallEntry(source, entryName, file.Length, GetUnixTime(file.LastWriteTimeUtc), GetFileMode(source)));
		}

		static bool IsExternal(string source, string path)
		{
			return Path.IsPathFullyQualified(path) &&
				!Path.GetFullPath(path).StartsWith(source, GetComparison());

			static StringComparison GetComparison() => OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
		}
		#endregion

		#region 脚本方法
		static string GetDaemonPath(CommandContext context, string source, IDictionary<string, string> variables)
		{
			if(!context.Options.TryGetValue<string>(DAEMON_OPTION, out var daemon) || string.IsNullOrWhiteSpace(daemon))
				return null;

			if(!Normalizer.Normalize(daemon, variables, out daemon))
				return null;

			if(!Path.IsPathFullyQualified(daemon))
				daemon = Path.Combine(source, daemon);

			if(!File.Exists(daemon))
				throw new FileNotFoundException($"The daemon service file '{daemon}' does not exist.", daemon);

			return Path.GetFullPath(daemon);
		}

		static string GetDaemonEntryName(string source, string daemon)
		{
			return IsExternal(source, daemon) ? Path.GetFileName(daemon) : NormalizeEntryName(Path.GetRelativePath(source, daemon));
		}

		static InstallScripts GetScripts(CommandContext context, string source, IDictionary<string, string> variables, InstallMetadata metadata, string daemon, string daemonEntryName)
		{
			var scripts = new InstallScripts(
				ReadScript(context, SCRIPT_INSTALLING_OPTION, source, variables),
				ReadScript(context, SCRIPT_INSTALLED_OPTION, source, variables),
				ReadScript(context, SCRIPT_UNINSTALLING_OPTION, source, variables),
				ReadScript(context, SCRIPT_UNINSTALLED_OPTION, source, variables));

			if(daemon == null)
				return scripts;

			if(metadata.Platform != Platform.Linux)
				Terminal.WriteLine(CommandOutletColor.DarkYellow, $"[Warn] The '{DAEMON_OPTION}' option is intended for Linux systemd services.");

			var service = Path.GetFileName(daemon);
			var servicePath = $"{metadata.InstallRoot}/{daemonEntryName}";
			var serviceLink = $"/etc/systemd/system/{service}";

			var installing = $$"""
				if command -v systemctl >/dev/null 2>&1; then
					systemctl stop '{{service}}' >/dev/null 2>&1 || true
				fi
				""";

			var installed = $$"""
				install -d /etc/systemd/system
				ln -sfn '{{servicePath}}' '{{serviceLink}}'
				if command -v systemctl >/dev/null 2>&1; then
					systemctl daemon-reload >/dev/null 2>&1 || true
					systemctl enable '{{service}}' >/dev/null 2>&1 || true
				fi
				""";

			var uninstalling = $$"""
				if command -v systemctl >/dev/null 2>&1; then
					systemctl stop '{{service}}' >/dev/null 2>&1 || true
				fi
				""";

			var uninstalled = $$"""
				rm -f '{{serviceLink}}'
				if command -v systemctl >/dev/null 2>&1; then
					systemctl daemon-reload >/dev/null 2>&1 || true
				fi
				rm -rf '{{metadata.InstallRoot}}'
				""";

			return new InstallScripts(
				CombineScript(installing, scripts.Installing),
				CombineScript(installed, scripts.Installed),
				CombineScript(uninstalling, scripts.Uninstalling),
				CombineScript(uninstalled, scripts.Uninstalled));
		}

		static string ReadScript(CommandContext context, string option, string source, IDictionary<string, string> variables)
		{
			if(!context.Options.TryGetValue<string>(option, out var path) || string.IsNullOrWhiteSpace(path))
				return null;

			if(!Normalizer.Normalize(path, variables, out path))
				return null;

			if(!Path.IsPathFullyQualified(path))
				path = Path.Combine(source, path);

			if(!File.Exists(path))
				throw new FileNotFoundException($"The script file '{path}' does not exist.", path);

			return File.ReadAllText(path);
		}

		static string CombineScript(string first, string second)
		{
			if(string.IsNullOrWhiteSpace(first))
				return string.IsNullOrWhiteSpace(second) ? null : second.Trim();
			if(string.IsNullOrWhiteSpace(second))
				return first.Trim();

			return first.Trim() + Environment.NewLine + Environment.NewLine + second.Trim();
		}
		#endregion

		#region Tar 方法
		static byte[] CreateDataTarball(IReadOnlyCollection<InstallEntry> entries)
		{
			using var memory = new MemoryStream();
			using(var gzip = new GZipStream(memory, CompressionLevel.Optimal, true))
			using(var writer = new TarWriter(gzip, TarEntryFormat.Pax, true))
			{
				foreach(var entry in entries)
					WriteTarEntry(writer, entry);
			}

			return memory.ToArray();
		}

		static byte[] CreateControlTarball(string control, InstallScripts scripts)
		{
			using var memory = new MemoryStream();
			using(var gzip = new GZipStream(memory, CompressionLevel.Optimal, true))
			using(var writer = new TarWriter(gzip, TarEntryFormat.Pax, true))
			{
				WriteTarText(writer, "control", control, 0644);
				WriteTarScript(writer, "preinst", scripts.Installing);
				WriteTarScript(writer, "postinst", scripts.Installed);
				WriteTarScript(writer, "prerm", scripts.Uninstalling);
				WriteTarScript(writer, "postrm", scripts.Uninstalled);
			}

			return memory.ToArray();
		}

		static void WriteTarEntry(TarWriter writer, InstallEntry item)
		{
			var entry = new PaxTarEntry(TarEntryType.RegularFile, item.EntryName)
			{
				Mode = (UnixFileMode)item.Mode,
				ModificationTime = DateTimeOffset.FromUnixTimeSeconds(item.ModifiedTime),
				DataStream = File.OpenRead(item.Source),
			};

			writer.WriteEntry(entry);
			entry.DataStream.Dispose();
		}

		static void WriteTarScript(TarWriter writer, string name, string script)
		{
			if(string.IsNullOrWhiteSpace(script))
				return;

			WriteTarText(writer, name, "#!/bin/sh" + Environment.NewLine + "set -e" + Environment.NewLine + script.Trim() + Environment.NewLine, 0755);
		}

		static void WriteTarText(TarWriter writer, string name, string text, int mode)
		{
			var data = Encoding.UTF8.GetBytes(text ?? string.Empty);
			var entry = new PaxTarEntry(TarEntryType.RegularFile, name)
			{
				Mode = (UnixFileMode)mode,
				ModificationTime = DateTimeOffset.UtcNow,
				DataStream = new MemoryStream(data),
			};

			writer.WriteEntry(entry);
			entry.DataStream.Dispose();
		}
		#endregion

		#region Deb 方法
		static string GetDebControl(InstallMetadata metadata, IReadOnlyCollection<InstallEntry> entries)
		{
			var builder = new StringBuilder();
			var summary = string.IsNullOrWhiteSpace(metadata.Summary) ? metadata.Title : metadata.Summary;
			var description = string.IsNullOrWhiteSpace(metadata.Description) ? summary : metadata.Description;

			builder.AppendLine($"Package: {metadata.PackageName}");
			builder.AppendLine($"Version: {metadata.Version}");
			builder.AppendLine($"Section: utils");
			builder.AppendLine($"Priority: optional");
			builder.AppendLine($"Architecture: {GetDebianArchitecture(metadata.Architecture)}");
			builder.AppendLine($"Installed-Size: {Math.Max(1, (GetInstallSize(entries) + 1023) / 1024)}");
			builder.AppendLine($"Maintainer: Zongsoft Studio <zongsoft@qq.com>");
			builder.AppendLine($"Homepage: https://github.com/Zongsoft/framework");
			builder.AppendLine($"Description: {NormalizeDebText(summary ?? metadata.Name)}");

			if(!string.IsNullOrWhiteSpace(description))
			{
				foreach(var line in description.Replace("\r", string.Empty).Split('\n'))
					builder.AppendLine(string.IsNullOrWhiteSpace(line) ? " ." : $" {NormalizeDebText(line)}");
			}

			return builder.ToString();
		}

		static void WriteArHeader(Stream stream) => stream.Write(Encoding.ASCII.GetBytes("!<arch>\n"));

		static void WriteArEntry(Stream stream, string name, byte[] data)
		{
			var header = Encoding.ASCII.GetBytes(string.Format(
				System.Globalization.CultureInfo.InvariantCulture,
				"{0,-16}{1,-12}{2,-6}{3,-6}{4,-8}{5,-10}`\n",
				name.EndsWith('/') ? name : name + "/",
				DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
				0,
				0,
				"100644",
				data.Length));

			stream.Write(header);
			stream.Write(data);

			if((data.Length & 1) != 0)
				stream.WriteByte((byte)'\n');
		}
		#endregion

		#region Rpm 方法
		static byte[] CreateCpioPayload(IReadOnlyCollection<InstallEntry> entries, out long archiveSize)
		{
			using var raw = new MemoryStream();
			var directories = GetRpmDirectories(entries);
			var inode = 1;

			foreach(var directory in directories)
				WriteCpioEntry(raw, inode++, "." + directory, 0040755, 0, DateTimeOffset.UtcNow.ToUnixTimeSeconds(), null);

			foreach(var entry in entries)
			{
				using var file = File.OpenRead(entry.Source);
				WriteCpioEntry(raw, inode++, "." + GetRpmPath(entry.EntryName), 0100000 | entry.Mode, entry.Size, entry.ModifiedTime, file);
			}

			WriteCpioEntry(raw, inode, "TRAILER!!!", 0, 0, 0, null);
			Pad(raw, 512);

			archiveSize = raw.Length;

			using var compressed = new MemoryStream();
			raw.Position = 0;
			using(var gzip = new GZipStream(compressed, CompressionLevel.Optimal, true))
				raw.CopyTo(gzip);

			return compressed.ToArray();
		}

		static void WriteCpioEntry(Stream stream, int inode, string name, int mode, long size, long mtime, Stream data)
		{
			var namesize = Encoding.UTF8.GetByteCount(name) + 1;
			var header = string.Create(System.Globalization.CultureInfo.InvariantCulture, $"070701{inode:x8}{mode:x8}{0:x8}{0:x8}{1:x8}{mtime:x8}{size:x8}{0:x8}{0:x8}{0:x8}{0:x8}{namesize:x8}{0:x8}");

			stream.Write(Encoding.ASCII.GetBytes(header));
			stream.Write(Encoding.UTF8.GetBytes(name));
			stream.WriteByte(0);
			Pad(stream, 4);

			if(data != null)
				data.CopyTo(stream);

			Pad(stream, 4);
		}

		static void WriteRpmLead(Stream stream, InstallMetadata metadata)
		{
			var lead = new byte[96];
			lead[0] = 0xed;
			lead[1] = 0xab;
			lead[2] = 0xee;
			lead[3] = 0xdb;
			lead[4] = 3;
			lead[5] = 0;
			WriteInt16(lead.AsSpan(6), 0);
			WriteInt16(lead.AsSpan(8), GetRpmArchitectureNumber(metadata.Architecture));

			var name = Encoding.ASCII.GetBytes($"{metadata.PackageName}-{metadata.Version}");
			name.AsSpan(0, Math.Min(name.Length, 65)).CopyTo(lead.AsSpan(10));

			WriteInt16(lead.AsSpan(76), 1);
			WriteInt16(lead.AsSpan(78), 5);
			stream.Write(lead);
		}

		static byte[] Combine(byte[] first, byte[] second)
		{
			var result = new byte[first.Length + second.Length];
			Buffer.BlockCopy(first, 0, result, 0, first.Length);
			Buffer.BlockCopy(second, 0, result, first.Length, second.Length);
			return result;
		}

		sealed class RpmSignature
		{
			public static byte[] Create(byte[] body)
			{
				using var md5 = MD5.Create();
				var digest = md5.ComputeHash(body);
				var header = new RpmHeaderBuilder();

				header.AddInt32(257, body.Length);
				header.AddBinary(261, digest);

				return header.Build(true);
			}
		}

		sealed class RpmHeader
		{
			public static byte[] Create(InstallMetadata metadata, IReadOnlyCollection<InstallEntry> entries, InstallScripts scripts, long archiveSize)
			{
				var builder = new RpmHeaderBuilder();
				var buildTime = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
				var rpmEntries = GetRpmEntries(entries);

				builder.AddString(1000, metadata.PackageName);
				builder.AddString(1001, metadata.Version.ToString());
				builder.AddString(1002, string.IsNullOrWhiteSpace(metadata.Edition) ? "1" : metadata.Edition);
				builder.AddInternationalString(1004, metadata.Summary ?? metadata.Title ?? metadata.Name);
				builder.AddInternationalString(1005, metadata.Description ?? metadata.Summary ?? metadata.Name);
				builder.AddInt32(1006, buildTime);
				builder.AddString(1007, Environment.MachineName);
				builder.AddInt32(1009, (int)Math.Min(int.MaxValue, GetInstallSize(entries)));
				builder.AddString(1014, "MIT");
				builder.AddString(1015, "Zongsoft Studio <zongsoft@qq.com>");
				builder.AddString(1016, "Applications/System");
				builder.AddString(1020, "https://github.com/Zongsoft/framework");
				builder.AddString(1021, "linux");
				builder.AddString(1022, GetRpmArchitecture(metadata.Architecture));
				builder.AddScript(1023, scripts.Installing);
				builder.AddScript(1024, scripts.Installed);
				builder.AddScript(1025, scripts.Uninstalling);
				builder.AddScript(1026, scripts.Uninstalled);
				builder.AddInt32Array(1028, rpmEntries.ConvertAll(entry => (int)Math.Min(int.MaxValue, entry.Size)));
				builder.AddInt16Array(1030, rpmEntries.ConvertAll(entry => (short)entry.Mode));
				builder.AddInt16Array(1033, rpmEntries.ConvertAll(_ => (short)0));
				builder.AddInt32Array(1034, rpmEntries.ConvertAll(entry => (int)entry.ModifiedTime));
				builder.AddStringArray(1035, rpmEntries.ConvertAll(entry => entry.Digest));
				builder.AddStringArray(1036, rpmEntries.ConvertAll(_ => string.Empty));
				builder.AddInt32Array(1037, rpmEntries.ConvertAll(_ => 0));
				builder.AddStringArray(1039, rpmEntries.ConvertAll(_ => "root"));
				builder.AddStringArray(1040, rpmEntries.ConvertAll(_ => "root"));
				builder.AddInt32Array(1045, rpmEntries.ConvertAll(_ => -1));
				builder.AddInt32(1046, (int)Math.Min(int.MaxValue, archiveSize));
				builder.AddStringArray(1047, [metadata.PackageName]);
				builder.AddInt32Array(1048, [0]);
				builder.AddStringArray(1049, ["rpmlib(CompressedFileNames)", "rpmlib(PayloadFilesHavePrefix)"]);
				builder.AddStringArray(1050, [""]);
				builder.AddString(1056, metadata.InstallRoot);
				builder.AddString(1124, "cpio");
				builder.AddString(1125, "gzip");
				builder.AddString(1126, "9");
				builder.AddInt32Array(1095, rpmEntries.ConvertAll(_ => 1));
				builder.AddInt32Array(1096, rpmEntries.ConvertAll(entry => entry.Inode));
				builder.AddStringArray(1097, rpmEntries.ConvertAll(_ => string.Empty));
				builder.AddInt32Array(1116, rpmEntries.ConvertAll(entry => entry.DirectoryIndex));
				builder.AddStringArray(1117, rpmEntries.ConvertAll(entry => entry.BaseName));
				builder.AddStringArray(1118, rpmEntries.Directories);
				builder.AddInt32Array(1140, rpmEntries.ConvertAll(_ => 0));
				builder.AddStringArray(1142, [""]);
				builder.AddInt32Array(5011, [1]);

				return builder.Build(false);
			}
		}

		sealed class RpmHeaderBuilder
		{
			private readonly List<RpmHeaderIndex> _indexes = [];
			private readonly MemoryStream _store = new();

			public void AddString(int tag, string value) => Add(tag, 6, 1, () => WriteString(value ?? string.Empty));
			public void AddInternationalString(int tag, string value) => Add(tag, 9, 1, () => WriteString(value ?? string.Empty));
			public void AddScript(int tag, string value)
			{
				if(!string.IsNullOrWhiteSpace(value))
					AddString(tag, "#!/bin/sh\nset -e\n" + value.Trim() + "\n");
			}
			public void AddBinary(int tag, byte[] value) => Add(tag, 7, value.Length, () => _store.Write(value));
			public void AddInt32(int tag, int value) => AddInt32Array(tag, [value]);
			public void AddInt32Array(int tag, IReadOnlyList<int> values) => Add(tag, 4, values.Count, () =>
			{
				Span<byte> buffer = stackalloc byte[4];

				foreach(var value in values)
				{
					WriteInt32(buffer, value);
					_store.Write(buffer);
				}
			});
			public void AddInt16Array(int tag, IReadOnlyList<short> values) => Add(tag, 3, values.Count, () =>
			{
				Span<byte> buffer = stackalloc byte[2];

				foreach(var value in values)
				{
					WriteInt16(buffer, value);
					_store.Write(buffer);
				}
			});
			public void AddStringArray(int tag, IReadOnlyList<string> values) => Add(tag, 8, values.Count, () =>
			{
				foreach(var value in values)
					WriteString(value ?? string.Empty);
			});

			public byte[] Build(bool signature)
			{
				using var stream = new MemoryStream();
				Span<byte> buffer = stackalloc byte[16];

				stream.Write(signature ? [0x8e, 0xad, 0xe8, 0x01] : [0x8e, 0xad, 0xe8, 0x01]);
				stream.WriteByte(0);
				stream.Write([0, 0, 0]);
				WriteInt32(buffer[..4], _indexes.Count);
				stream.Write(buffer[..4]);
				WriteInt32(buffer[..4], (int)_store.Length);
				stream.Write(buffer[..4]);

				foreach(var index in _indexes)
				{
					WriteInt32(buffer[..4], index.Tag);
					WriteInt32(buffer[4..8], index.Type);
					WriteInt32(buffer[8..12], index.Offset);
					WriteInt32(buffer[12..16], index.Count);
					stream.Write(buffer);
				}

				_store.Position = 0;
				_store.CopyTo(stream);
				Pad(stream, 8);

				return stream.ToArray();
			}

			private void Add(int tag, int type, int count, Action writer)
			{
				Align(_store, GetAlignment(type));
				_indexes.Add(new(tag, type, (int)_store.Position, count));
				writer();
			}

			private void WriteString(string value)
			{
				_store.Write(Encoding.UTF8.GetBytes(value));
				_store.WriteByte(0);
			}

			static int GetAlignment(int type) => type switch
			{
				3 => 2,
				4 => 4,
				5 => 8,
				_ => 1,
			};
		}

		sealed class RpmEntryCollection : List<RpmEntry>
		{
			public List<string> Directories { get; } = [];
		}
		#endregion

		#region 辅助方法
		static Dictionary<string, string> GetVariables(CommandContext context, Architecture architecture, string runtime, string installRoot)
		{
			var variables = Packager.GetVariables();

			foreach(var option in context.Options)
			{
				if(option.Value != null)
					variables[option.Key] = option.Value.ToString();
			}

			variables["Runtime"] = runtime;
			variables["RuntimeIdentifier"] = runtime;
			variables["Architecture"] = architecture.ToString().ToLowerInvariant();
			variables["InstallRoot"] = installRoot;

			return variables;
		}

		static string GetOutputPath(string source, string output, string name, string edition, Version version, string runtime, InstallFormat format)
		{
			var extension = GetExtension(format);

			if(string.IsNullOrEmpty(output))
				output = Path.Combine(source, GetFileName(name, edition, version, runtime) + extension);
			else
			{
				if(!Path.IsPathFullyQualified(output))
					output = Path.Combine(source, output);

				if(Directory.Exists(output) || EndsWithDirectorySeparator(output) || string.IsNullOrEmpty(Path.GetExtension(output)))
					output = Path.Combine(output, GetFileName(name, edition, version, runtime) + extension);
				else if(!HasExtension(output, extension))
					output += extension;
			}

			return Path.GetFullPath(output);
		}

		static string GetFileName(string name, string edition, Version version, string runtime) => string.IsNullOrEmpty(edition) ?
			$"{name}@{version}_{runtime}" :
			$"{name}({edition})@{version}_{runtime}";

		static string GetExtension(InstallFormat format) => format switch
		{
			InstallFormat.Tarball => ".tar.gz",
			InstallFormat.Deb => ".deb",
			InstallFormat.Rpm => ".rpm",
			_ => throw new CommandOptionValueException(FORMAT_OPTION, format.ToString()),
		};

		static bool HasExtension(string path, string extension)
		{
			return path.EndsWith(extension, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
		}

		static bool EndsWithDirectorySeparator(string path)
		{
			return path.EndsWith(Path.DirectorySeparatorChar) || path.EndsWith(Path.AltDirectorySeparatorChar);
		}

		static Architecture GetArchitecture(string value)
		{
			if(string.IsNullOrWhiteSpace(value))
				return Architecture.X64;

			return value.Trim().ToLowerInvariant() switch
			{
				"x64" or "amd64" => Architecture.X64,
				"x32" or "x86" or "i386" => Architecture.X86,
				"arm64" or "aarch64" => Architecture.Arm64,
				"arm32" or "arm" or "armhf" => Architecture.Arm32,
				_ => throw new CommandOptionValueException(ARCHITECTURE_OPTION, value),
			};
		}

		static string GetPackagePrefix(InstallFormat format, InstallMetadata metadata)
		{
			return format == InstallFormat.Tarball ? null : metadata.InstallRoot.TrimStart('/');
		}

		static string GetPackageName(string name, string edition)
		{
			var value = string.IsNullOrWhiteSpace(edition) ? name : $"{name}-{edition}";
			var builder = new StringBuilder(value.Length);
			var dash = false;

			foreach(var ch in value.ToLowerInvariant())
			{
				if(char.IsLetterOrDigit(ch) || ch == '+' || ch == '-' || ch == '.')
				{
					builder.Append(ch);
					dash = false;
				}
				else if(!dash)
				{
					builder.Append('-');
					dash = true;
				}
			}

			return builder.ToString().Trim('-', '.');
		}

		static string NormalizeText(string text, IDictionary<string, string> variables)
		{
			if(string.IsNullOrWhiteSpace(text))
				return null;

			if(!Normalizer.Normalize(text, variables, out var result))
				return null;

			return File.Exists(result) ? File.ReadAllText(result) : result;
		}

		static string NormalizeEntryName(string value)
		{
			if(string.IsNullOrWhiteSpace(value))
				return string.Empty;

			return value
				.Replace(Path.DirectorySeparatorChar, '/')
				.Replace(Path.AltDirectorySeparatorChar, '/')
				.TrimStart('/');
		}

		static string GetRpmPath(string entryName) => "/" + NormalizeEntryName(entryName);

		static List<string> GetRpmDirectories(IReadOnlyCollection<InstallEntry> entries)
		{
			var result = new SortedSet<string>(StringComparer.Ordinal) { "/" };

			foreach(var entry in entries)
			{
				var directory = Path.GetDirectoryName(GetRpmPath(entry.EntryName))?.Replace('\\', '/');

				while(!string.IsNullOrEmpty(directory) && directory != "/")
				{
					result.Add(directory);
					directory = Path.GetDirectoryName(directory)?.Replace('\\', '/');
				}
			}

			return [.. result];
		}

		static RpmEntryCollection GetRpmEntries(IReadOnlyCollection<InstallEntry> entries)
		{
			var result = new RpmEntryCollection();
			var directories = GetRpmDirectories(entries);

			foreach(var directory in directories)
			{
				var fullName = directory == "/" ? "/" : directory + "/";
				AddRpmEntry(result, directories, fullName, 0, 0040755, DateTimeOffset.UtcNow.ToUnixTimeSeconds(), string.Empty);
			}

			foreach(var entry in entries)
			{
				using var stream = File.OpenRead(entry.Source);
				var digest = Convert.ToHexString(SHA1.HashData(stream)).ToLowerInvariant();
				AddRpmEntry(result, directories, GetRpmPath(entry.EntryName), entry.Size, 0100000 | entry.Mode, entry.ModifiedTime, digest);
			}

			result.Directories.AddRange(directories.ConvertAll(directory => directory.EndsWith('/') ? directory : directory + "/"));
			return result;
		}

		static void AddRpmEntry(RpmEntryCollection entries, IReadOnlyList<string> directories, string fullName, long size, int mode, long modified, string digest)
		{
			var name = fullName.TrimEnd('/');
			var directory = fullName.EndsWith('/') ?
				Path.GetDirectoryName(name)?.Replace('\\', '/') :
				Path.GetDirectoryName(fullName)?.Replace('\\', '/');

			if(string.IsNullOrEmpty(directory))
				directory = "/";

			var directoryName = directory.EndsWith('/') ? directory : directory + "/";
			var directoryIndex = IndexOf(directories, directory);

			if(directoryIndex < 0)
				directoryIndex = IndexOf(directories, directoryName.TrimEnd('/'));

			entries.Add(new(
				entries.Count + 1,
				size,
				mode,
				modified,
				digest,
				directoryIndex < 0 ? 0 : directoryIndex,
				fullName == "/" ? string.Empty : Path.GetFileName(name)));
		}

		static string GetDebianArchitecture(Architecture architecture) => architecture switch
		{
			Architecture.X64 => "amd64",
			Architecture.X86 => "i386",
			Architecture.Arm64 => "arm64",
			Architecture.Arm32 => "armhf",
			_ => "all",
		};

		static string GetRpmArchitecture(Architecture architecture) => architecture switch
		{
			Architecture.X64 => "x86_64",
			Architecture.X86 => "i386",
			Architecture.Arm64 => "aarch64",
			Architecture.Arm32 => "armv7hl",
			_ => "noarch",
		};

		static short GetRpmArchitectureNumber(Architecture architecture) => architecture switch
		{
			Architecture.X64 => 1,
			Architecture.X86 => 1,
			Architecture.Arm64 => 12,
			Architecture.Arm32 => 12,
			_ => 255,
		};

		static string NormalizeDebText(string text)
		{
			return string.IsNullOrWhiteSpace(text) ? string.Empty : text.Replace("\r", string.Empty).Replace("\n", " ").Trim();
		}

		static long GetInstallSize(IEnumerable<InstallEntry> entries)
		{
			long result = 0;

			foreach(var entry in entries)
				result += entry.Size;

			return result;
		}

		static int GetFileMode(string path)
		{
			if(!OperatingSystem.IsWindows())
			{
				var mode = (int)(File.GetUnixFileMode(path) & (UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute | UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.GroupExecute | UnixFileMode.OtherRead | UnixFileMode.OtherWrite | UnixFileMode.OtherExecute));

				if(mode > 0)
					return mode;
			}

			return IsExecutable(path) ? 0755 : 0644;
		}

		static bool IsExecutable(string path)
		{
			var extension = Path.GetExtension(path);
			return string.IsNullOrEmpty(extension) ||
				extension.Equals(".sh", StringComparison.OrdinalIgnoreCase) ||
				extension.Equals(".dll", StringComparison.OrdinalIgnoreCase) ||
				extension.Equals(".exe", StringComparison.OrdinalIgnoreCase);
		}

		static long GetUnixTime(DateTime value) => new DateTimeOffset(value).ToUnixTimeSeconds();

		static int IndexOf(IReadOnlyList<string> values, string value)
		{
			if(values == null)
				return -1;

			for(int i = 0; i < values.Count; i++)
			{
				if(string.Equals(values[i], value, StringComparison.Ordinal))
					return i;
			}

			return -1;
		}

		static void Pad(Stream stream, int size)
		{
			while(stream.Position % size != 0)
				stream.WriteByte(0);
		}

		static void Align(Stream stream, int size) => Pad(stream, size);

		static void WriteInt16(Span<byte> destination, int value) => BinaryPrimitives.WriteInt16BigEndian(destination, (short)value);
		static void WriteInt32(Span<byte> destination, int value) => BinaryPrimitives.WriteInt32BigEndian(destination, value);
		#endregion

		#region 嵌套结构
		readonly record struct InstallEntry(string Source, string EntryName, long Size, long ModifiedTime, int Mode);
		readonly record struct InstallMetadata(
			string Name,
			string PackageName,
			string Edition,
			Version Version,
			string Runtime,
			Platform Platform,
			Architecture Architecture,
			string Framework,
			string Title,
			string Summary,
			string Description,
			string InstallRoot);
		readonly record struct InstallScripts(string Installing, string Installed, string Uninstalling, string Uninstalled);
		readonly record struct RpmHeaderIndex(int Tag, int Type, int Offset, int Count);
		readonly record struct RpmEntry(int Inode, long Size, int Mode, long ModifiedTime, string Digest, int DirectoryIndex, string BaseName);
		#endregion
	}

	public enum InstallFormat
	{
		Tarball,
		Deb,
		Rpm,
	}
}

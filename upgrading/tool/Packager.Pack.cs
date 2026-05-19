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
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using Zongsoft.Terminals;
using Zongsoft.Components;

namespace Zongsoft.Upgrading;

partial class Packager
{
	[CommandOption(NAME_OPTION, typeof(string), Required = true)]
	[CommandOption(KIND_OPTION, typeof(string), nameof(ReleaseKind.Fully))]
	[CommandOption(TAGS_OPTION, typeof(string))]
	[CommandOption(TITLE_OPTION, typeof(string))]
	[CommandOption(SOURCE_OPTION, typeof(string))]
	[CommandOption(OUTPUT_OPTION, typeof(string))]
	[CommandOption(EXCLUDE_OPTION, typeof(string))]
	[CommandOption(EDITION_OPTION, typeof(string))]
	[CommandOption(VERSION_OPTION, typeof(Version), Required = true)]
	[CommandOption(CHECKSUM_OPTION, typeof(ChecksumAlgorithm), ChecksumAlgorithm.Sha1)]
	[CommandOption(OVERWRITE_OPTION, typeof(bool), false)]
	[CommandOption(PLATFORM_OPTION, typeof(Platform), Required = true)]
	[CommandOption(FRAMEWORK_OPTION, typeof(string), Required = true)]
	[CommandOption(ARCHITECTURE_OPTION, typeof(Architecture), Architecture.X64)]
	[CommandOption(SUMMARY_OPTION, typeof(string))]
	[CommandOption(DESCRIPTION_OPTION, typeof(string))]
	public sealed class PackCommand : CommandBase<CommandContext>
	{
		#region 常量定义
		private const string NAME_OPTION = "name";
		private const string KIND_OPTION = "kind";
		private const string TAGS_OPTION = "tags";
		private const string TITLE_OPTION = "title";
		private const string SOURCE_OPTION = "source";
		private const string OUTPUT_OPTION = "output";
		private const string EXCLUDE_OPTION = "exclude";
		private const string EDITION_OPTION = "edition";
		private const string VERSION_OPTION = "version";
		private const string CHECKSUM_OPTION = "checksum";
		private const string PLATFORM_OPTION = "platform";
		private const string FRAMEWORK_OPTION = "framework";
		private const string OVERWRITE_OPTION = "overwrite";
		private const string ARCHITECTURE_OPTION = "architecture";

		private const string SUMMARY_OPTION = "summary";
		private const string DESCRIPTION_OPTION = "description";
		#endregion

		#region 执行方法
		protected override async ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
		{
			var name = context.Options.GetValue<string>(NAME_OPTION);
			var edition = context.Options.GetValue<string>(EDITION_OPTION);
			var version = context.Options.GetValue<Version>(VERSION_OPTION);
			var platform = context.Options.GetValue<Platform>(PLATFORM_OPTION);
			var architecture = context.Options.GetValue<Architecture>(ARCHITECTURE_OPTION);

			if(version.IsZero())
			{
				Terminal.WriteLine(CommandOutletColor.Red, $"The version number is invalid.");
				return null;
			}

			//获取当前操作的变量集
			var variables = GetVariables(context);

			//规范化来源路径
			if(!Normalizer.TryNormalize(context.Options.GetValue<string>(SOURCE_OPTION), variables, out var source))
				return null;

			if(string.IsNullOrEmpty(source) || !Path.IsPathFullyQualified(source))
				source = Path.Combine(Environment.CurrentDirectory, source);

			if(!Directory.Exists(source))
			{
				Terminal.WriteLine(CommandOutletColor.Red, $"The source directory '{source}' does not exist.");
				return null;
			}

			//规范化目标路径
			if(!Normalizer.TryNormalize(context.Options.GetValue<string>(OUTPUT_OPTION), variables, out var output))
				return null;

			if(!string.IsNullOrEmpty(output) && IsFile(output, out var filename))
				output += Path.HasExtension(filename) ? null : EXTENSION;
			else
				output = Path.Combine(output, $"{GetFileName(name, edition, version, platform, architecture)}{EXTENSION}");

			if(!Path.IsPathFullyQualified(output))
				output = Path.Combine(source, output);

			//修整路径格式
			source = Path.GetFullPath(source);
			output = Path.GetFullPath(output);

			//将修整后的源目录和输出文件路径添加到变量集
			variables[SOURCE_OPTION] = source;
			variables[OUTPUT_OPTION] = output;

			//生成排除规则集
			var exclusions = PackageExclusions.Parse(source, context.Options.GetValue<string>(EXCLUDE_OPTION, null), variables);
			if(exclusions == null)
				return null;

			//输出打包中的信息
			Terminal.WriteLine(CommandOutletColor.DarkCyan, Properties.Resources.Packing_Message);
			Terminal.WriteLine();

			//如果需要覆盖目标文件则先将其删除
			if(context.Options.Switch(OVERWRITE_OPTION))
			{
				File.Delete(output);
				File.Delete(Path.ChangeExtension(output, Manifest.FILE_NAME));
			}

			//生成发布包文件
			GeneratePackage(source, output, context.Arguments, variables, exclusions);
			//生成发布元文件
			GenerateRelease(name, edition, version, platform, architecture, output, context.Options, variables);

			return output;

			static string GetFileName(string name, string edition, Version version, Platform platform, Architecture architecture) => string.IsNullOrEmpty(edition) ?
				$"{name}@{version}_{Application.GetRuntimeIdentifier(platform, architecture)}" :
				$"{name}({edition})@{version}_{Application.GetRuntimeIdentifier(platform, architecture)}";
		}
		#endregion

		#region 私有方法
		static Dictionary<string, string> GetVariables(CommandContext context)
		{
			//加载应用程序设置
			var variables = Packager.GetVariables();

			//将命令选项添加到变量集
			foreach(var option in context.Options)
			{
				if(option.Value != null)
					variables[option.Key] = option.Value.ToString();
			}

			variables["Runtime"] = Application.GetRuntimeIdentifier(context.Options.GetValue<Platform>(PLATFORM_OPTION), context.Options.GetValue<Architecture>(ARCHITECTURE_OPTION));
			variables["RuntimeIdentifier"] = Application.GetRuntimeIdentifier(context.Options.GetValue<Platform>(PLATFORM_OPTION), context.Options.GetValue<Architecture>(ARCHITECTURE_OPTION));

			return variables;
		}

		static bool IsFile(string path, out string name)
		{
			if(string.IsNullOrEmpty(path))
			{
				name = null;
				return false;
			}

			name = Path.GetFileName(path);
			return !string.IsNullOrEmpty(name) && name != "." && name != "..";
		}

		static void GeneratePackage(string source, string output, IReadOnlyCollection<string> entries, IDictionary<string, string> variables, PackageExclusions exclusions)
		{
			Predicate<string> excluded = path => IsGeneratedFile(path, output) || exclusions != null && exclusions.Contains(path);

			if(entries == null || entries.Count == 0)
			{
				if(exclusions == null || exclusions.IsEmpty)
					ZipFile.CreateFromDirectory(source, output);
				else
				{
					using var package = new Packager(output);
					package.PackDirectory(source, string.Empty, excluded);
				}

				//输出文件生成成功信息
				Terminal.WriteLine(CommandOutletColor.DarkGreen, string.Format(Properties.Resources.PackageGeneratedSuccessfully_Message, output));

				return;
			}

			using var packager = new Packager(output);

			foreach(var entry in entries)
			{
				if(!Normalizer.TryNormalize(entry, variables, out var text))
					continue;

				var index = text.LastIndexOf(':');

				//处理 Windows 平台绝对路径中含盘符的情况
				if(OperatingSystem.IsWindows() && index == 1)
					index = -1;

				var path = index > 0 ? text[..index].Trim() : text;
				var alias = index > 0 ? text[(index + 1)..].Trim() : null;

				//生成打包条目
				GeneratePackageEntry(packager, source, path, alias, excluded);
			}

			//输出文件生成成功信息
			Terminal.WriteLine(CommandOutletColor.DarkGreen, string.Format(Properties.Resources.PackageGeneratedSuccessfully_Message, output));

			static bool IsGeneratedFile(string path, string output)
			{
				if(string.IsNullOrEmpty(path) || string.IsNullOrEmpty(output))
					return false;

				var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
				path = Path.GetFullPath(path);
				output = Path.GetFullPath(output);

				return string.Equals(path, output, comparison) ||
					string.Equals(path, Path.ChangeExtension(output, Manifest.FILE_NAME), comparison);
			}
		}

		static void GeneratePackageEntry(Packager packager, string source, string path, string alias, Predicate<string> excluded)
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

				alias ??= Path.GetRelativePath(source, working);

				if(alias == "." || alias.StartsWith(".."))
					alias = string.Empty;

				foreach(var file in Directory.GetFiles(working, pattern))
					packager.PackFile(file, Path.Combine(alias, Path.GetFileName(file)), excluded);

				foreach(var directory in Directory.GetDirectories(working, pattern))
					packager.PackDirectory(directory, Path.Combine(alias, Path.GetFileName(directory)), excluded);
			}
			else
			{
				alias ??= Path.GetRelativePath(source, path);

				if(alias == "." || alias.StartsWith(".."))
					alias = string.Empty;

				if(File.Exists(path))
					packager.PackFile(path, alias, excluded);
				else if(Directory.Exists(path))
					packager.PackDirectory(path, alias, excluded);
				else
					Terminal.WriteLine(CommandOutletColor.DarkYellow, $"[Warn] The source path '{path}' does not exist.");
			}

			//判断指定的路径是否为绝对路径且位于源目录之外
			static bool IsExternal(string source, string path)
			{
				return Path.IsPathFullyQualified(path) &&
					!Path.GetFullPath(path).StartsWith(source, GetComparison());

				static StringComparison GetComparison() => OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
			}
		}

		static void GenerateRelease(string name, string edition, Version version, Platform platform, Architecture architecture, string filePath, CommandLine.CmdletOptionCollection options, IDictionary<string, string> variables)
		{
			const string EXECUTOR_PREFIX = "executor.";

			var file = new FileInfo(filePath);

			if(!file.Exists)
				return;

			var release = new Release(name, edition, version, platform, architecture)
			{
				Kind = options.GetValue<ReleaseKind>(KIND_OPTION),
				Size = (uint)file.Length,
				Path = file.Name,
			};

			if(options.TryGetValue<string>(TAGS_OPTION, out var tags) && tags != null)
				release.Tags = tags.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

			if(options.TryGetValue<string>(TITLE_OPTION, out var title))
				release.Title = title;

			if(options.TryGetValue<string>(SUMMARY_OPTION, out var summary))
				release.Summary = File.Exists(summary) ? File.ReadAllText(summary) : summary;

			if(options.TryGetValue<string>(DESCRIPTION_OPTION, out var description))
				release.Description = File.Exists(description) ? File.ReadAllText(description) : description;

			//计算安装包文件的校验码
			release.Checksum = Common.Checksum.Compute(options.GetValue<string>(CHECKSUM_OPTION), file.OpenRead());

			foreach(var option in options)
			{
				//如果选项名以“executor.”打头，则表示设置执行器
				if(option.Key.StartsWith(EXECUTOR_PREFIX, StringComparison.OrdinalIgnoreCase) && option.Value != null)
				{
					var cmdlet = option.Key[EXECUTOR_PREFIX.Length..];
					var parts = cmdlet.Split('@');

					if(parts.Length != 2)
					{
						Terminal.WriteLine(CommandOutletColor.DarkYellow, $"[Warn] The specified '{cmdlet}' executor has not defined its event.");
						continue;
					}

					if(Normalizer.TryNormalize(option.Value.ToString(), variables, out var result))
						release.Executors.Add(new(parts[1], $"{parts[0]} {result}"));
				}
			}

			//保存升级发布清单文件
			release.Save(Path.ChangeExtension(filePath, Manifest.FILE_NAME));

			//输出文件生成成功信息
			Terminal.WriteLine(CommandOutletColor.DarkGreen, string.Format(Properties.Resources.ManifestGeneratedSuccessfully_Message, Path.ChangeExtension(filePath, Manifest.FILE_NAME)));
		}
		#endregion

		#region 嵌套子类
		sealed class PackageExclusions
		{
			private readonly List<Entry> _entries = [];
			private readonly StringComparison _comparison;
			private readonly RegexOptions _regexOptions;

			private PackageExclusions()
			{
				_comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
				_regexOptions = OperatingSystem.IsWindows() ? RegexOptions.IgnoreCase | RegexOptions.CultureInvariant : RegexOptions.CultureInvariant;
			}

			public bool IsEmpty => _entries.Count == 0;
			public bool Contains(string path)
			{
				if(string.IsNullOrEmpty(path))
					return false;

				path = NormalizePath(Path.GetFullPath(path));

				foreach(var entry in _entries)
				{
					if(entry.Expression != null)
					{
						if(entry.Expression.IsMatch(path))
							return true;
					}
					else if(entry.Directory)
					{
						if(string.Equals(path, entry.Path, _comparison) || path.StartsWith(entry.Path + '/', _comparison))
							return true;
					}
					else if(string.Equals(path, entry.Path, _comparison))
						return true;
				}

				return false;
			}

			public static PackageExclusions Parse(string source, string text, IDictionary<string, string> variables)
			{
				var exclusions = new PackageExclusions();

				if(string.IsNullOrWhiteSpace(text))
					return exclusions;

				foreach(var part in text.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
				{
					if(!Normalizer.TryNormalize(part, variables, out var pattern))
						return null;

					if(!string.IsNullOrWhiteSpace(pattern))
						exclusions.Add(source, pattern.Trim());
				}

				return exclusions;
			}

			private void Add(string source, string pattern)
			{
				pattern = NormalizeSeparators(pattern);
				var wildcards = ContainsWildcard(pattern);
				var directory = EndsWithDirectorySeparator(pattern);
				var path = Path.IsPathFullyQualified(pattern) ? pattern : Path.Combine(source, pattern);

				if(!wildcards)
				{
					path = Path.GetFullPath(path);
					_entries.Add(new Entry(NormalizePath(path), directory || Directory.Exists(path)));
					return;
				}

				_entries.Add(new Entry(CreateExpression(source, pattern)));
			}

			private Regex CreateExpression(string source, string pattern)
			{
				var expression = new System.Text.StringBuilder();
				expression.Append('^');

				if(!Path.IsPathFullyQualified(pattern) && !ContainsDirectorySeparator(pattern))
				{
					expression.Append(Regex.Escape(NormalizePath(Path.GetFullPath(source))));
					expression.Append("(?:/.*/|/)");
					expression.Append(GetWildcardExpression(NormalizeSeparators(pattern)));
				}
				else
				{
					var path = Path.IsPathFullyQualified(pattern) ? pattern : Path.Combine(source, pattern);
					expression.Append(GetWildcardExpression(NormalizePath(Path.GetFullPath(path))));
				}

				expression.Append("(?:/.*)?$");
				return new Regex(expression.ToString(), _regexOptions);
			}

			private static bool ContainsWildcard(string text) => text != null && (text.Contains('*') || text.Contains('?'));
			private static bool ContainsDirectorySeparator(string text) => text != null && (text.Contains('/') || text.Contains('\\'));
			private static bool EndsWithDirectorySeparator(string text) => text != null && (text.EndsWith('/') || text.EndsWith('\\'));
			private static string NormalizeSeparators(string text) => text.Replace('\\', '/');

			private static string NormalizePath(string path)
			{
				if(string.IsNullOrEmpty(path))
					return path;

				var normalized = NormalizeSeparators(path);
				var root = Path.GetPathRoot(path);
				var rootLength = string.IsNullOrEmpty(root) ? 0 : NormalizeSeparators(root).Length;

				while(normalized.Length > rootLength && normalized[^1] == '/')
					normalized = normalized[..^1];

				return normalized;
			}

			private static string GetWildcardExpression(string pattern)
			{
				var expression = new System.Text.StringBuilder();

				foreach(var character in pattern)
				{
					switch(character)
					{
						case '*':
							expression.Append("[^/]*");
							break;
						case '?':
							expression.Append("[^/]");
							break;
						case '/':
							expression.Append('/');
							break;
						default:
							expression.Append(Regex.Escape(character.ToString()));
							break;
					}
				}

				return expression.ToString();
			}

			private readonly struct Entry
			{
				public Entry(string path, bool directory = false)
				{
					this.Path = path;
					this.Directory = directory;
					this.Expression = null;
				}

				public Entry(Regex expression)
				{
					this.Path = null;
					this.Directory = false;
					this.Expression = expression;
				}

				public readonly string Path;
				public readonly bool Directory;
				public readonly Regex Expression;
			}
		}
		#endregion
	}
}

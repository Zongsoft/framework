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
	[CommandOption(EDITION_OPTION, typeof(string))]
	[CommandOption(VERSION_OPTION, typeof(Version), Required = true)]
	[CommandOption(CHECKSUM_OPTION, typeof(string))]
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
			if(!Normalizer.Normalize(context.Options.GetValue<string>(SOURCE_OPTION), variables, out var source))
				return null;

			if(string.IsNullOrEmpty(source) || !Path.IsPathFullyQualified(source))
				source = Path.Combine(Environment.CurrentDirectory, source);

			if(!Directory.Exists(source))
			{
				Terminal.WriteLine(CommandOutletColor.Red, $"The source directory '{source}' does not exist.");
				return null;
			}

			//规范化目标路径
			if(!Normalizer.Normalize(context.Options.GetValue<string>(OUTPUT_OPTION), variables, out var output))
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

			//如果需要覆盖目标文件则先将其删除
			if(context.Options.Switch(OVERWRITE_OPTION))
			{
				File.Delete(output);
				File.Delete(Path.ChangeExtension(output, Manifest.FILE_NAME));
			}

			//生成发布包文件
			GeneratePackage(source, output, context.Arguments, variables);
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

		static void GeneratePackage(string source, string output, IReadOnlyCollection<string> entries, IDictionary<string, string> variables)
		{
			if(entries == null || entries.Count == 0)
			{
				ZipFile.CreateFromDirectory(source, output);

				//输出文件生成成功信息
				Terminal.WriteLine(CommandOutletColor.DarkGreen, string.Format(Properties.Resources.PackageGeneratedSuccessfully_Message, output));

				return;
			}

			using var zip = ZipFile.Open(output, ZipArchiveMode.Create);

			foreach(var entry in entries)
			{
				if(!Normalizer.Normalize(entry, variables, out var text))
					continue;

				var index = text.IndexOf(':');
				var path = index > 0 ? text[..index] : text;

				//将相对路径改为绝对路径
				if(!Path.IsPathFullyQualified(path))
					path = Path.Combine(source, path);

				if(path.Contains('*') || path.Contains('?'))
				{
					var section = GetDirectoryEntryName(path, text);
					var working = Path.GetDirectoryName(path);
					var pattern = Path.GetFileName(path);

					foreach(var file in Directory.GetFiles(working, pattern))
						zip.CreateEntryFromFile(file, Path.Combine(section, Path.GetFileName(file)));

					foreach(var directory in Directory.GetDirectories(working, pattern))
						zip.CreateEntryFromDirectory(directory, Path.Combine(section, Path.GetFileName(directory)));
				}
				else
				{
					if(File.Exists(path))
						zip.CreateEntryFromFile(path, GetFileEntryName(path, text));
					else if(Directory.Exists(path))
						zip.CreateEntryFromDirectory(path, GetDirectoryEntryName(path, text));
					else
						Terminal.WriteLine(CommandOutletColor.DarkYellow, $"[Warn] The source path '{path}' does not exist.");
				}
			}

			//输出文件生成成功信息
			Terminal.WriteLine(CommandOutletColor.DarkGreen, string.Format(Properties.Resources.PackageGeneratedSuccessfully_Message, output));

			static string GetFileEntryName(string path, ReadOnlySpan<char> entry)
			{
				if(entry.IsEmpty || entry.IsWhiteSpace())
					return Path.GetFileName(path);

				var index = entry.IndexOf(':');

				if(index > 0)
				{
					var alias = entry[(index + 1)..]
						.Trim()
						.TrimStart('~');

					if(alias.IsEmpty)
						return Path.GetFileName(path);

					var filename = Path.GetFileName(alias);

					return filename.IsEmpty || filename == "." ?
						Path.Combine(alias.ToString(), Path.GetFileName(path)) :
						alias.ToString();
				}

				return entry
					.Trim(Path.DirectorySeparatorChar)
					.Trim(Path.AltDirectorySeparatorChar)
					.ToString();
			}

			static string GetDirectoryEntryName(string path, ReadOnlySpan<char> entry)
			{
				if(entry.IsEmpty || entry.IsWhiteSpace())
					return Path.GetFileName(path);

				var index = entry.IndexOf(':');

				if(index > 0)
				{
					var alias = entry[(index + 1)..]
						.Trim()
						.TrimStart('~')
						.TrimEnd(Path.DirectorySeparatorChar)
						.TrimEnd(Path.AltDirectorySeparatorChar);

					return alias.ToString();
				}

				return entry
					.Trim(Path.DirectorySeparatorChar)
					.Trim(Path.AltDirectorySeparatorChar)
					.ToString();
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

					if(Normalizer.Normalize(option.Value.ToString(), variables, out var result))
						release.Executors.Add(new(parts[1], $"{parts[0]} {result}"));
				}
			}

			//保存升级发布清单文件
			release.Save(Path.ChangeExtension(filePath, Manifest.FILE_NAME));

			//输出文件生成成功信息
			Terminal.WriteLine(CommandOutletColor.DarkGreen, string.Format(Properties.Resources.ManifestGeneratedSuccessfully_Message, Path.ChangeExtension(filePath, Manifest.FILE_NAME)));
		}
		#endregion
	}
}

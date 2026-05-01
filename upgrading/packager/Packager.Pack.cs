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
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Terminals;
using Zongsoft.Components;

namespace Zongsoft.Upgrading;

partial class Packager
{
	public void Pack()
	{
	}

	[CommandOption(NAME_OPTION, typeof(string), Required = true)]
	[CommandOption(KIND_OPTION, typeof(string), nameof(ReleaseKind.Fully))]
	[CommandOption(TAGS_OPTION, typeof(string))]
	[CommandOption(SOURCE_OPTION, typeof(string))]
	[CommandOption(OUTPUT_OPTION, typeof(string))]
	[CommandOption(EDITION_OPTION, typeof(string))]
	[CommandOption(VERSION_OPTION, typeof(Version), Required = true)]
	[CommandOption(CHECKSUM_OPTION, typeof(string))]
	[CommandOption(PLATFORM_OPTION, typeof(Platform), Required = true)]
	[CommandOption(FRAMEWORK_OPTION, typeof(string), Required = true)]
	[CommandOption(ARCHITECTURE_OPTION, typeof(Architecture), Architecture.X64)]
	public sealed class PackCommand : CommandBase<CommandContext>
	{
		private const string NAME_OPTION = "name";
		private const string KIND_OPTION = "kind";
		private const string TAGS_OPTION = "tags";
		private const string SOURCE_OPTION = "source";
		private const string OUTPUT_OPTION = "output";
		private const string EDITION_OPTION = "edition";
		private const string VERSION_OPTION = "version";
		private const string CHECKSUM_OPTION = "checksum";
		private const string PLATFORM_OPTION = "platform";
		private const string FRAMEWORK_OPTION = "framework";
		private const string ARCHITECTURE_OPTION = "architecture";

		protected override async ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
		{
			var name = context.Options.GetValue<string>(NAME_OPTION);
			var kind = context.Options.GetValue<ReleaseKind>(KIND_OPTION);
			var edition = context.Options.GetValue<string>(EDITION_OPTION);
			var version = context.Options.GetValue<Version>(VERSION_OPTION);
			var platform = context.Options.GetValue<Platform>(PLATFORM_OPTION);
			var architecture = context.Options.GetValue<Architecture>(ARCHITECTURE_OPTION);

			var source = context.Options.GetValue<string>(SOURCE_OPTION);
			if(string.IsNullOrEmpty(source) || !Path.IsPathFullyQualified(source))
				source = Path.Combine(Environment.CurrentDirectory, source);

			if(!Directory.Exists(source))
			{
				Terminal.WriteLine(CommandOutletColor.Red, $"The source path '{source}' does not exist.");
				return null;
			}

			var output = context.Options.GetValue<string>(OUTPUT_OPTION);
			if(IsFile(output, out var filename))
				output += Path.HasExtension(filename) ? null : ".zip";
			else
				output = Path.Combine(output, $"{GetFileName(name, edition, version, platform, architecture)}.zip");

			if(!Path.IsPathFullyQualified(output))
				output = Path.Combine(Environment.CurrentDirectory, output);

			if(context.Arguments.IsEmpty)
			{
				System.IO.Compression.ZipFile.CreateFromDirectory(source, output);
				return output;
			}

			return null;

			static string GetFileName(string name, string edition, Version version, Platform platform, Architecture architecture) => string.IsNullOrEmpty(edition) ?
				$"{name}@{version}_{Application.GetRuntimeIdentifier(platform, architecture)}" :
				$"{name}({edition})@{version}_{Application.GetRuntimeIdentifier(platform, architecture)}";
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
	}
}

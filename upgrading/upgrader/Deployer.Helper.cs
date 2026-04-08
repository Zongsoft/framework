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
 * Copyright (C) 2020-2026 Zongsoft Corporation <http://zongsoft.com>
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

namespace Zongsoft.Upgrading;

partial class Deployer
{
	internal static class Helper
	{
		public static bool Clean(string root, Argument argument)
		{
			if(string.IsNullOrEmpty(root) || !Directory.Exists(root))
				return true;

			//获取部署器程序所在的目录
			var directory = Path.GetDirectoryName(Environment.ProcessPath);
			//定义路径匹配的文本比较方式
			var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

			try
			{
				var directories = Directory.GetDirectories(root);
				for(int i = 0; i < directories.Length; i++)
				{
					if(directories[i].StartsWith(directory, comparison))
						continue;

					Directory.Delete(directories[i], true);
				}

				var files = Directory.GetFiles(root);
				for(int i = 0; i < files.Length; i++)
				{
					if(files[i].StartsWith(directory, comparison))
						continue;

					if(string.Equals(files[i], argument.Deployment, comparison))
						continue;

					File.Delete(files[i]);
				}

				return true;
			}
			catch(Exception ex)
			{
				Zongsoft.Diagnostics.Logging.GetLogging<Program>().Error(ex);
				return false;
			}
		}

		public static void Replicate(DirectoryInfo source, string destination)
		{
			if(string.IsNullOrEmpty(destination) || !Directory.Exists(destination) || !source.Exists)
				return;

			var files = source.GetFiles();
			for(int i = 0; i < files.Length; i++)
			{
				files[i].CopyTo(destination, true);
			}

			var directories = source.GetDirectories();
			for(int i = 0; i < directories.Length; i++)
			{
				var directory = EnsureDirectory(Path.Combine(destination, directories[i].Name));
				Replicate(directories[i], directory.FullName);
			}

			static DirectoryInfo EnsureDirectory(string path)
			{
				var directory = new DirectoryInfo(path);
				return directory.Exists ? directory : Directory.CreateDirectory(path);
			}
		}
	}
}

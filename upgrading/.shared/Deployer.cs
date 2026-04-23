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
using System.Reflection;

namespace Zongsoft.Upgrading;

/// <summary>提供升级部署功能的类，即部署器。</summary>
public static partial class Deployer
{
	#region 公共字段
	/// <summary>表示部署器程序的目录名。</summary>
	public static readonly string DIRECTORY = ".deployer";
	/// <summary>表示部署器程序的文件名。</summary>
	public static readonly string FILENAME = OperatingSystem.IsWindows() ? $"Zongsoft.Upgrading.Deployer.exe" : "Zongsoft.Upgrading.Deployer";
	#endregion

	#region 公共属性
	/// <summary>获取部署器程序的名称。</summary>
	public static string Name => field ??= Assembly.GetEntryAssembly()?.GetName().Name;
	/// <summary>获取部署器程序的版本号。</summary>
	public static Version Version => field ??= Assembly.GetEntryAssembly().GetName().Version;
	#endregion

	#region 公共方法
	public static bool HasDeployment() => HasDeployment(out _);
	public static bool HasDeployment(out FileInfo file)
	{
		file = new FileInfo(Path.Combine(Application.ApplicationPath, Deployment.FILE_NAME));

		if(file.Exists)
		{
			using var deployment = Deployment.Load(file.FullName, false);

			if(deployment != null)
			{
				var identifier = Services.ApplicationIdentifier.Load(deployment.Packages);

				return identifier.Version != null &&
					identifier.Version > Application.ApplicationVersion &&
					string.Equals(Application.ApplicationName, identifier.Name, StringComparison.OrdinalIgnoreCase);
			}

			Zongsoft.Diagnostics.Logging.GetLogging().Warn($"The specified '{file.FullName}' deployment file is invalid. The deployment process cannot proceed.");
		}

		file = null;
		return false;
	}
	#endregion

	/// <summary>提供部署器相关配置功能的类。</summary>
	public sealed class Deployment : IDisposable
	{
		#region 常量定义
		internal const string FILE_NAME = ".deployment";
		#endregion

		#region 私有变量
		private FileStream _stream;
		#endregion

		#region 私有构造
		private Deployment(FileStream stream, string manifest, string packages)
		{
			_stream = stream;
			this.Manifest = manifest;
			this.Packages = packages;
		}
		#endregion

		#region 公共属性
		/// <summary>获取发布清单文件路径。</summary>
		public string Manifest { get; }
		/// <summary>获取部署包目录的路径。</summary>
		public string Packages { get; }
		#endregion

		#region 处置方法
		public void Dispose()
		{
			_stream?.Dispose();
			_stream = null;
		}
		#endregion

		#region 静态方法
		public static Deployment Load(string path, bool exclusive)
		{
			if(string.IsNullOrEmpty(path) || !File.Exists(path))
				return null;

			var count = 0;
			string text, manifest = null, packages = null;

			var stream = exclusive ?
				new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None, 1024, FileOptions.DeleteOnClose) :
				new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 1024);

			var reader = new StreamReader(stream, leaveOpen: exclusive);

			try
			{
				while((text = reader.ReadLine()) != null)
				{
					//跳过空行
					if(string.IsNullOrWhiteSpace(text))
						continue;

					var index = text.IndexOf('=');

					if(index > 0)
					{
						var name = text[..index];

						if(string.Equals(name, nameof(Manifest), StringComparison.OrdinalIgnoreCase))
							manifest = text[(index + 1)..];
						else if(string.Equals(name, nameof(Packages), StringComparison.OrdinalIgnoreCase))
							packages = text[(index + 1)..];
					}

					//如果信息读取完成则退出读取循环
					if(!string.IsNullOrEmpty(manifest) && !string.IsNullOrEmpty(packages))
						break;

					//如果超过指定行数则退出读取循环
					if(++count > 10)
						break;
				}
			}
			finally
			{
				reader.Dispose();

				if(!exclusive)
					stream.Dispose();
			}

			return string.IsNullOrEmpty(manifest) || string.IsNullOrEmpty(packages) ? null : new Deployment(exclusive ? stream : null, manifest, packages);
		}

		public static string Save(string manifest, string packages, string directory = null)
		{
			if(string.IsNullOrEmpty(directory))
				directory = Application.ApplicationPath;

			//定义部署文件路径
			var path = Path.Combine(Path.Combine(directory, FILE_NAME));

			using var stream = File.OpenWrite(path);
			using var writer = new StreamWriter(stream);
			writer.WriteLine($"{nameof(Manifest)}={manifest}");
			writer.WriteLine($"{nameof(Packages)}={packages}");
			writer.Close();
			stream.Close();

			//返回部署文件路径
			return path;
		}
		#endregion
	}
}

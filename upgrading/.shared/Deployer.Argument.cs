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
using System.Collections;
using System.Collections.Generic;

namespace Zongsoft.Upgrading;

partial class Deployer
{
	/// <summary>表示部署器的参数类。</summary>
	/// <param name="parameters">命令行参数集的字典。</param>
	public sealed class Argument(Dictionary<string, string> parameters) : IReadOnlyCollection<KeyValuePair<string, string>>
	{
		#region 成员字段
		private readonly Dictionary<string, string> _parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
		#endregion

		#region 公共属性
		/// <summary>获取宿主程序的站点标识。</summary>
		public string Site => this.TryGetValue(Keys.Site, out var value) ? value : null;
		/// <summary>获取宿主程序的进程编号。</summary>
		public int AppId => this.TryGetInt32(Keys.AppId, out var value) ? value : 0;
		/// <summary>获取宿主程序的应用名称。</summary>
		public string AppName => this.TryGetValue(Keys.AppName, out var value) ? value : null;
		/// <summary>获取宿主程序的应用类型。</summary>
		public string AppType => this.TryGetValue(Keys.AppType, out var value) ? value : null;
		/// <summary>获取宿主程序的完整路径。</summary>
		public string AppPath => this.TryGetValue(Keys.AppPath, out var value) ? value : null;
		/// <summary>获取宿主程序的命令行参数集。</summary>
		public string[] AppArgs => field ??= this.GetAppArgs() ?? [];
		/// <summary>获取部署器的部署文件完整路径。</summary>
		public string Deployment => this.TryGetValue(Keys.Deployment, out var value) ? value : null;
		#endregion

		#region 公共方法
		public bool Contains(string name) => name != null && _parameters.ContainsKey(name);
		public bool TryGetValue(string name, out string value) => _parameters.TryGetValue(name, out value);
		public bool TryGetInt16(string name, out short value)
		{
			if(_parameters.TryGetValue(name, out var text) && text != null)
				return short.TryParse(text, out value);

			value = 0;
			return false;
		}
		public bool TryGetInt32(string name, out int value)
		{
			if(_parameters.TryGetValue(name, out var text) && text != null)
				return int.TryParse(text, out value);

			value = 0;
			return false;
		}
		public bool TryGetInt64(string name, out long value)
		{
			if(_parameters.TryGetValue(name, out var text) && text != null)
				return long.TryParse(text, out value);

			value = 0;
			return false;
		}
		#endregion

		#region 私有方法
		private string[] GetAppArgs()
		{
			var args = new List<string>();

			foreach(var entry in _parameters)
			{
				if(entry.Key.StartsWith($"{Keys.AppArgs}#") || entry.Key.StartsWith($"{Keys.AppArgs}:"))
					args.Add(entry.Value);
			}

			return [.. args];
		}
		#endregion

		#region 显式实现
		int IReadOnlyCollection<KeyValuePair<string, string>>.Count => _parameters.Count;
		IEnumerator IEnumerable.GetEnumerator() => _parameters.GetEnumerator();
		public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => _parameters.GetEnumerator();
		#endregion

		#region 静态方法
		/// <summary>根据命令行参数数组构建部署器参数对象。</summary>
		/// <param name="args">指定的部署器命令行参数数组。</param>
		/// <returns>如果指定的 <paramref name="args"/> 命令行参数数组不为空或空集则返回创建的参数对象，否则返回空(<c>null</c>)。</returns>
		public static Argument Create(string[] args)
		{
			if(args == null || args.Length == 0)
				return null;

			var parameters = new Dictionary<string, string>(args.Length, StringComparer.OrdinalIgnoreCase);

			for(int i = 0; i < args.Length; i++)
			{
				var arg = args[i].AsSpan();
				if(arg.IsEmpty)
					continue;

				if(arg.StartsWith("--"))
					arg = arg[2..];
				else if(arg[0] == '-' || arg[0] == '/')
					arg = arg[1..];

				var index = arg.IndexOf('=');

				if(index > 0)
					parameters.Add(arg[..index].Trim().ToString(), arg[(index + 1)..].Trim().ToString());
				else
					parameters.Add(arg.Trim().ToString(), null);
			}

			return parameters.Count > 0 ? new(parameters) : null;
		}
		#endregion

		#region 嵌套子类
		internal sealed class Keys
		{
			public const string Site = "site";
			public const string AppId = "app.id";
			public const string AppName = "app.name";
			public const string AppType = "app.type";
			public const string AppPath = "app.path";
			public const string AppArgs = "app.args";
			public const string Deployment = "deployment";
		}
		#endregion
	}
}

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
using System.Collections;
using System.Collections.Generic;

namespace Zongsoft.Upgrading;

public partial class Packager : IDisposable
{
	#region 常量定义
	internal const string EXTENSION = ".zip";
	#endregion

	#region 成员字段
	private ZipArchive _archive;
	private Dictionary<string, string> _entries;
	#endregion

	#region 构造函数
	public Packager(string path)
	{
		_archive = ZipFile.Open(path, ZipArchiveMode.Create);
		_entries = new(StringComparer.OrdinalIgnoreCase);
	}
	#endregion

	#region 打包方法
	internal void PackFile(string source, string entryName, Predicate<string> excluded = null)
	{
		ArgumentException.ThrowIfNullOrEmpty(source);

		if(excluded != null && excluded(source))
			return;

		if(string.IsNullOrEmpty(entryName))
			entryName = Path.GetFileName(source);
		else
		{
			var filename = Path.GetFileName(entryName);

			if(string.IsNullOrEmpty(filename) || filename == ".")
				entryName = Path.Combine(Path.GetDirectoryName(entryName), Path.GetFileName(source));
			else
				entryName = entryName
					.Trim(Path.DirectorySeparatorChar)
					.Trim(Path.AltDirectorySeparatorChar);
		}

		if(_entries.TryGetValue(entryName, out var entry))
		{
			Terminals.Terminal.WriteLine(Components.CommandOutletColor.Cyan, string.Format(Properties.Resources.PackingConflict_Message, source, entry, entryName));
			Terminals.Terminal.Write(Components.CommandOutletColor.DarkYellow, Properties.Resources.Tip_Label);
			Terminals.Terminal.Write(Components.CommandOutletColor.DarkGray, string.Format(Properties.Resources.PackingConflict_Tip, source, entry));
			Terminals.Terminal.WriteLine(Environment.NewLine);

			return;
		}

		_archive.CreateEntryFromFile(source, entryName);
		_entries[entryName] = source;
	}

	internal void PackDirectory(string source, string entryName, Predicate<string> excluded = null)
	{
		if(excluded != null && excluded(source))
			return;

		foreach(var file in Directory.GetFiles(source))
			this.PackFile(file, Path.Combine(entryName, Path.GetFileName(file)), excluded);
		foreach(var directory in Directory.GetDirectories(source))
			this.PackDirectory(directory, Path.Combine(entryName, Path.GetFileName(directory)), excluded);
	}
	#endregion

	#region 释放方法
	public void Dispose()
	{
		this.Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if(disposing)
		{
			_archive?.Dispose();
			_entries.Clear();
		}

		_archive = null;
		_entries = null;
	}
	#endregion

	#region 静态方法
	public static Dictionary<string, string> GetVariables(params IEnumerable<KeyValuePair<string, string>> variables)
	{
		var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		//加载系统环境变量
		foreach(DictionaryEntry variable in Environment.GetEnvironmentVariables())
			result[variable.Key.ToString()] = variable.Value?.ToString();

		if(variables != null)
		{
			foreach(var variable in variables)
				result[variable.Key] = variable.Value;
		}

		return result;
	}
	#endregion
}

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
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Data library.
 *
 * The Zongsoft.Data is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.IO;
using System.Collections.Concurrent;

namespace Zongsoft.Data.Metadata.Profiles;

public class MetadataCommandScriptor : IDataCommandScriptor
{
	#region 静态字段
	private static readonly ConcurrentDictionary<ScriptKey, string> _scripts = new();
	#endregion

	#region 成员字段
	private readonly MetadataCommand _command;
	#endregion

	#region 构造函数
	public MetadataCommandScriptor(MetadataCommand command)
	{
		_command = command ?? throw new ArgumentNullException(nameof(command));
	}
	#endregion

	#region 公共方法
	public void Load(string directory, string provider)
	{
		if(string.IsNullOrEmpty(directory))
			return;

		var name = _command.Name;

		if(string.IsNullOrEmpty(provider))
		{
			var files = Directory.GetFiles(directory, $"{name}-*.sql", SearchOption.AllDirectories);
			for(int i = 0; i < files.Length; i++)
				LoadFile(files[i], name);
		}
		else
		{
			var files = Directory.GetFiles(directory, $"{provider}.{name}-*.sql", SearchOption.AllDirectories);
			for(int i = 0; i < files.Length; i++)
				LoadFile(files[i], name);

			files = Directory.GetFiles(directory, $"{name}-*.sql", SearchOption.AllDirectories);
			for(int i = 0; i < files.Length; i++)
				LoadFile(files[i], name);
		}

		static bool LoadFile(string filePath, string commandName)
		{
			var fileName = Path.GetFileNameWithoutExtension(filePath);
			var index = fileName.LastIndexOf('-');

			if(index > 0 && index < fileName.Length - 1)
			{
				var driver = fileName.Substring(index + 1);

				if(!_scripts.ContainsKey(new ScriptKey(commandName, driver)))
					return TrySetScript(commandName, driver, File.ReadAllText(filePath));
			}
			else
			{
				if(!_scripts.ContainsKey(new ScriptKey(commandName)))
					return TrySetScript(commandName, null, File.ReadAllText(filePath));
			}

			return false;
		}
	}

	public string GetScript(string driver)
	{
		return _scripts.TryGetValue(new ScriptKey(_command.Name, driver), out var script) ? script : null;
	}

	public void SetScript(string driver, string text)
	{
		if(string.IsNullOrWhiteSpace(text))
			return;

		_scripts[new ScriptKey(_command.Name, driver)] = text.Trim();
	}
	#endregion

	#region 私有方法
	private static bool TrySetScript(string name, string driver, string text)
	{
		if(string.IsNullOrWhiteSpace(text))
			return false;

		return _scripts.TryAdd(new ScriptKey(name, driver), text.Trim());
	}
	#endregion

	#region 嵌套结构
	private readonly struct ScriptKey : IEquatable<ScriptKey>
	{
		#region 常量定义
		private const string DEFAULT_DRIVER = "*";
		#endregion

		#region 构造函数
		public ScriptKey(string name, string driver = null)
		{
			this.Name = name.ToLowerInvariant();
			this.Driver = string.IsNullOrEmpty(driver) ? DEFAULT_DRIVER : driver.ToLowerInvariant();
		}
		#endregion

		#region 公共字段
		public readonly string Name;
		public readonly string Driver;
		#endregion

		#region 重写方法
		public bool Equals(ScriptKey other) => string.Equals(this.Name, other.Name) && string.Equals(this.Driver, other.Driver);
		public override bool Equals(object obj) => obj is ScriptKey other && this.Equals(other);
		public override int GetHashCode() => HashCode.Combine(this.Name, this.Driver);
		public override string ToString() => string.IsNullOrEmpty(this.Driver) || this.Driver == DEFAULT_DRIVER ? this.Name : $"{this.Driver}:{this.Name}";
		#endregion
	}
	#endregion
}

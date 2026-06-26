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
 * Copyright (C) 2025-2026 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Learning library.
 *
 * The Zongsoft.Learning is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Learning is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Learning library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Reflection;
using System.ComponentModel;

using Microsoft.ML;
using Microsoft.ML.Data;

using Zongsoft.Configuration;

namespace Zongsoft.Learning.Data;

public class TextFileLoaderSettings : ConnectionSettingsBase<TextFileLoaderSettingsDriver, TextLoader.Options>
{
	#region 构造函数
	public TextFileLoaderSettings(TextFileLoaderSettingsDriver driver, string settings) : base(driver, settings) { }
	public TextFileLoaderSettings(TextFileLoaderSettingsDriver driver, string name, string settings) : base(driver, name, settings) { }
	#endregion

	#region 公共属性
	public bool AllowQuoting
	{
		get => this.GetValue<bool>();
		set => this.SetValue(value);
	}

	public bool AllowSparse
	{
		get => this.GetValue<bool>();
		set => this.SetValue(value);
	}

	public bool TrimWhitespace
	{
		get => this.GetValue<bool>();
		set => this.SetValue(value);
	}

	public bool HasHeader
	{
		get => this.GetValue<bool>();
		set => this.SetValue(value);
	}

	public bool UseThreads
	{
		get => this.GetValue<bool>();
		set => this.SetValue(value);
	}

	public bool ReadMultilines
	{
		get => this.GetValue<bool>();
		set => this.SetValue(value);
	}

	public bool MissingRealsAsNaNs
	{
		get => this.GetValue<bool>();
		set => this.SetValue(value);
	}

	public int InputSize
	{
		get => this.GetValue<int>();
		set => this.SetValue(value);
	}

	public long MaxRows
	{
		get => this.GetValue<long>();
		set => this.SetValue(value);
	}

	public string[] Separators
	{
		get => this.GetValue<string[]>();
		set => this.SetValue(value);
	}

	[DefaultValue('"')]
	public char EscapeChar
	{
		get => this.GetValue<char>();
		set => this.SetValue(value);
	}

	[DefaultValue('.')]
	public char DecimalMarker
	{
		get => this.GetValue<char>();
		set => this.SetValue(value);
	}

	[ConnectionSetting(true, Ignored = true)]
	public string FilePath
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	public string HeaderFile
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}
	#endregion

	#region 重写方法
	protected override void Populate(ref TextLoader.Options options, ConnectionSettingDescriptor descriptor, PropertyInfo property, object value)
	{
		if(descriptor.Name.Equals(nameof(this.InputSize), StringComparison.OrdinalIgnoreCase))
		{
			if(value == null || (int)Convert.ChangeType(value, typeof(int)) <= 0)
				return;
		}

		if(descriptor.Name.Equals(nameof(this.MaxRows), StringComparison.OrdinalIgnoreCase))
		{
			if(value == null || (long)Convert.ChangeType(value, typeof(long)) <= 0)
				return;
		}

		if(descriptor.Name.Equals(nameof(this.Separators), StringComparison.OrdinalIgnoreCase))
		{
			if(value == null || (value is string[] array && array.Length == 0))
				return;
		}

		base.Populate(ref options, descriptor, property, value);
	}
	#endregion
}

public class TextFileLoaderSettingsDriver : ConnectionSettingsDriver<TextFileLoaderSettings>
{
	#region 常量定义
	internal const string NAME = "ML.TextFile";
	#endregion

	#region 单例字段
	public static readonly TextFileLoaderSettingsDriver Instance = new();
	#endregion

	#region 私有构造
	private TextFileLoaderSettingsDriver() : base(NAME) { }
	#endregion
}
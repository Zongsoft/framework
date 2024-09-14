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
 * Copyright (C) 2010-2024 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Core library.
 *
 * The Zongsoft.Core is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Core is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Core library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;

namespace Zongsoft.Configuration
{
	/// <summary>
	/// 表示连接设置驱动器的接口。
	/// </summary>
	public interface IConnectionSettingsDriver : IEquatable<IConnectionSettingsDriver>
	{
		#region 静态属性
		/// <summary>获取连接设置项描述器集合。</summary>
		static ConnectionSettingDescriptorCollection Descriptors { get; }
		#endregion

		#region 实例属性
		/// <summary>获取驱动名称。</summary>
		string Name { get; }
		/// <summary>获取或设置驱动的描述信息。</summary>
		string Description { get; set; }
		/// <summary>获取连接设置映射器。</summary>
		IConnectionSettingsMapper Mapper { get; }
		/// <summary>获取连接设置模型器。</summary>
		IConnectionSettingsModeler Modeler { get; }
		#endregion

		#region 方法实现
		bool IsDriver(string name)
		{
			if(string.IsNullOrEmpty(name))
				return string.IsNullOrEmpty(this.Name);

			return string.Equals(this.Name, name, StringComparison.OrdinalIgnoreCase) || this.Name.StartsWith($".{name}", StringComparison.OrdinalIgnoreCase);
		}
		#endregion
	}

	/// <summary>
	/// 表示连接设置驱动器的接口。
	/// </summary>
	public interface IConnectionSettingsDriver<out TDescriptors> : IConnectionSettingsDriver where TDescriptors : ConnectionSettingDescriptorCollection, new()
	{
		#region 静态属性
		/// <summary>获取连接设置项描述器集合。</summary>
		static new TDescriptors Descriptors { get; }
		#endregion
	}
}

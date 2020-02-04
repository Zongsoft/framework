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
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Plugins library.
 *
 * The Zongsoft.Plugins is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Plugins is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Plugins library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;

namespace Zongsoft.Plugins.Builders
{
	/// <summary>
	/// 表示构建设置的类。
	/// </summary>
	public class BuilderSettings
	{
		#region 成员字段
		private BuilderSettingsFlags _flags;
		#endregion

		#region 构造函数
		public BuilderSettings(Type targetType, Action<BuilderContext> builded = null)
		{
			this.TargetType = targetType;
			this.Builded = builded;
		}
		#endregion

		#region 公共属性
		/// <summary>
		/// 获取或设置构建的结果类型。
		/// </summary>
		public Type TargetType
		{
			get; set;
		}

		/// <summary>
		/// 获取或设置构建行为的标记。
		/// </summary>
		public BuilderSettingsFlags Flags
		{
			get => _flags;
			set => _flags = value;
		}

		/// <summary>
		/// 获取或设置构建完成的回调方法。
		/// </summary>
		public Action<BuilderContext> Builded
		{
			get; set;
		}
		#endregion

		#region 公共方法
		public void SetFlags(BuilderSettingsFlags flags)
		{
			_flags |= flags;
		}

		public bool HasFlags(BuilderSettingsFlags flags)
		{
			return (_flags & flags) == flags;
		}
		#endregion

		#region 静态方法
		public static BuilderSettings Create(BuilderSettingsFlags flags)
		{
			return new BuilderSettings(null) { Flags = flags };
		}
		#endregion
	}
}

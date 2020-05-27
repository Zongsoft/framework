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
 * This file is part of Zongsoft.Externals.Aliyun library.
 *
 * The Zongsoft.Externals.Aliyun is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Aliyun is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Aliyun library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;

namespace Zongsoft.Externals.Aliyun.Pushing.Options
{
	/// <summary>
	/// 表示移动应用的配置项接口。
	/// </summary>
	public interface IAppOption
	{
		/// <summary>
		/// 获取或设置移动应用的名称。
		/// </summary>
		string Name
		{
			get;
			set;
		}

		/// <summary>
		/// 获取或设置移动应用的代号(即移动推送的App-Key)。
		/// </summary>
		string Code
		{
			get;
			set;
		}

		/// <summary>
		/// 获取或设置移动应用的推送密码。
		/// </summary>
		string Secret
		{
			get;
			set;
		}

		/// <summary>
		/// 获取或设置移动应用所属的运营商区域。
		/// </summary>
		ServiceCenterName? Region
		{
			get;
			set;
		}

		/// <summary>
		/// 获取或设置移动应用关联的凭证名。
		/// </summary>
		string Certificate
		{
			get;
			set;
		}
	}
}

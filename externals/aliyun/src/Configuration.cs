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

namespace Zongsoft.Externals.Aliyun
{
	/// <summary>
	/// 表示本应用的配置类。
	/// </summary>
	public static class Configuration
	{
		#region 成员字段
		private static Options.IConfiguration _instance;
		#endregion

		#region	公共属性
		/// <summary>
		/// 获取或设置本应用的配置对象。
		/// </summary>
		public static Options.IConfiguration Instance
		{
			get
			{
				if(_instance == null)
				{
					_instance = Zongsoft.Options.OptionManager.Instance.GetOptionValue("/Externals/Aliyun/General") as Options.IConfiguration;

					if(_instance == null)
						throw new InvalidOperationException("Missing required configuation of the Aliyun.");
				}

				return _instance;
			}
			set
			{
				_instance = value ?? throw new ArgumentNullException();
			}
		}
		#endregion
	}
}

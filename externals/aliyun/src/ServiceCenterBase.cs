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
	/// 表示服务中心的基类。
	/// </summary>
	public class ServiceCenterBase
	{
		#region 成员字段
		private ServiceCenterName _name;
		private string _alias;
		private string _path;
		#endregion

		#region 构造函数
		protected ServiceCenterBase(ServiceCenterName name, bool isIntranet)
		{
			_name = name;

			switch(name)
			{
				case ServiceCenterName.Beijing: //北京服务中心
					_alias = "cn-beijing";
					_path = isIntranet ? "beijing-internal.aliyuncs.com" : "beijing.aliyuncs.com";
					break;
				case ServiceCenterName.Qingdao: //青岛服务中心
					_alias = "cn-qingdao";
					_path = isIntranet ? "qingdao-internal.aliyuncs.com" : "qingdao.aliyuncs.com";
					break;
				case ServiceCenterName.Hangzhou: //杭州服务中心
					_alias = "cn-hangzhou";
					_path = isIntranet ? "hangzhou-internal.aliyuncs.com" : "hangzhou.aliyuncs.com";
					break;
				case ServiceCenterName.Shenzhen: //深圳服务中心
					_alias = "cn-shenzhen";
					_path = isIntranet ? "shenzhen-internal.aliyuncs.com" : "shenzhen.aliyuncs.com";
					break;
				case ServiceCenterName.Hongkong: //香港服务中心
					_alias = "cn-hongkong";
					_path = isIntranet ? "hongkong-internal.aliyuncs.com" : "hongkong.aliyuncs.com";
					break;
			}
		}
		#endregion

		#region 公共属性
		/// <summary>
		/// 获取服务中心的名称。
		/// </summary>
		public ServiceCenterName Name
		{
			get => _name;
		}

		/// <summary>
		/// 获取或设置服务中心的别名。
		/// </summary>
		public string Alias
		{
			get => _alias;
			protected set => _alias = value;
		}

		/// <summary>
		/// 获取或设置服务中心的访问路径。
		/// </summary>
		public virtual string Path
		{
			get => _path;
			protected set
			{
				if(string.IsNullOrWhiteSpace(value))
					throw new ArgumentNullException();

				_path = value.Trim();
			}
		}
		#endregion
	}
}

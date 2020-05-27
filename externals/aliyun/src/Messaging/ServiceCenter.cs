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
using System.ComponentModel;

namespace Zongsoft.Externals.Aliyun.Messaging
{
	/// <summary>
	/// 表示消息队列服务中心的类。
	/// </summary>
	public class ServiceCenter : ServiceCenterBase
	{
		#region 常量定义
		//中国存储服务中心访问地址的前缀常量
		private const string MNS_CN_PREFIX = "mns.cn-";

		//美国存储服务中心访问地址的前缀常量
		private const string MNS_US_PREFIX = "mns.us-";
		#endregion

		#region 构造函数
		private ServiceCenter(ServiceCenterName name, bool isInternal) : base(name, isInternal)
		{
			this.Path = MNS_CN_PREFIX + base.Path;
		}
		#endregion

		#region 静态方法
		public static ServiceCenter GetInstance(ServiceCenterName name, bool isInternal = false)
		{
			switch(name)
			{
				case ServiceCenterName.Beijing:
					return isInternal ? Internal.Beijing : External.Beijing;
				case ServiceCenterName.Qingdao:
					return isInternal ? Internal.Qingdao : External.Qingdao;
				case ServiceCenterName.Hangzhou:
					return isInternal ? Internal.Hangzhou : External.Hangzhou;
				case ServiceCenterName.Shenzhen:
					return isInternal ? Internal.Shenzhen : External.Shenzhen;
				case ServiceCenterName.Hongkong:
					return isInternal ? Internal.Hongkong : External.Hongkong;
			}

			throw new NotSupportedException();
		}
		#endregion

		#region 嵌套子类
		public static class External
		{
			/// <summary>北京消息队列服务中心的外部访问地址</summary>
			public static readonly ServiceCenter Beijing = new ServiceCenter(ServiceCenterName.Beijing, false);

			/// <summary>青岛消息队列服务中心的外部访问地址</summary>
			public static readonly ServiceCenter Qingdao = new ServiceCenter(ServiceCenterName.Qingdao, false);

			/// <summary>杭州消息队列服务中心的外部访问地址</summary>
			public static readonly ServiceCenter Hangzhou = new ServiceCenter(ServiceCenterName.Hangzhou, false);

			/// <summary>深圳消息队列服务中心的外部访问地址</summary>
			public static readonly ServiceCenter Shenzhen = new ServiceCenter(ServiceCenterName.Shenzhen, false);

			/// <summary>香港消息队列服务中心的外部访问地址</summary>
			public static readonly ServiceCenter Hongkong = new ServiceCenter(ServiceCenterName.Hongkong, false);
		}

		public static class Internal
		{
			/// <summary>北京消息队列服务中心的内部访问地址</summary>
			public static readonly ServiceCenter Beijing = new ServiceCenter(ServiceCenterName.Beijing, true);

			/// <summary>青岛消息队列服务中心的内部访问地址</summary>
			public static readonly ServiceCenter Qingdao = new ServiceCenter(ServiceCenterName.Qingdao, true);

			/// <summary>杭州消息队列服务中心的内部访问地址</summary>
			public static readonly ServiceCenter Hangzhou = new ServiceCenter(ServiceCenterName.Hangzhou, true);

			/// <summary>深圳消息队列服务中心的内部访问地址</summary>
			public static readonly ServiceCenter Shenzhen = new ServiceCenter(ServiceCenterName.Shenzhen, true);

			/// <summary>香港消息队列服务中心的内部访问地址</summary>
			public static readonly ServiceCenter Hongkong = new ServiceCenter(ServiceCenterName.Hongkong, true);
		}
		#endregion
	}
}

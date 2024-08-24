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

using Zongsoft.Services;
using Zongsoft.Configuration;

namespace Zongsoft.Externals.Aliyun.Options
{
	/// <summary>
	/// 表示阿里云的常规配置选项。
	/// </summary>
	public class GeneralOptions
	{
		#region 构造函数
		public GeneralOptions()
		{
			this.Certificates = new CertificateCollection();
		}
		#endregion

		#region 公共属性
		/// <summary>
		/// 获取或设置配置的服务中心。
		/// </summary>
		public ServiceCenterName Name
		{
			get; set;
		}

		/// <summary>
		/// 获取或设置一个值，指示是否为内网访问。
		/// </summary>
		[ConfigurationProperty("intranet")]
		public bool IsIntranet
		{
			get; set;
		}

		/// <summary>
		/// 获取阿里云的凭证提供程序。
		/// </summary>
		public ICertificateProvider Certificates
		{
			get;
		}
		#endregion

		#region 静态方法
		private static GeneralOptions _instance;
		public static GeneralOptions Instance
		{
			get => _instance ?? (_instance = ApplicationContext.Current.Configuration.GetOption<Options.GeneralOptions>("Externals/Aliyun/General"));
		}
		#endregion

		#region 嵌套子类
		private class CertificateCollection : Zongsoft.Collections.NamedCollectionBase<ICertificate>, ICertificateProvider
		{
			public string Default
			{
				get; set;
			}

			ICertificate ICertificateProvider.Default
			{
				get
				{
					if(this.TryGetItem(this.Default, out var certificate))
						return certificate;

					return null;
				}
			}

			protected override string GetKeyForItem(ICertificate item)
			{
				return item.Name;
			}
		}
		#endregion
	}
}

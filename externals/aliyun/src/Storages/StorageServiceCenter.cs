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

namespace Zongsoft.Externals.Aliyun.Storages
{
	/// <summary>
	/// 表示存储服务中心的类。
	/// </summary>
	public class StorageServiceCenter : ServiceCenterBase
	{
		#region 常量定义
		//中国存储服务中心访问地址的前缀常量
		private const string OSS_CN_PREFIX = "oss-cn-";

		//美国存储服务中心访问地址的前缀常量
		private const string OSS_US_PREFIX = "oss-us-";
		#endregion

		#region 构造函数
		private StorageServiceCenter(ServiceCenterName name, bool isInternal) : base(name, isInternal)
		{
			this.Path = OSS_CN_PREFIX + base.Path;
		}
		#endregion

		#region 公共方法
		public string GetRequestUrl(string path, bool secured = false)
		{
			return this.GetRequestUrl(path, secured, out _);
		}

		public string GetRequestUrl(string path, bool secured, out string resourcePath)
		{
			this.ResolvePath(path, out var bucketName, out resourcePath);

			return secured ?
				Uri.EscapeUriString(string.Format("https://{0}.{1}/{2}", bucketName, this.Path, resourcePath)) :
				Uri.EscapeUriString(string.Format("http://{0}.{1}/{2}", bucketName, this.Path, resourcePath));
		}
		#endregion

		#region 内部方法
		internal string GetBaseUrl(string path)
		{
			string baseName, resourcePath;
			return this.GetBaseUrl(path, out baseName, out resourcePath);
		}

		internal string GetBaseUrl(string path, out string baseName, out string resourcePath)
		{
			this.ResolvePath(path, out baseName, out resourcePath);
			return Uri.EscapeUriString(string.Format("http://{0}.{1}", baseName.ToLowerInvariant(), this.Path));
		}
		#endregion

		#region 静态方法
		public static StorageServiceCenter GetInstance(ServiceCenterName name, bool isInternal = true)
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

		#region 私有方法
		private void ResolvePath(string path, out string bucketName, out string resourcePath)
		{
			if(string.IsNullOrWhiteSpace(path))
				throw new ArgumentNullException(nameof(path));

			path = path.Trim();
			var parts = path.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

			bucketName = parts[0];

			if(parts.Length > 1)
				resourcePath = string.Join("/", parts, 1, parts.Length - 1) + (path.EndsWith("/") ? "/" : string.Empty);
			else
				resourcePath = string.Empty;
		}
		#endregion

		#region 嵌套子类
		public static class External
		{
			/// <summary>北京存储服务中心的外部访问地址</summary>
			public static readonly StorageServiceCenter Beijing = new StorageServiceCenter(ServiceCenterName.Beijing, false);

			/// <summary>青岛存储服务中心的外部访问地址</summary>
			public static readonly StorageServiceCenter Qingdao = new StorageServiceCenter(ServiceCenterName.Qingdao, false);

			/// <summary>杭州存储服务中心的外部访问地址</summary>
			public static readonly StorageServiceCenter Hangzhou = new StorageServiceCenter(ServiceCenterName.Hangzhou, false);

			/// <summary>深圳存储服务中心的外部访问地址</summary>
			public static readonly StorageServiceCenter Shenzhen = new StorageServiceCenter(ServiceCenterName.Shenzhen, false);

			/// <summary>香港存储服务中心的外部访问地址</summary>
			public static readonly StorageServiceCenter Hongkong = new StorageServiceCenter(ServiceCenterName.Hongkong, false);
		}

		public static class Internal
		{
			/// <summary>北京存储服务中心的内部访问地址</summary>
			public static readonly StorageServiceCenter Beijing = new StorageServiceCenter(ServiceCenterName.Beijing, true);

			/// <summary>青岛存储服务中心的内部访问地址</summary>
			public static readonly StorageServiceCenter Qingdao = new StorageServiceCenter(ServiceCenterName.Qingdao, true);

			/// <summary>杭州存储服务中心的内部访问地址</summary>
			public static readonly StorageServiceCenter Hangzhou = new StorageServiceCenter(ServiceCenterName.Hangzhou, true);

			/// <summary>深圳存储服务中心的内部访问地址</summary>
			public static readonly StorageServiceCenter Shenzhen = new StorageServiceCenter(ServiceCenterName.Shenzhen, true);

			/// <summary>香港存储服务中心的内部访问地址</summary>
			public static readonly StorageServiceCenter Hongkong = new StorageServiceCenter(ServiceCenterName.Hongkong, true);
		}
		#endregion
	}
}

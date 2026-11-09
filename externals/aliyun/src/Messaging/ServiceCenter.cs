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
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
 */

using System;
using System.ComponentModel;

namespace Zongsoft.Externals.Aliyun.Messaging;

/// <summary>表示消息队列服务中心的类。</summary>
public class ServiceCenter : ServiceCenterBase
{
	#region 常量定义
	//中国存储服务中心访问地址的前缀常量
	private const string MNS_CN_PREFIX = "mns.cn-";

	//美国存储服务中心访问地址的前缀常量
	private const string MNS_US_PREFIX = "mns.us-";
	#endregion

	#region 构造函数
	private ServiceCenter(ServiceCenterName name, bool isIntranet) : base(name, isIntranet)
	{
		this.Path = MNS_CN_PREFIX + base.Path;
	}
	#endregion

	#region 静态方法
	public static ServiceCenter GetInstance(ServiceCenterName name, bool isIntranet = false)
	{
		switch(name)
		{
			case ServiceCenterName.Beijing:
				return isIntranet ? Intranet.Beijing : Internet.Beijing;
			case ServiceCenterName.Qingdao:
				return isIntranet ? Intranet.Qingdao : Internet.Qingdao;
			case ServiceCenterName.Hangzhou:
				return isIntranet ? Intranet.Hangzhou : Internet.Hangzhou;
			case ServiceCenterName.Shenzhen:
				return isIntranet ? Intranet.Shenzhen : Internet.Shenzhen;
			case ServiceCenterName.Hongkong:
				return isIntranet ? Intranet.Hongkong : Internet.Hongkong;
		}

		throw new NotSupportedException();
	}
	#endregion

	#region 嵌套子类
	public static class Internet
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

	public static class Intranet
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

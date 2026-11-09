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

namespace Zongsoft.Externals.Aliyun.Pushing;

public class PushingServiceCenter : ServiceCenterBase
{
	#region 单例字段
	public static readonly PushingServiceCenter Beijing = new PushingServiceCenter(ServiceCenterName.Beijing);
	public static readonly PushingServiceCenter Qingdao = new PushingServiceCenter(ServiceCenterName.Qingdao);
	public static readonly PushingServiceCenter Hangzhou = new PushingServiceCenter(ServiceCenterName.Hangzhou);
	public static readonly PushingServiceCenter Shenzhen = new PushingServiceCenter(ServiceCenterName.Shenzhen);
	public static readonly PushingServiceCenter Hongkong = new PushingServiceCenter(ServiceCenterName.Hongkong);
	#endregion

	#region 构造函数
	private PushingServiceCenter(ServiceCenterName name) : base(name, false)
	{
		this.Path = "cloudpush.aliyuncs.com";
	}
	#endregion

	#region 静态方法
	public static PushingServiceCenter GetInstance(ServiceCenterName name)
	{
		switch(name)
		{
			case ServiceCenterName.Beijing:
				return Beijing;
			case ServiceCenterName.Qingdao:
				return Qingdao;
			case ServiceCenterName.Hangzhou:
				return Hangzhou;
			case ServiceCenterName.Shenzhen:
				return Shenzhen;
			case ServiceCenterName.Hongkong:
				return Hongkong;
		}

		return null;
	}
	#endregion
}

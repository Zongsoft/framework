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

namespace Zongsoft.Externals.Aliyun;

/// <summary>表示服务中心的基类。</summary>
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
	/// <summary>获取服务中心的名称。</summary>
	public ServiceCenterName Name
	{
		get => _name;
	}

	/// <summary>获取或设置服务中心的别名。</summary>
	public string Alias
	{
		get => _alias;
		protected set => _alias = value;
	}

	/// <summary>获取或设置服务中心的访问路径。</summary>
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

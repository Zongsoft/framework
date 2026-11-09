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

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Zongsoft.Services;

/// <summary>表示服务注册的接口。</summary>
/// <remarks>程序集扫描支持非公开的实现类；优先使用公开静态字段或属性提供的注册器实例，否则通过公开或非公开的无参构造函数创建实例。</remarks>
public interface IServiceRegistration
{
	void Register(IServiceCollection services, IConfiguration configuration);
}

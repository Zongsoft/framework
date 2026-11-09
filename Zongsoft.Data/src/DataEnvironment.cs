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
using System.Reflection;

using Zongsoft.Services;
using Zongsoft.Data.Common;

namespace Zongsoft.Data;

/// <summary>提供数据访问操作的环境信息。</summary>
[DefaultMember(nameof(Filters))]
[Service(Members = $"{nameof(Filters)}")]
public static class DataEnvironment
{
	#region 成员字段
	private static IDataValidatorProvider _validators;
	#endregion

	#region 静态构造
	static DataEnvironment()
	{
		Drivers = new();
		Filters = new();
		Populators = new();
		_validators = DataValidatorProvider.Instance;
	}
	#endregion

	#region 公共属性
	public static DataDriverCollection Drivers { get; }
	public static DataAccessFilterCollection Filters { get; }
	public static DataPopulatorProviderManager Populators { get; }

	public static IDataValidatorProvider Validators
	{
		get => _validators;
		set => _validators = value ?? throw new ArgumentNullException();
	}
	#endregion
}

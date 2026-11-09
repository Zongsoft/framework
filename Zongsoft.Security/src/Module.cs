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

using Zongsoft.Data;
using Zongsoft.Services;

[assembly: ApplicationModule(Zongsoft.Security.Module.NAME)]

namespace Zongsoft.Security;

public partial class Module : ApplicationModule<Module.EventRegistry>
{
	#region 常量定义
	/// <summary>表示安全模块的名称常量值。</summary>
	public const string NAME = nameof(Security);
	#endregion

	#region 单例字段
	/// <summary>表示安全模块的单例字段。</summary>
	public static readonly Module Current = new();
	#endregion

	#region 私有构造
	private Module() : base(NAME) { }
	#endregion

	#region 公共属性
	private IDataAccess _accessor;
	public IDataAccess Accessor => _accessor ??= this.Services.ResolveRequired<IDataAccessProvider>().GetAccessor(this.Name);
	#endregion
}

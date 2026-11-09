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
using Zongsoft.Components;

[assembly: ApplicationModule(Zongsoft.Upgrading.Module.NAME)]

namespace Zongsoft.Upgrading;

/// <summary>表示升级模块。</summary>
public class Module : ApplicationModule<Module.EventRegistry>
{
	#region 常量定义
	/// <summary>表示升级模块的名称常量。</summary>
	public const string NAME = nameof(Upgrading);
	#endregion

	#region 单例字段
	/// <summary>获取升级模块单例实例。</summary>
	public static readonly Module Current = new();
	#endregion

	#region 私有构造
	private Module() : base(NAME) => this.Evaluators = [Evaluator.Default];
	#endregion

	#region 公共属性
	/// <summary>获取升级模块的数据访问器。</summary>
	public IDataAccess Accessor => field ??= this.Services.ResolveRequired<IDataAccessProvider>().GetAccessor(this.Name);

	/// <summary>获取升级评估器集合。</summary>
	public EvaluatorCollection Evaluators { get; }
	#endregion

	#region 嵌套子类
	/// <summary>表示升级模块的事件注册表。</summary>
	public sealed class EventRegistry : EventRegistryBase
	{
		#region 构造函数
		public EventRegistry() : base(NAME) { }
		#endregion
	}
	#endregion
}

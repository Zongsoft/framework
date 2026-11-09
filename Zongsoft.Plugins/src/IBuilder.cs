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
using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Plugins;

/// <summary>表示构建器的接口。</summary>
public interface IBuilder : IDisposable
{
	/// <summary>获取构建器对应指定构件的构建结果的类型。</summary>
	/// <param name="builtin">要获取构建结果类型的构件。</param>
	/// <returns>返回构件目标对象的类型；无法确定时返回空。</returns>
	Type GetValueType(Builtin builtin);

	/// <summary>创建指定构件对应的目标对象。</summary>
	/// <param name="context">包含构件、构建结果和构建设置的上下文。</param>
	/// <returns>创建成功后的目标对象，有关该返回值的详细定义请参考说明部分。</returns>
	/// <remarks>
	///		<para>该方法返回值会被作为对应<see cref="Builtin"/>的子构件对应目标对象的所有者(即上级对象)。</para>
	///		<para>实现者可以在本方法中通过构建上下文决定是否装配子节点、是否向所有者追加。管理器将这些决定与返回的实例一起保存。</para>
	/// </remarks>
	object Build(Builders.BuilderContext context);

	/// <summary>卸载指定构件对应的目标对象。</summary>
	/// <param name="context">包含构件、构建结果和构建设置的上下文。</param>
	/// <param name="cancellation">取消等待的通知。</param>
	/// <returns>构件结果释放完成的操作。</returns>
	ValueTask DestroyAsync(Builders.BuilderContext context, CancellationToken cancellation = default);
}

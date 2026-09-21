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
 * This file is part of Zongsoft.Plugins library.
 *
 * The Zongsoft.Plugins is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Plugins is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Plugins library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;

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
	///		<para>如果该方法内部设置了<paramref name="context"/>参数对象中的<seealso cref="Builders.BuilderContext.Result"/>属性不为空<c>null</c>。</para>
	/// </remarks>
	object Build(Builders.BuilderContext context);

	/// <summary>当创建完成后由创建管理器调用。</summary>
	/// <param name="context">指定的创建器上下文对象。</param>
	/// <remarks>注意：对于创建器的实现者，请在本方法内实现将目标对象添加到所有者对应的子集中去的逻辑，切勿在<see cref="Build"/>方法中实现添加逻辑，因为构建逻辑可被外部调用者覆盖，因此务必将创建于添加子集的逻辑分别处理。</remarks>
	void OnBuildComplete(Builders.BuilderContext context);

	/// <summary>卸载指定构件对应的目标对象。</summary>
	/// <param name="context">包含构件、构建结果和构建设置的上下文。</param>
	void Destroy(Builders.BuilderContext context);
}

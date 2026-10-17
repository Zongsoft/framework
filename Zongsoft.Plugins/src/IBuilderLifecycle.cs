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
 * Copyright (C) 2010-2026 Zongsoft Studio <http://www.zongsoft.com>
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

namespace Zongsoft.Plugins;

/// <summary>提供构建过程的可选生命周期回调。</summary>
/// <remarks>
///		<para>由构建器按需实现，构建管理器负责调用。回调异常将中断本次构建。</para>
///		<para>已完成实例的缓存复用不重复回调；仅创建实例后延后装配时，两次回调可能使用不同的上下文对象。</para>
/// </remarks>
public interface IBuilderLifecycle
{
	/// <summary>在实际调用构建器创建实例之前执行。</summary>
	/// <param name="context">本次构建的上下文，其结果尚未赋值。</param>
	/// <remarks>管理器已建立重入保护。每次实际创建尝试调用一次，缓存复用和延后完成不重复调用。</remarks>
	void OnBuilding(Builders.BuilderContext context);

	/// <summary>在非空实例及其所需子节点装配完成后、向所有者追加之前执行。</summary>
	/// <param name="context">包含已发布实例的构建上下文。</param>
	/// <remarks>
	///		<para>回调成功且追加流程完成后，管理器才设置 <see cref="Builtin.IsBuilded"/>。回调不能替换结果或更改已保存的装配决定。</para>
	///		<para>返回空值或此前构建失败时不调用；仅创建实例用于模块定位时，推迟到正常装配完成后调用。</para>
	/// </remarks>
	void OnBuilded(Builders.BuilderContext context);
}

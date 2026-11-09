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

namespace Zongsoft.Common;

public partial class Variables
{
	#region 静态字段
	private static readonly IVariables _processEnvironments = new EnvironmentVariables(EnvironmentVariableTarget.Process);
	private static readonly IVariables _machineEnvironments = new EnvironmentVariables(EnvironmentVariableTarget.Machine);
	private static readonly IVariables _userEnvironments = new EnvironmentVariables(EnvironmentVariableTarget.User);
	#endregion

	#region 静态方法
	/// <summary>获取指定来源的环境变量实时视图。</summary>
	/// <param name="target">环境变量来源，默认为当前进程；用户和计算机来源遵循平台支持范围。</param>
	/// <returns>指定来源共享的只读变量视图；复用视图不缓存环境变量的值。</returns>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="target"/> 不是有效的环境变量来源。</exception>
	/// <remarks>
	/// 	<para>每次查询调用 <see cref="Environment.GetEnvironmentVariable(string, EnvironmentVariableTarget)"/>，返回原始字符串，不展开模板或转换类型。</para>
	/// 	<para>此视图是 <see cref="IVariables"/> 忽略大小写约定的特例，名称比较遵循平台规则：Windows 忽略大小写，Unix/Linux 区分大小写；调用方应使用来源所需的名称。不合并或回退到其它来源。</para>
	/// 	<para>只提供全局命名空间；<see langword="null"/> 或空字符串直接查询名称，非空命名空间仅在 <c>fallback=true</c> 时回退到全局。没有其它默认值，查询参数不裁剪或拆分。</para>
	/// 	<para>查询名称为 <see langword="null"/> 时抛出 <see cref="ArgumentNullException"/>；存在的空字符串仍表示查询成功，原生访问异常向调用方传播。</para>
	/// 	<para>Unix/Linux 上用户和计算机来源的查询结果为空。视图不保证多次查询期间的环境变量保持不变。</para>
	/// </remarks>
	public static IVariables Environments(EnvironmentVariableTarget target = EnvironmentVariableTarget.Process) => target switch
	{
		EnvironmentVariableTarget.Process => _processEnvironments,
		EnvironmentVariableTarget.User => _userEnvironments,
		EnvironmentVariableTarget.Machine => _machineEnvironments,
		_ => throw new ArgumentOutOfRangeException(nameof(target)),
	};
	#endregion

	#region 嵌套子类
	private sealed class EnvironmentVariables(EnvironmentVariableTarget target) : IVariables
	{
		public bool TryGetValue(string name, out object value) => this.TryGetValue(null, name, out value);
		public bool TryGetValue(string name, bool fallback, out object value) => this.TryGetValue(null, name, fallback, out value);
		public bool TryGetValue(string @namespace, string name, out object value) => this.TryGetValue(@namespace, name, false, out value);
		public bool TryGetValue(string @namespace, string name, bool fallback, out object value)
		{
			ArgumentNullException.ThrowIfNull(name);
			value = fallback || string.IsNullOrEmpty(@namespace) ? Environment.GetEnvironmentVariable(name, target) : null;
			return value != null;
		}
	}
	#endregion
}

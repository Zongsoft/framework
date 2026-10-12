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
 * Copyright (C) 2010-2025 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Core library.
 *
 * The Zongsoft.Core is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Core is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Core library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;

namespace Zongsoft.Configuration;

/// <summary>表示具有驱动、类型化读取能力和扩展属性的连接设置。</summary>
/// <remarks>
///		<para>继承的字典成员只访问显式主参数；扩展参数通过 <see cref="Properties"/> 访问。
///		<see cref="ISettings.Value"/> 同时包含这两部分，类型化读取使用的描述器默认值不会写入字典或文本。</para>
///		<para>不带选项类型约束的连接设置允许任意主参数键；带选项类型约束的连接设置依据
///		<see cref="IConnectionSettingsDriver.Descriptors"/> 识别名称、别名及有效复合路径，文本中的未知参数归入扩展属性，向主字典写入未知名称会抛出 <see cref="ArgumentException"/>。</para>
///		<para>连接设置按设置名称、驱动名称、主参数及扩展属性判断相等。名称和键忽略大小写，值区分大小写，条目顺序不影响结果。</para>
/// </remarks>
[Configuration(nameof(Properties), ResolverType = typeof(ConnectionSettingsResolver))]
public interface IConnectionSettings : ISettings, IEquatable<IConnectionSettings>
{
	#region 属性定义
	/// <summary>获取当前连接设置的驱动。</summary>
	/// <value>负责解释连接参数的驱动实例；实现者必须保证此值不为 <see langword="null"/>。</value>
	IConnectionSettingsDriver Driver { get; }

	/// <summary>获取是否包含扩展属性。</summary>
	/// <value><see cref="Properties"/> 包含至少一项时为 <see langword="true"/>，否则为 <see langword="false"/>。</value>
	/// <remarks>此属性与主字典的 <see cref="ISettings.HasValue"/> 分别判断各自的内容。</remarks>
	bool HasProperties { get; }

	/// <summary>获取连接设置的可写扩展属性集。</summary>
	/// <value>键名忽略大小写的字符串字典；无扩展参数时返回空字典。</value>
	/// <remarks>
	/// <para>扩展属性的添加、修改、删除及清空均反映到 <see cref="ISettings.Value"/>，但不会改变主字典的计数和枚举结果。
	/// 清空主字典会保留扩展属性；给 <see cref="ISettings.Value"/> 赋值则会替换两者。</para>
	/// <para>对于带选项类型约束的连接设置，通过此字典的索引器或添加方法写入描述器已定义的名称、别名或有效复合路径，
	/// 会抛出 <see cref="ArgumentException"/> 且不改变状态；这些参数应通过主字典或类型化属性设置。</para>
	/// </remarks>
	IDictionary<string, string> Properties { get; }
	#endregion

	#region 方法定义
	/// <summary>获取指定主参数的类型化值。</summary>
	/// <typeparam name="T">要转换到的目标类型。</typeparam>
	/// <param name="name">主参数名称、驱动描述器支持的别名或有效复合路径。</param>
	/// <returns>返回转换后的显式值；未设置时使用描述器默认值，没有可用默认值时返回 <typeparamref name="T"/> 的默认值。</returns>
	/// <exception cref="ArgumentNullException"><paramref name="name"/> 为 <see langword="null"/>。</exception>
	/// <exception cref="InvalidOperationException">参数值无法转换为目标类型或无法组装为对应的复合参数。</exception>
	/// <remarks>不读取扩展属性。显式值转换失败时不会回退到描述器默认值；读取默认值不会将其加入主字典。</remarks>
	T GetValue<T>(string name);

	/// <summary>尝试获取指定主参数的类型化值。</summary>
	/// <typeparam name="T">要转换到的目标类型。</typeparam>
	/// <param name="name">主参数名称、驱动描述器支持的别名或有效复合路径。</param>
	/// <param name="value">成功时接收转换后的显式值或描述器声明的默认值；失败时为 <typeparamref name="T"/> 的默认值。</param>
	/// <returns>显式值可转换，或缺少显式值且描述器声明的默认值可转换时返回 <see langword="true"/>；名称为 <see langword="null"/>、缺失且未声明默认值、或转换失败时返回 <see langword="false"/>。</returns>
	/// <remarks>不读取扩展属性。显式的 <see langword="false"/> 和零值也可以成功读取；非法显式值不会回退到描述器默认值。
	/// 与继承的非泛型字典读取方法不同，此方法支持类型转换及描述器默认值。</remarks>
	bool TryGetValue<T>(string name, out T value);
	#endregion

	#region 默认实现
	/// <summary>判断当前连接的驱动是否匹配指定名称。</summary>
	/// <param name="name">要匹配的驱动名称；<see langword="null"/> 或空字符串表示无名驱动。</param>
	/// <returns>当前驱动匹配指定名称时返回 <see langword="true"/>，否则返回 <see langword="false"/>。</returns>
	/// <remarks>匹配规则由当前驱动的 <see cref="IConnectionSettingsDriver.IsDriver(string)"/> 方法决定。</remarks>
	bool IsDriver(string name) => ConnectionSettingsUtility.IsDriver(this, name);

	/// <summary>判断当前连接的驱动是否匹配指定驱动。</summary>
	/// <param name="driver">要匹配的驱动；<see langword="null"/> 表示无名驱动。</param>
	/// <returns>当前驱动匹配指定驱动名称时返回 <see langword="true"/>，否则返回 <see langword="false"/>。</returns>
	/// <remarks>使用指定驱动的名称进行匹配，不比较驱动实例引用。</remarks>
	bool IsDriver(IConnectionSettingsDriver<IConnectionSettings> driver) => ConnectionSettingsUtility.IsDriver(this, driver);
	#endregion
}

/// <summary>表示能够生成指定类型选项的连接设置。</summary>
/// <typeparam name="TOptions">提供程序或业务组件使用的选项类型。</typeparam>
/// <remarks>主参数范围由驱动描述器决定，未定义参数保存在扩展属性中；完整设置文本通过 <see cref="ISettings.Value"/> 获取。</remarks>
public interface IConnectionSettings<out TOptions> : IConnectionSettings
{
	/// <summary>根据当前连接参数生成对应的选项对象。</summary>
	/// <returns>返回由显式主参数及描述器默认值填充的 <typeparamref name="TOptions"/> 对象。</returns>
	/// <exception cref="InvalidOperationException">参与选项填充的参数值无法转换或无法组装为对应的复合参数。</exception>
	/// <remarks>扩展属性和标记为 <see cref="ConnectionSettingDescriptor.Ignored"/> 的业务参数不参与标准选项填充。
	/// 需要提供程序原生连接字符串时，应从具体驱动生成的选项中获取，不应直接使用完整设置文本。</remarks>
	TOptions GetOptions();
}

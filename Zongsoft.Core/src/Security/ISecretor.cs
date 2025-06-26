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
using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Security;

/// <summary>
/// 提供秘密（验证码）生成和校验功能的接口。
/// </summary>
public interface ISecretor
{
	#region 属性定义
	/// <summary>获取或设置秘密内容的默认过期时长（默认为10分钟），不能设置为零。</summary>
	TimeSpan Expiry { get; set; }

	/// <summary>获取或设置重新生成秘密(验证码)的最小间隔时长，如果为零则表示不做限制。</summary>
	TimeSpan Period { get; set; }

	/// <summary>获取或设置秘密(验证码)发射器。</summary>
	SecretTransmitter Transmitter { get; set; }
	#endregion

	#region 方法定义
	/// <summary>判断指定名称的秘密（验证码）是否存在。</summary>
	/// <param name="name">指定的验证码名称。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回一个元组，分别表示指定名称的验证码是否存在（有效），以及其有效期时长。</returns>
	ValueTask<(bool existed, TimeSpan duration)> ExistsAsync(string name, CancellationToken cancellation = default);

	/// <summary>生成一个指定名称的秘密（验证码）。</summary>
	/// <param name="name">指定的验证码名称，该名称通常包含对应目标标识（譬如：user.forget:100、user.email:100，其中数字100表示用户的唯一编号），调用者应确保该名称全局唯一。</param>
	/// <param name="extra">指定的附加文本，该附加文本可通过<see cref="VerifyAsync(string, string, CancellationToken)"/>方法验证通过后获取到。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回生成成功的验证码，关于验证码的具体生成规则请参考特定实现版本。</returns>
	ValueTask<string> GenerateAsync(string name, string extra, CancellationToken cancellation = default);

	/// <summary>生成一个指定名称的秘密（验证码）。</summary>
	/// <param name="name">指定的验证码名称，该名称通常包含对应的目标标识（譬如：user.forget:100、user.phone:13812345678，其中数字100表示用户的唯一编号)，调用者应确保该名称全局唯一。</param>
	/// <param name="pattern">指定的验证码生成模式，基本定义参考备注说明。</param>
	/// <param name="extra">指定的附加文本，该附加文本可通过<see cref="VerifyAsync(string, string, CancellationToken)"/>方法验证通过后获取到。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回生成成功的验证码，关于验证码的生成规则由<paramref name="pattern"/>参数定义。</returns>
	/// <remarks>
	/// 	<para>参数<paramref name="pattern"/>用来定义生成验证码的模式，如果为空(<c>null</c>)或空字符串则由特定实现版本自行定义（建议生成6位数字的验证码）；也可以表示生成验证码的规则，基本模式定义如下：</para>
	/// 	<list type="bullet">
	/// 		<item>guid|uuid，表示生成一个GUID值</item>
	/// 		<item>#{number}，表示生成{number}个的数字字符，譬如：#4</item>
	/// 		<item>?{number}，表示生成{number}个的含有字母或数字的字符，譬如：?8</item>
	/// 		<item>*{number}，完全等同于?{number}。</item>
	/// 	</list>
	/// 	<para>注：如果<paramref name="pattern"/>参数不匹配模式定义，则表示其即为要生成的秘密（验证码）值，这样的固定秘密（验证码）应只由字母和数字组成，不要包含其他符号。</para>
	/// </remarks>
	ValueTask<string> GenerateAsync(string name, string pattern, string extra, CancellationToken cancellation = default);

	/// <summary>验证指定名称的秘密（验证码）是否正确并删除它。</summary>
	/// <param name="name">指定的验证码名称。</param>
	/// <param name="secret">指定待确认的验证码。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回一个元组，分别表示验证是否成功，以及其绑定的附加文本。</returns>
	ValueTask<(bool succeed, string extra)> RemoveAsync(string name, string secret, CancellationToken cancellation = default);

	/// <summary>验证指定名称的秘密（验证码）是否正确。</summary>
	/// <param name="name">指定的验证码名称。</param>
	/// <param name="secret">指定待确认的验证码。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回一个元组，分别表示验证是否成功，以及其绑定的附加文本。</returns>
	/// <remarks>验证成功并不会立即删除对应的缓存项（即可重复验证），如果希望验证后立即失效应使用<see cref="RemoveAsync(string, string, CancellationToken)"/>方法。</remarks>
	ValueTask<(bool succeed, string extra)> VerifyAsync(string name, string secret, CancellationToken cancellation = default);
	#endregion

	#region 嵌套接口
	/// <summary>
	/// 提供秘密（验证码）发送功能的类。
	/// </summary>
	public abstract class SecretTransmitter
	{
		public ValueTask<string> TransmitAsync(string scheme, string destination, string template, string scenario, string captcha, CancellationToken cancellation = default) =>
			this.TransmitAsync(scheme, destination, template, scenario, captcha, null, null, cancellation);

		public ValueTask<string> TransmitAsync(string scheme, string destination, string template, string scenario, string captcha, string channel, CancellationToken cancellation = default) =>
			this.TransmitAsync(scheme, destination, template, scenario, captcha, channel, null, cancellation);

		/// <summary>发送秘密（验证码）到指定的目的。</summary>
		/// <param name="scheme">指定的发送方案。</param>
		/// <param name="destination">指定的验证码接受目的。</param>
		/// <param name="template">指定的模板标识。</param>
		/// <param name="scenario">指定的应用场景。</param>
		/// <param name="captcha">指定的人机识别结果令牌。</param>
		/// <param name="channel">指定的通道标识。</param>
		/// <param name="extra">指定的附加信息。</param>
		/// <param name="cancellation">指定的异步操作取消标记。</param>
		/// <returns>返回的验证码凭证标识。</returns>
		public abstract ValueTask<string> TransmitAsync(string scheme, string destination, string template, string scenario, string captcha, string channel, string extra, CancellationToken cancellation = default);
	}
	#endregion
}

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

namespace Zongsoft.Security;

/// <summary>
/// 表示数字证书提供程序的接口。
/// </summary>
[Obsolete("This interface has been deprecated, Please use the CertificateManager class instead.")]
public interface ICertificateProvider<out TCertificate> where TCertificate : ICertificate
{
	/// <summary>获取数字证书提供程序名称。</summary>
	string Name { get; }

	/// <summary>获取一个符合参数的数字证书。</summary>
	/// <param name="subject">指定要获取的主体标识。</param>
	/// <param name="format">指定要获取的证书格式。</param>
	/// <returns>返回指定条件的证书对象，如果没有找到则返回空(null)。</returns>
	TCertificate GetCertificate(string subject, string format = null);
}

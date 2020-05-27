/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
 *
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Externals.Aliyun library.
 *
 * The Zongsoft.Externals.Aliyun is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Aliyun is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Aliyun library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;

namespace Zongsoft.Externals.Aliyun.Messaging
{
	internal class MessageQueueAuthenticator : HttpAuthenticator
	{
		#region 单例字段
		public static MessageQueueAuthenticator Instance = new MessageQueueAuthenticator("MNS");
		#endregion

		#region 私有构造
		private MessageQueueAuthenticator(string name) : base(name, HttpSignatureMode.Header)
		{
		}
		#endregion

		#region 重写方法
		protected override bool IsCanonicalizedHeader(string name)
		{
			return name.StartsWith("x-mns-");
		}

		protected override string CanonicalizeResource(HttpRequestMessage request)
		{
			return request.RequestUri.PathAndQuery;
		}
		#endregion
	}
}

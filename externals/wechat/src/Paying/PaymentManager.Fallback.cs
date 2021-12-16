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
 * This file is part of Zongsoft.Externals.WeChat library.
 *
 * The Zongsoft.Externals.WeChat is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.WeChat is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.WeChat library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

using Zongsoft.Common;

namespace Zongsoft.Externals.Wechat.Paying
{
	public partial class PaymentManager
	{
		public class Fallback
		{
			#region 嵌套子类
			private class ResponseWrapper
			{
				[JsonPropertyName("id")]
				public string Identifier { get; set; }

				[JsonPropertyName("create_time")]
				public DateTime Timestamp { get; set; }

				[JsonPropertyName("event_type")]
				public string Status { get; set; }

				[JsonPropertyName("summary")]
				public string Description { get; set; }

				[JsonPropertyName("resource_type")]
				public string ResourceType { get; set; }

				[JsonPropertyName("resource")]
				public ResourceInfo Resource { get; set; }

				public struct ResourceInfo
				{
					[JsonPropertyName("original_type")]
					public string Source { get; set; }
					[JsonPropertyName("algorithm")]
					public string Algorithm { get; set; }
					[JsonPropertyName("nonce")]
					public string Nonce { get; set; }
					[JsonPropertyName("associated_data")]
					public string AssociatedData { get; set; }
					[JsonPropertyName("ciphertext")]
					public string Ciphertext { get; set; }
				}
			}
			#endregion
		}
	}
}

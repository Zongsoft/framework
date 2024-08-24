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
using System.Xml;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Zongsoft.Externals.Wechat
{
	internal static class XmlUtility
	{
		#region 私有变量
		private static readonly XmlReaderSettings _settings = new()
		{
			IgnoreComments = true,
			IgnoreWhitespace = true,
			IgnoreProcessingInstructions = true,
			ValidationType = ValidationType.None,
		};
		#endregion

		#region 公共方法
		public static StringContent CreateXmlContent(this IEnumerable<KeyValuePair<string, object>> data)
		{
			var text = new System.Text.StringBuilder();
			text.AppendLine("<xml>");

			foreach(var entry in data)
			{
				if(entry.Value == null)
					continue;

				text.AppendLine($"<{entry.Key}>{entry.Value}</{entry.Key}>");
			}

			text.AppendLine("</xml>");
			return new StringContent(text.ToString(), System.Text.Encoding.UTF8, "application/xml");
		}

		public static IDictionary<string, string> GetXmlContent(this HttpResponseMessage response, CancellationToken cancellation = default)
		{
			if(response == null || response.Content.Headers.ContentLength <= 0)
				return null;

			return Resolve(response.Content.ReadAsStream(cancellation));
		}

		public static async ValueTask<IDictionary<string, string>> GetXmlContentAsync(this HttpResponseMessage response, CancellationToken cancellation = default)
		{
			if(response == null || response.Content.Headers.ContentLength <= 0)
				return null;

			return Resolve(await response.Content.ReadAsStreamAsync(cancellation));
		}
		#endregion

		#region 私有方法
		private static IDictionary<string, string> Resolve(System.IO.Stream stream, XmlReaderSettings settings = null)
		{
			if(stream == null || !stream.CanRead)
				return null;

			using var reader = XmlReader.Create(stream, settings ?? _settings);

			if(reader.Read())
			{
				var result = new Dictionary<string, string>();

				while(reader.Read())
				{
					if(reader.NodeType == XmlNodeType.Element && !reader.IsEmptyElement)
					{
						var key = reader.LocalName;

						if(reader.Read() && (reader.NodeType == XmlNodeType.Text || reader.NodeType == XmlNodeType.CDATA))
							result[key] = reader.Value;
					}
				}

				return result;
			}

			return null;
		}
		#endregion
	}
}

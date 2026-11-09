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
using System.IO;
using System.Xml;
using System.Text;
using System.Net.Http;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Zongsoft.Externals.Wechat;

internal static class XmlUtility
{
	private const int MAXIMUM_LENGTH = 2 * 1024 * 1024;

	public static StringContent CreateXmlContent(this IEnumerable<KeyValuePair<string, object>> data)
	{
		ArgumentNullException.ThrowIfNull(data);
		var text = new StringBuilder();

		using(var writer = XmlWriter.Create(text, new XmlWriterSettings { OmitXmlDeclaration = true }))
		{
			writer.WriteStartElement("xml");
			foreach(var entry in data)
			{
				var value = Convert.ToString(entry.Value, CultureInfo.InvariantCulture);
				if(!string.IsNullOrEmpty(value))
					writer.WriteElementString(entry.Key, value);
			}

			writer.WriteEndElement();
		}

		return new StringContent(text.ToString(), Encoding.UTF8, "application/xml");
	}

	public static async ValueTask<IDictionary<string, string>> GetXmlContentAsync(this HttpResponseMessage response, CancellationToken cancellation = default)
	{
		ArgumentNullException.ThrowIfNull(response);
		if(response.Content.Headers.ContentLength > MAXIMUM_LENGTH)
			throw new FormatException();

		await using var source = await response.Content.ReadAsStreamAsync(cancellation);
		using var buffer = new MemoryStream();
		var bytes = new byte[8192];
		int count;

		while((count = await source.ReadAsync(bytes, cancellation)) != 0)
		{
			if(buffer.Length + count > MAXIMUM_LENGTH)
				throw new FormatException();

			buffer.Write(bytes, 0, count);
		}

		cancellation.ThrowIfCancellationRequested();
		buffer.Position = 0;
		using var reader = XmlReader.Create(buffer, new XmlReaderSettings
		{
			DtdProcessing = DtdProcessing.Prohibit,
			XmlResolver = null,
			MaxCharactersInDocument = MAXIMUM_LENGTH,
			IgnoreWhitespace = true,
		});

		if(reader.MoveToContent() != XmlNodeType.Element || reader.Name != "xml" || reader.NamespaceURI.Length != 0 || reader.HasAttributes || reader.IsEmptyElement)
			throw new FormatException();

		var result = new Dictionary<string, string>(StringComparer.Ordinal);
		reader.ReadStartElement("xml");

		while(reader.NodeType == XmlNodeType.Element)
		{
			if(reader.NamespaceURI.Length != 0 || reader.HasAttributes || result.ContainsKey(reader.Name))
				throw new FormatException();

			var key = reader.Name;
			result.Add(key, reader.ReadElementContentAsString());
		}

		if(reader.NodeType != XmlNodeType.EndElement || reader.Name != "xml")
			throw new FormatException();

		reader.ReadEndElement();
		if(!reader.EOF)
			throw new FormatException();

		return result;
	}
}

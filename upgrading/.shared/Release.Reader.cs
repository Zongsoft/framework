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
 * Copyright (C) 2020-2026 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Upgrading library.
 *
 * The Zongsoft.Upgrading is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Upgrading is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Upgrading library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.IO;
using System.Xml;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Zongsoft.Common;

namespace Zongsoft.Upgrading;

partial class Release
{
	#region 公共方法
	public static IEnumerable<Release> Load(string filePath)
	{
		ArgumentException.ThrowIfNullOrEmpty(filePath);
		return File.Exists(filePath) ? Load(File.OpenRead(filePath)) : [];
	}
	public static IEnumerable<Release> Load(Stream stream)
	{
		ArgumentNullException.ThrowIfNull(stream);
		using var reader = XmlReader.Create(stream, GetSettings(false));

		if(reader.MoveToContent() == XmlNodeType.Element)
		{
			if(reader.LocalName == RELEASES_ELEMENT)
			{
				var depth = reader.Depth;

				while(reader.Read() && reader.Depth > depth)
				{
					var release = Read(reader);

					if(release != null)
						yield return release;
				}
			}
			else if(reader.LocalName == nameof(RELEASE_ELEMENT))
				yield return Read(reader);
		}
	}

	public static IAsyncEnumerable<Release> LoadAsync(string filePath, CancellationToken cancellation = default)
	{
		ArgumentException.ThrowIfNullOrEmpty(filePath);
		return File.Exists(filePath) ? LoadAsync(File.OpenRead(filePath), cancellation) : default;
	}
	public static async IAsyncEnumerable<Release> LoadAsync(Stream stream, [System.Runtime.CompilerServices.EnumeratorCancellation]CancellationToken cancellation = default)
	{
		ArgumentNullException.ThrowIfNull(stream);
		using var reader = XmlReader.Create(stream, GetSettings(true));

		if(reader.MoveToContent() == XmlNodeType.Element)
		{
			if(reader.LocalName == RELEASES_ELEMENT)
			{
				var depth = reader.Depth;

				while(reader.Read() && reader.Depth > depth)
				{
					var release = Read(reader);

					if(release != null)
						yield return release;
				}
			}
			else if(reader.LocalName == nameof(RELEASE_ELEMENT))
				yield return Read(reader);
		}
	}
	#endregion

	#region 内部方法
	internal static Release Read(Stream stream)
	{
		ArgumentNullException.ThrowIfNull(stream);
		using var reader = XmlReader.Create(stream, GetSettings(false));
		return Read(reader);
	}
	internal static Release Read(XmlReader reader)
	{
		ArgumentNullException.ThrowIfNull(reader);

		if(reader.NodeType != XmlNodeType.Element)
			return null;
		if(reader.LocalName != RELEASE_ELEMENT)
			return null;

		var depth = reader.Depth;
		var release = new Release();
		PopulateAttributes(release, reader);

		while(reader.Read() && reader.Depth > depth)
		{
			if(reader.NodeType == XmlNodeType.Element)
			{
				switch(reader.LocalName)
				{
					case nameof(Release.Title):
						if(reader.Read() && reader.NodeType == XmlNodeType.Text)
							release.Title = reader.Value;

						break;
					case nameof(Release.Summary):
						if(reader.Read() && reader.NodeType == XmlNodeType.Text)
							release.Summary = reader.Value;

						break;
					case nameof(Release.Description):
						if(reader.Read() && reader.NodeType == XmlNodeType.Text)
							release.Description = reader.Value;

						break;
					case nameof(TAGS_ELEMENT):
						PopulateTags(release, reader);
						break;
					case nameof(EXECUTORS_ELEMENT):
						PopulateExecutors(release, reader);
						break;
					case nameof(PROPERTIES_ELEMENT):
						PopulateProperties(release, reader);
						break;
				}
			}
		}

		return release;
	}

	internal static ValueTask<Release> ReadAsync(Stream stream, CancellationToken cancellation = default)
	{
		ArgumentNullException.ThrowIfNull(stream);
		using var reader = XmlReader.Create(stream, GetSettings(true));
		return ReadAsync(reader, cancellation);
	}
	internal static async ValueTask<Release> ReadAsync(XmlReader reader, CancellationToken cancellation = default)
	{
		ArgumentNullException.ThrowIfNull(reader);

		if(reader.NodeType != XmlNodeType.Element)
			return null;
		if(reader.LocalName != RELEASE_ELEMENT)
			return null;

		var depth = reader.Depth;
		var release = new Release();
		PopulateAttributes(release, reader);

		while(await reader.ReadAsync() && reader.Depth > depth)
		{
			if(reader.NodeType == XmlNodeType.Element)
			{
				switch(reader.LocalName)
				{
					case nameof(Release.Title):
						if(await reader.ReadAsync() && reader.NodeType == XmlNodeType.Text)
							release.Title = reader.Value;

						break;
					case nameof(Release.Summary):
						if(await reader.ReadAsync() && reader.NodeType == XmlNodeType.Text)
							release.Summary = reader.Value;

						break;
					case nameof(Release.Description):
						if(await reader.ReadAsync() && reader.NodeType == XmlNodeType.Text)
							release.Description = reader.Value;

						break;
					case nameof(TAGS_ELEMENT):
						PopulateTags(release, reader);
						break;
					case nameof(EXECUTORS_ELEMENT):
						PopulateExecutors(release, reader);
						break;
					case nameof(PROPERTIES_ELEMENT):
						PopulateProperties(release, reader);
						break;
				}
			}
		}

		return release;
	}
	#endregion

	#region 私有方法
	static void PopulateTags(Release release, XmlReader reader)
	{
		var depth = reader.Depth;
		var tags = new List<string>();

		while(reader.Read() && reader.Depth > depth)
		{
			if(reader.NodeType == XmlNodeType.Element && reader.LocalName.Equals(TAG_ELEMENT, StringComparison.OrdinalIgnoreCase))
				if(reader.Read() && reader.NodeType == XmlNodeType.Text && !string.IsNullOrWhiteSpace(reader.Value))
					tags.Add(reader.Value);
		}

		if(tags != null && tags.Count > 0)
			release.Tags = [.. tags];
	}

	static void PopulateExecutors(Release release, XmlReader reader)
	{
		var depth = reader.Depth;

		while(reader.Read() && reader.Depth > depth)
		{
			if(reader.NodeType == XmlNodeType.Element && reader.LocalName.Equals(EXECUTOR_ELEMENT, StringComparison.OrdinalIgnoreCase))
			{
				var @event = reader.GetAttribute(EVENT_ATTRIBUTE);

				if(string.IsNullOrWhiteSpace(@event))
					reader.Skip();

				if(reader.Read() && (reader.NodeType == XmlNodeType.Text || reader.NodeType == XmlNodeType.CDATA))
					release.Executors.Add(new(@event, reader.Value));
			}
		}
	}

	static void PopulateProperties(Release release, XmlReader reader)
	{
		var depth = reader.Depth;

		while(reader.Read() && reader.Depth > depth)
		{
			if(reader.NodeType == XmlNodeType.Element && reader.LocalName.Equals(PROPERTY_ELEMENT, StringComparison.OrdinalIgnoreCase))
			{
				var name = reader.GetAttribute(NAME_ATTRIBUTE);

				if(string.IsNullOrWhiteSpace(name))
					reader.Skip();

				if(reader.Read() && (reader.NodeType == XmlNodeType.Text || reader.NodeType == XmlNodeType.CDATA))
				{
					var type = reader.GetAttribute(TYPE_ATTRIBUTE);

					if(string.IsNullOrWhiteSpace(type))
						release.Properties.Add(name, reader.Value);
					else
						release.Properties.Add(name, Common.Convert.ConvertValue(reader.Value, TypeAlias.Parse(type)));
				}
			}
		}
	}

	static void PopulateAttributes(Release release, XmlReader reader)
	{
		for(int i = 0; i < reader.AttributeCount; i++)
		{
			reader.MoveToAttribute(i);

			switch(reader.LocalName)
			{
				case nameof(Release.Name):
					release.Name = reader.Value;
					break;
				case nameof(Release.Kind):
					release.Kind = Common.Convert.ConvertValue<ReleaseKind>(reader.Value);
					break;
				case nameof(Release.Title):
					release.Title = reader.Value;
					break;
				case nameof(Release.Edition):
					release.Edition = reader.Value;
					break;
				case nameof(Release.Version):
					release.Version = Version.Parse(reader.Value);
					break;
				case nameof(Release.Size):
					release.Size = uint.Parse(reader.Value);
					break;
				case nameof(Release.Path):
					release.Path = reader.Value;
					break;
				case nameof(Release.Checksum):
					release.Checksum = Checksum.Parse(reader.Value);
					break;
				case nameof(Release.Platform):
					release.Platform = Common.Convert.ConvertValue<Platform>(reader.Value);
					break;
				case nameof(Release.Architecture):
					release.Architecture = Common.Convert.ConvertValue<Architecture>(reader.Value);
					break;
				case nameof(Release.Deprecated):
					release.Deprecated = bool.Parse(reader.Value);
					break;
				case nameof(Release.Creation):
					release.Creation = DateTime.Parse(reader.Value);
					break;
			}
		}
	}

	private static XmlReaderSettings GetSettings(bool asynchronous) => new()
	{
		Async = asynchronous,
		IgnoreComments = true,
		IgnoreWhitespace = true,
		IgnoreProcessingInstructions = true,
	};
	#endregion
}

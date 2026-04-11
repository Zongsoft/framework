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
using System.Collections.Generic;

using Zongsoft.Common;

namespace Zongsoft.Upgrading;

partial class Release
{
	#region 私有常量
	private const string NAME_ATTRIBUTE = "name";
	private const string TYPE_ATTRIBUTE = "type";
	private const string EVENT_ATTRIBUTE = "event";

	private const string TAG_ELEMENT = "Tag";
	private const string TAGS_ELEMENT = "Tags";
	private const string EXECUTOR_ELEMENT = "Executor";
	private const string EXECUTORS_ELEMENT = "Executors";
	private const string PROPERTY_ELEMENT = "Property";
	private const string PROPERTIES_ELEMENT = "Properties";
	private const string RELEASE_ELEMENT = "Release";
	private const string RELEASES_ELEMENT = "Releases";
	#endregion

	#region 实例方法
	public void Save(string filePath)
	{
		ArgumentException.ThrowIfNullOrEmpty(filePath);
		using var writer = XmlWriter.Create(filePath);
		Write(writer, this);
	}

	public void Save(Stream stream)
	{
		ArgumentNullException.ThrowIfNull(stream);
		using var writer = XmlWriter.Create(stream);
		Write(writer, this);
	}
	#endregion

	#region 静态方法
	public static void Save(Stream stream, IEnumerable<Release> releases)
	{
		ArgumentNullException.ThrowIfNull(stream);
		ArgumentNullException.ThrowIfNull(releases);

		using var writer = XmlWriter.Create(stream);
		writer.WriteStartElement(RELEASES_ELEMENT);

		foreach(var release in releases)
			Write(writer, release);

		writer.WriteEndElement();
	}

	private static void Write(XmlWriter writer, Release release)
	{
		ArgumentNullException.ThrowIfNull(writer);
		ArgumentNullException.ThrowIfNull(release);

		writer.WriteStartElement(RELEASE_ELEMENT);

		writer.WriteAttributeString(nameof(release.Name), release.Name);
		writer.WriteAttributeString(nameof(release.Kind), release.Kind.ToString());
		writer.WriteAttributeString(nameof(release.Edition), release.Edition);
		writer.WriteAttributeString(nameof(release.Version), release.Version.ToString());
		writer.WriteAttributeString(nameof(release.Size), release.Size.ToString());
		writer.WriteAttributeString(nameof(release.Path), release.Path);
		writer.WriteAttributeString(nameof(release.Checksum), release.Checksum.ToString());
		writer.WriteAttributeString(nameof(release.Platform), release.Platform.ToString());
		writer.WriteAttributeString(nameof(release.Architecture), release.Architecture.ToString());
		writer.WriteAttributeString(nameof(release.Deprecated), release.Deprecated.ToString());
		writer.WriteAttributeString(nameof(release.Creation), release.Creation.ToString());

		if(!string.IsNullOrWhiteSpace(release.Title))
		{
			writer.WriteStartElement(nameof(release.Title));
			writer.WriteValue(release.Title);
			writer.WriteEndElement();
		}

		if(!string.IsNullOrWhiteSpace(release.Summary))
		{
			writer.WriteStartElement(nameof(release.Summary));
			writer.WriteValue(release.Summary);
			writer.WriteEndElement();
		}

		if(!string.IsNullOrWhiteSpace(release.Description))
		{
			writer.WriteStartElement(nameof(release.Description));
			writer.WriteValue(release.Description);
			writer.WriteEndElement();
		}

		if(release.Tags != null && release.Tags.Length > 0)
		{
			writer.WriteStartElement(TAGS_ELEMENT);

			foreach(var tag in release.Tags)
			{
				if(string.IsNullOrWhiteSpace(tag))
					continue;

				writer.WriteStartElement(TAG_ELEMENT);
				writer.WriteValue(tag);
				writer.WriteEndElement();
			}

			writer.WriteEndElement();
		}

		if(release.Properties != null && release.Properties.Count > 0)
		{
			writer.WriteStartElement(PROPERTIES_ELEMENT);

			foreach(var property in release.Properties)
			{
				if(string.IsNullOrEmpty(property.Key) || property.Value == null)
					continue;

				writer.WriteStartElement(PROPERTY_ELEMENT);
				writer.WriteAttributeString(NAME_ATTRIBUTE, property.Key);
				writer.WriteAttributeString(TYPE_ATTRIBUTE, property.Value.GetType().GetAlias());
				writer.WriteCData(Common.Convert.ConvertValue<string>(property.Value));
				writer.WriteEndElement();
			}

			writer.WriteEndElement();
		}

		if(release.Executors != null && release.Executors.Count > 0)
		{
			writer.WriteStartElement(EXECUTORS_ELEMENT);

			foreach(var executor in release.Executors)
			{
				if(string.IsNullOrEmpty(executor.Event) || string.IsNullOrEmpty(executor.Command))
					continue;

				writer.WriteStartElement(EXECUTOR_ELEMENT);
				writer.WriteAttributeString(EVENT_ATTRIBUTE, executor.Event);
				writer.WriteCData(executor.Command);
				writer.WriteEndElement();
			}

			writer.WriteEndElement();
		}

		writer.WriteEndElement();
	}
	#endregion
}

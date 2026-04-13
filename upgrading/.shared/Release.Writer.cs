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
	private const string KIND_ATTRIBUTE = "kind";
	private const string PATH_ATTRIBUTE = "path";
	private const string SIZE_ATTRIBUTE = "size";
	private const string EVENT_ATTRIBUTE = "event";
	private const string TITLE_ATTRIBUTE = "title";
	private const string EDITION_ATTRIBUTE = "edition";
	private const string VERSION_ATTRIBUTE = "version";
	private const string CREATION_ATTRIBUTE = "creation";
	private const string CHECKSUM_ATTRIBUTE = "checksum";
	private const string PLATFORM_ATTRIBUTE = "platform";
	private const string DEPRECATED_ATTRIBUTE = "deprecated";
	private const string ARCHITECTURE_ATTRIBUTE = "architecture";

	private const string TITLE_ELEMENT = "title";
	private const string SUMMARY_ELEMENT = "summary";
	private const string DESCRIPTION_ELEMENT = "description";

	private const string TAG_ELEMENT = "tag";
	private const string TAGS_ELEMENT = "tags";
	private const string EXECUTOR_ELEMENT = "executor";
	private const string EXECUTORS_ELEMENT = "executors";
	private const string PROPERTY_ELEMENT = "property";
	private const string PROPERTIES_ELEMENT = "properties";
	private const string RELEASE_ELEMENT = "release";
	private const string RELEASES_ELEMENT = "releases";
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

		writer.WriteAttributeString(NAME_ATTRIBUTE, release.Name);
		writer.WriteAttributeString(KIND_ATTRIBUTE, release.Kind.ToString());
		writer.WriteAttributeString(EDITION_ATTRIBUTE, release.Edition);
		writer.WriteAttributeString(VERSION_ATTRIBUTE, release.Version.ToString());
		writer.WriteAttributeString(SIZE_ATTRIBUTE, release.Size.ToString());
		writer.WriteAttributeString(PATH_ATTRIBUTE, release.Path);
		writer.WriteAttributeString(CHECKSUM_ATTRIBUTE, release.Checksum.ToString());
		writer.WriteAttributeString(PLATFORM_ATTRIBUTE, release.Platform.ToString());
		writer.WriteAttributeString(ARCHITECTURE_ATTRIBUTE, release.Architecture.ToString());
		writer.WriteAttributeString(DEPRECATED_ATTRIBUTE, release.Deprecated.ToString());
		writer.WriteAttributeString(CREATION_ATTRIBUTE, release.Creation.ToString());

		if(!string.IsNullOrWhiteSpace(release.Title))
		{
			writer.WriteStartElement(TITLE_ELEMENT);
			writer.WriteValue(release.Title);
			writer.WriteEndElement();
		}

		if(!string.IsNullOrWhiteSpace(release.Summary))
		{
			writer.WriteStartElement(SUMMARY_ELEMENT);
			writer.WriteValue(release.Summary);
			writer.WriteEndElement();
		}

		if(!string.IsNullOrWhiteSpace(release.Description))
		{
			writer.WriteStartElement(DESCRIPTION_ELEMENT);
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

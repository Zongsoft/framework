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
using System.Linq;
using System.Collections.Generic;

using Microsoft.OpenApi;

namespace Zongsoft.Web.OpenApi;

partial class DocumentGenerator
{
	private static ISet<OpenApiTag> GenerateTags(ControllerServiceDescriptorCollection descriptors, List<ControllerServiceDescriptor> services)
	{
		var tags = new Dictionary<string, OpenApiTag>(StringComparer.OrdinalIgnoreCase);

		foreach(var service in services.OrderBy(service => service.QualifiedName, StringComparer.Ordinal))
		{
			string parent = null;
			var module = service.Module;

			if(!string.IsNullOrEmpty(module))
				parent = Add($"module/{module}", module, null, null);

			if(!string.IsNullOrEmpty(service.Namespace))
			{
				string path = null;

				foreach(var segment in service.Namespace.Split(Type.Delimiter))
				{
					path = path == null ? segment : $"{path}{Type.Delimiter}{segment}";
					if(descriptors.TryGetValue(module, path, out var ancestor))
						parent = Add(ancestor.QualifiedName, ancestor.Title ?? ancestor.Name, ancestor.Description, parent);
					else
						parent = Add($"namespace/{module}:{path}", segment, null, parent);
				}
			}

			Add(service.QualifiedName, service.Title ?? service.Name, service.Description, parent);
		}

		var comparer = Comparer<OpenApiTag>.Create((left, right) => StringComparer.Ordinal.Compare(left.Name, right.Name));
		return new SortedSet<OpenApiTag>(tags.Values, comparer);

		string Add(string name, string caption, string description, string parent)
		{
			if(string.Equals(name, parent, StringComparison.OrdinalIgnoreCase))
				throw new InvalidOperationException(string.Format(Properties.Resources.TagConflict_Message, name, parent));

			if(tags.TryGetValue(name, out var existing))
			{
				if(!string.Equals(name, existing.Name, StringComparison.Ordinal) ||
				   !string.Equals(parent, existing.Parent?.Reference.Id, StringComparison.Ordinal))
					throw new InvalidOperationException(string.Format(Properties.Resources.TagConflict_Message, name, parent));

				return existing.Name;
			}

			var tag = Tags.Tag(name, parent).Caption(caption).Description(description);
			tag.Kind = "nav";
			tags.Add(name, tag);
			return tag.Name;
		}
	}
}

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

using System.IO;

using Microsoft.OpenApi;

namespace Zongsoft.Web.OpenApi;

partial class WebExtension
{
	// OpenAPI.NET 3.11 writes 3.2.0. Apply the 3.2.1 patch version to the root property only.
	private const string SPECIFICATION_VERSION = "3.2.1";

	private sealed class VersionedJsonWriter(TextWriter writer) : OpenApiJsonWriter(writer)
	{
		private bool _version;

		public override void WritePropertyName(string name)
		{
			_version = this.IsTopLevelScope() && name == "openapi";
			base.WritePropertyName(name);
		}

		public override void WriteValue(string value) => base.WriteValue(_version && value == "3.2.0" ? SPECIFICATION_VERSION : value);
	}

	private sealed class VersionedYamlWriter(TextWriter writer) : OpenApiYamlWriter(writer)
	{
		private bool _version;

		public override void WritePropertyName(string name)
		{
			_version = this.IsTopLevelScope() && name == "openapi";
			base.WritePropertyName(name);
		}

		public override void WriteValue(string value) => base.WriteValue(_version && value == "3.2.0" ? SPECIFICATION_VERSION : value);
	}
}

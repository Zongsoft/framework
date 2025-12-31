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
 * Copyright (C) 2010-2025 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Commands library.
 *
 * The Zongsoft.Commands is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Commands is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Commands library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Collections.Generic;

using Zongsoft.Components;
using Zongsoft.Serialization;

namespace Zongsoft.Commands;

[DisplayName("Text.JsonCommand.Name")]
[Description("Text.JsonCommand.Description")]
[CommandOption(DEPTH_OPTION, typeof(int), 3, "Text.JsonCommand.Options.Depth")]
[CommandOption(TYPED_OPTION, 't', Description = "Text.JsonCommand.Options.Typed")]
[CommandOption(INDENTED_OPTION, 'i', Description = "Text.JsonCommand.Options.Indented")]
[CommandOption(CASING_OPTION, typeof(SerializationNamingConvention), SerializationNamingConvention.None, "Text.JsonCommand.Options.Casing")]
public class JsonCommand : CommandBase<CommandContext>
{
	#region 常量定义
	private const string DEPTH_OPTION = "depth";
	private const string TYPED_OPTION = "typed";
	private const string CASING_OPTION = "casing";
	private const string INDENTED_OPTION = "indented";
	#endregion

	#region 构造函数
	public JsonCommand() : base("Json") { }
	public JsonCommand(string name) : base(name) { }
	#endregion

	#region 重写方法
	protected override async ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
	{
		var graph = context.Value;

		if(graph == null)
			return null;

		//如果输入参数是文本或流或文本读取器，则反序列化它并返回
		if(graph is string raw)
			return await Serializer.Json.DeserializeAsync<Dictionary<string, object>>(raw, cancellation);
		if(graph is System.Text.StringBuilder text)
			return await Serializer.Json.DeserializeAsync<Dictionary<string, object>>(text.ToString(), cancellation);
		if(graph is Stream stream)
			return await Serializer.Json.DeserializeAsync<Dictionary<string, object>>(stream, cancellation);

		var options = new TextSerializationOptions()
		{
			MaximumDepth = context.Options.GetValue<int>(DEPTH_OPTION),
			Typified = context.Options.Switch(TYPED_OPTION),
			Indented = context.Options.Switch(INDENTED_OPTION),
			NamingConvention = context.Options.GetValue<SerializationNamingConvention>(CASING_OPTION),
			IncludeFields = true,
		};

		var json = await Serializer.Json.SerializeAsync(graph, options, cancellation);

		if(json != null && !context.GetOptions().GetValue(KEY_QUIET_OPTION, true))
			context.Output.WriteLine(json);

		return json;
	}
	#endregion
}

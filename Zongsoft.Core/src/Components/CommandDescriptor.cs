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
using System.Reflection;

namespace Zongsoft.Components;

public class CommandDescriptor
{
	#region 私有构造
	private CommandDescriptor(Type commandType, CommandOptionDescriptorCollection options = null, ArgumentRequirement arguments = null)
	{
		this.CommandType = commandType ?? throw new ArgumentNullException(nameof(commandType));
		this.Options = options ?? new CommandOptionDescriptorCollection();
		this.Arguments = arguments;
	}
	#endregion

	#region 公共属性
	public Type CommandType { get; }
	public CommandOptionDescriptorCollection Options { get; }
	public ArgumentRequirement Arguments { get; set; }
	#endregion

	#region 静态方法
	private static readonly System.Collections.Concurrent.ConcurrentDictionary<Type, CommandDescriptor> _cache = new();
	public static CommandDescriptor Describe(Type commandType)
	{
		if(commandType == null)
			throw new ArgumentNullException(nameof(commandType));

		if(!typeof(ICommand).IsAssignableFrom(commandType))
			throw new ArgumentException(string.Format(Properties.Resources.Command_InvalidType_Message, commandType.FullName));

		return _cache.GetOrAdd(commandType, commandType =>
		{
			var options = new CommandOptionDescriptorCollection();
			var attributes = commandType.GetCustomAttributes<CommandOptionAttribute>(true);

			foreach(var attribute in attributes)
			{
				var option = new CommandOptionDescriptor(
					attribute.Name,
					attribute.Symbol,
					attribute.Required,
					attribute.Type,
					attribute.ConverterType)
				{
					Description = attribute.Description,
				};

				if(attribute.HasDefaultValue)
					option.DefaultValue = attribute.DefaultValue;

				options.Add(option);
			}

			return new CommandDescriptor(commandType, options);
		});
	}
	#endregion

	#region 嵌套子类
	public class ArgumentRequirement
	{
		public bool Required { get; set; }
		public int Minimum { get; set; }
		public int Maximum { get; set; }
	}
	#endregion
}

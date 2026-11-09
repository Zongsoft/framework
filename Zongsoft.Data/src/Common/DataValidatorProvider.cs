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
using System.Collections.Generic;
using System.Collections.Concurrent;

using Zongsoft.Services;

namespace Zongsoft.Data.Common;

[System.Reflection.DefaultMember(nameof(Validators))]
public class DataValidatorProvider : IDataValidatorProvider
{
	#region 单例字段
	public static readonly DataValidatorProvider Instance = new();
	#endregion

	#region 私有构造
	private DataValidatorProvider() { }
	#endregion

	#region 成员字段
	private readonly ConcurrentDictionary<string, IDataValidator> _cache = new(StringComparer.OrdinalIgnoreCase);
	private readonly List<IDataValidator> _validators = new();
	#endregion

	#region 公共属性
	public ICollection<IDataValidator> Validators => _validators;
	#endregion

	#region 公共方法
	public IDataValidator GetValidator(IDataAccessContextBase context) =>
		_cache.GetOrAdd(context.Name, (key, context) => this.Locate(context), context);
	#endregion

	#region 私有方法
	private IDataValidator Locate(IDataAccessContextBase context)
	{
		foreach(var validator in this.Validators)
		{
			switch(validator)
			{
				case IMatchable<IDataAccessContextBase> matchable:
					if(matchable.Match(context))
						return validator;

					break;
				case IMatchable<string> matchable:
					if(matchable.Match(GetNamespace(context)))
						return validator;

					break;
				default:
					var @namespace = GetNamespace(context);
					var module = ApplicationModuleAttribute.Find(validator.GetType())?.Name;

					if(string.IsNullOrEmpty(module))
					{
						if(string.IsNullOrEmpty(@namespace))
							return validator;
					}
					else
					{
						if(string.Equals(module, @namespace, StringComparison.OrdinalIgnoreCase))
							return validator;
					}
					break;
			}
		}

		return null;
	}

	private static string GetNamespace(IDataAccessContextBase context) => context switch
	{
		DataExistContextBase exist => exist.Entity.Namespace,
		DataImportContextBase import => import.Entity.Namespace,
		DataSelectContextBase select => select.Entity.Namespace,
		IDataMutateContextBase mutate => mutate.Entity.Namespace,
		DataExecuteContextBase execute => execute.Command.Namespace,
		DataAggregateContextBase aggregate => aggregate.Entity.Namespace,
		_ => null,
	};
	#endregion
}
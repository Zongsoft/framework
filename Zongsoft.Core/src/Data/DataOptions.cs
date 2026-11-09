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

namespace Zongsoft.Data;

public abstract class DataOptionsBase : IDataOptions
{
	#region 构造函数
	protected DataOptionsBase() => this.Parameters = new Collections.Parameters();
	protected DataOptionsBase(Collections.Parameters parameters) => this.Parameters = parameters ?? new Collections.Parameters();
	protected DataOptionsBase(IEnumerable<KeyValuePair<string, object>> parameters) => this.Parameters = new Collections.Parameters(parameters);
	#endregion

	#region 公共属性
	public Collections.Parameters Parameters { get; }
	#endregion
}

public abstract class DataOptionsBuilder<TOptions> : IDataOptionsBuilder<TOptions> where TOptions : IDataOptions
{
	protected DataOptionsBuilder() => this.Parameters = new Collections.Parameters();
	#region 公共属性
	public Collections.Parameters Parameters { get; }
	#endregion

	#region 公共方法
	public abstract TOptions Build();
	#endregion
	public static implicit operator TOptions(DataOptionsBuilder<TOptions> builder) => builder.Build();
}

public interface IDataMutateOptions : IDataOptions
{
	#region 公共属性
	/// <summary>获取或设置一个值，指示是否禁用当前数据访问操作的验证器，默认不禁用。</summary>
	bool ValidatorSuppressed { get; set; }

	/// <summary>获取或设置写操作的返回输出。</summary>
	Returning Returning { get; set; }
	#endregion

	#region 公共方法
	/// <summary>获取当前写操作是否指定了返回输出。</summary>
	/// <param name="returning">输出参数，获取到的返回输出信息。</param>
	/// <returns>如果返回真(<c>True</c>)则表示指定了返回输出，否则返回假(<c>False</c>)。</returns>
	bool HasReturning(out Returning returning);
	#endregion
}

public abstract class DataMutateOptions : DataOptionsBase, IDataMutateOptions
{
	#region 构造函数
	protected DataMutateOptions() { }
	protected DataMutateOptions(Collections.Parameters parameters) : base(parameters) { }
	protected DataMutateOptions(IEnumerable<KeyValuePair<string, object>> parameters) : base(parameters) { }
	#endregion

	#region 公共属性
	public bool ValidatorSuppressed { get; set; }
	public Returning Returning { get; set; }
	#endregion

	#region 公共方法
	public bool HasReturning(out Returning returning)
	{
		returning = this.Returning;
		return returning != null && !returning.Columns.IsEmpty;
	}
	#endregion
}

public abstract class DataMutateOptionsBuilder<TOptions> : DataOptionsBuilder<TOptions> where TOptions : IDataMutateOptions
{
	#region 公共属性
	/// <summary>获取或设置一个值，指示是否禁用当前数据访问操作的验证器，默认不禁用。</summary>
	public bool ValidatorSuppressed { get; set; }
	/// <summary>获取或设置写操作的返回输出。</summary>
	public Returning Returning { get; set; }
	#endregion
}

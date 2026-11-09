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

namespace Zongsoft.Data;

/// <summary>表示数据约束失败的异常类。</summary>
public class DataConstraintException : DataAccessException
{
	#region 构造函数
	public DataConstraintException(string driverName, int code, DataConstraintKind kind, string name, string value, string table, string[] fields, Exception innerException = null) : this(driverName, code, kind, name, value, table, fields, null, null, innerException) { }
	public DataConstraintException(string driverName, int code, DataConstraintKind kind, string name, string value, string table, string[] fields, string foreignTable, string[] foreignFields, Exception innerException = null) : base(driverName, code, Properties.Resources.DataConstraintException_Message, innerException)
	{
		this.Kind = kind;
		this.Name = name;
		this.Value = value;

		if(!string.IsNullOrEmpty(table))
			this.Principal = new Actor(table, fields);

		if(!string.IsNullOrEmpty(foreignTable))
			this.Foreigner = new Actor(foreignTable, foreignFields);
	}

	public DataConstraintException(string driverName, int code, DataConstraintKind kind, string name, string value, Actor principal, Exception innerException = null) : this(driverName, code, kind, name, value, principal, null, innerException) { }
	public DataConstraintException(string driverName, int code, DataConstraintKind kind, string name, string value, Actor principal, Actor foreigner, Exception innerException = null) : base(driverName, code, Properties.Resources.DataConstraintException_Message, innerException)
	{
		this.Kind = kind;
		this.Name = name;
		this.Value = value;
		this.Principal = principal;
		this.Foreigner = foreigner;
	}
	#endregion

	#region 公共属性
	/// <summary>获取数据约束的名称。</summary>
	public string Name { get; }
	/// <summary>获取数据约束冲突的值。</summary>
	public string Value { get; }
	/// <summary>获取数据约束的类型。</summary>
	public DataConstraintKind Kind { get; }
	/// <summary>获取数据约束的主表。</summary>
	public Actor Principal { get; }
	/// <summary>获取数据约束的外表。</summary>
	public Actor Foreigner { get; }
	#endregion

	public sealed class Actor
	{
		#region 构造函数
		public Actor(string name, params string[] fields)
		{
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			this.Name = name;
			this.Fields = fields ?? [];
		}
		#endregion

		#region 公共属性
		/// <summary>获取数据约束的表名。</summary>
		public string Name { get; }
		/// <summary>获取或设置数据约束的字段集。</summary>
		public string[] Fields { get; set; }
		#endregion

		#region 重写方法
		public bool Equals(Actor other) => other is not null && string.Equals(this.Name, other.Name);
		public override bool Equals(object obj) => this.Equals(obj as Actor);
		public override int GetHashCode() => this.Name.GetHashCode();
		public override string ToString() => this.Name;
		#endregion
	}
}

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
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Data library.
 *
 * The Zongsoft.Data is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;

namespace Zongsoft.Data.Common;

public class Feature : IEquatable<Feature>, IComparable<Feature>
{
	#region 单例字段
	/// <summary>表示多活动结果集(MARS)的特性（注：目前仅 Microsoft SQL Server 2005 及其以上版本支持）。</summary>
	/// <remarks>更多信息请参考微软文档：<see ref="https://docs.microsoft.com/zh-cn/dotnet/framework/data/adonet/sql/multiple-active-result-sets-mars" />。</remarks>
	public static readonly Feature MultipleActiveResultSets = new(nameof(MultipleActiveResultSets));

	/// <summary>表示不支持数据事务功能。</summary>
	public static readonly Feature TransactionSuppressed = new(nameof(TransactionSuppressed));
	#endregion

	#region 成员字段
	private readonly string _name;
	private readonly Version _version;
	#endregion

	#region 构造函数
	public Feature(string name)
	{
		if(string.IsNullOrWhiteSpace(name))
			throw new ArgumentNullException(nameof(name));

		_name = name.Trim().ToLowerInvariant();
	}

	public Feature(string name, Version version)
	{
		if(string.IsNullOrWhiteSpace(name))
			throw new ArgumentNullException(nameof(name));

		_name = name.Trim().ToLowerInvariant();

		if(version != null && !(version.Major == 0 && version.Minor == 0 && version.Build <= 0 && version.Revision <= 0))
			_version = version;
	}
	#endregion

	#region 公共属性
	/// <summary>获取功能特性的名称。</summary>
	public string Name => _name;

	/// <summary>获取功能特性的版本号。</summary>
	public Version Version => _version;
	#endregion

	#region 重写方法
	public int CompareTo(Feature other)
	{
		if(other == null)
			return -1;

		if(other._name != this._name)
			return -1;

		if(_version == null)
			return other._version == null ? 0 : -1;
		else
			return _version.CompareTo(other._version);
	}

	public bool Equals(Feature other) => other is not null && string.Equals(_name, other._name) && _version == other._version;
	public override bool Equals(object obj) => obj is Feature other && this.Equals(other);
	public override int GetHashCode() => HashCode.Combine(_name, _version);
	public override string ToString() => _version == null ? _name : $"{_name}@{_version}";
	#endregion

	#region 嵌套子类
	/// <summary>
	/// 表示删除语句的功能特性集。
	/// </summary>
	public static class Deletion
	{
		#region 常量定义
		private const string FEATURE_DELETE_PREFIX = "DELETE:";
		#endregion

		#region 公共字段
		/// <summary>表示删除语句中“多表删除”的功能特性。</summary>
		public static readonly Feature Multitable = new(FEATURE_DELETE_PREFIX + nameof(Multitable));

		/// <summary>表示删除语句中“输出子句”的功能特性。</summary>
		public static readonly Feature Outputting = new(FEATURE_DELETE_PREFIX + nameof(Outputting));
		#endregion
	}

	/// <summary>
	/// 表示更新语句的功能特性集。
	/// </summary>
	public static class Updation
	{
		#region 常量定义
		private const string FEATURE_UPDATE_PREFIX = "UPDATE:";
		#endregion

		#region 公共字段
		/// <summary>表示更新语句中“多表更新”的功能特性。</summary>
		public static readonly Feature Multitable = new(FEATURE_UPDATE_PREFIX + nameof(Multitable));

		/// <summary>表示更新语句中“输出子句”的功能特性。</summary>
		public static readonly Feature Outputting = new(FEATURE_UPDATE_PREFIX + nameof(Outputting));
		#endregion
	}
	#endregion
}

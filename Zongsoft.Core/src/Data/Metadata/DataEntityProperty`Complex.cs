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
 * This file is part of Zongsoft.Core library.
 *
 * The Zongsoft.Core is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Core is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Core library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;

namespace Zongsoft.Data.Metadata;

public class DataEntityComplexProperty : DataEntityPropertyBase, IDataEntityComplexProperty
{
	#region 成员字段
	private IDataEntity _foreign;
	private IDataEntityProperty _foreignProperty;
	#endregion

	#region 构造函数
	public DataEntityComplexProperty(IDataEntity entity, string name, string port, DataEntityComplexPropertyBehaviors behaviors = DataEntityComplexPropertyBehaviors.None) : this(entity, name, port, true, behaviors) { }
	public DataEntityComplexProperty(IDataEntity entity, string name, string port, bool immutable, DataEntityComplexPropertyBehaviors behaviors = DataEntityComplexPropertyBehaviors.None) : base(entity, name, immutable)
	{
		if(string.IsNullOrWhiteSpace(port))
			throw new ArgumentNullException(nameof(port));

		this.Port = port.Trim();
		this.Behaviors = behaviors;
	}

	public DataEntityComplexProperty(IDataEntity entity, string name, string port, bool immutable, DataAssociationMultiplicity multiplicity, params DataAssociationLink[] links) : this(entity, name, port, immutable, DataEntityComplexPropertyBehaviors.None, multiplicity, links) { }
	public DataEntityComplexProperty(IDataEntity entity, string name, string port, DataEntityComplexPropertyBehaviors behaviors, DataAssociationMultiplicity multiplicity, params DataAssociationLink[] links) : this(entity, name, port, true, behaviors, multiplicity, links) { }
	public DataEntityComplexProperty(IDataEntity entity, string name, string port, bool immutable, DataEntityComplexPropertyBehaviors behaviors, DataAssociationMultiplicity multiplicity, params DataAssociationLink[] links) : base(entity, name, immutable)
	{
		if(string.IsNullOrWhiteSpace(port))
			throw new ArgumentNullException(nameof(port));

		this.Port = port.Trim();
		this.Behaviors = behaviors;
		this.Multiplicity = multiplicity;
		this.Links = links;
	}
	#endregion

	#region 公共属性
	public IDataEntity Foreign
	{
		get
		{
			if(_foreign == null)
				this.UpdateForeign();

			return _foreign;
		}
	}

	public IDataEntityProperty ForeignProperty
	{
		get
		{
			if(_foreign == null)
				this.UpdateForeign();

			return _foreignProperty;
		}
	}

	public string Port { get; }
	public DataEntityComplexPropertyBehaviors Behaviors { get; set; }
	public DataAssociationMultiplicity Multiplicity { get; set; }
	public DataAssociationLink[] Links { get; set; }
	public DataAssociationConstraint[] Constraints { get; set; }
	#endregion

	#region 重写属性
	/// <summary>获取一个值，指示数据实体属性是否为复合类型。该重写方法始终返回真(True)。</summary>
	public override bool IsComplex => true;

	/// <summary>获取一个值，指示数据实体属性是否为单值类型。该重写方法始终返回假(False)。</summary>
	public override bool IsSimplex => false;
	#endregion

	#region 重写方法
	public override string ToString()
	{
		var text = new System.Text.StringBuilder();

		foreach(var link in this.Links)
		{
			if(text.Length > 0)
				text.Append(" AND ");

			text.Append(link.ToString());
		}

		return $"{this.Name} -> {this.Port} ({text.ToString()})";
	}
	#endregion

	#region 私有方法
	private void UpdateForeign()
	{
		var port = this.Port;
		var index = port.IndexOf(':');

		if(index < 0)
		{
			_foreign = this.Entity.GetEntity(port);
		}
		else
		{
			_foreign = this.Entity.GetEntity(port[..index]);

			if(_foreign != null)
				_foreignProperty = _foreign.Properties[port[(index + 1)..]];
		}
	}
	#endregion
}

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

namespace Zongsoft.Data.Metadata;

/// <summary>表示数据实体关联成员的元数据类。</summary>
public class DataAssociationLink
{
	#region 成员字段
	private IDataEntityComplexProperty _owner;
	private IDataEntitySimplexProperty _foreignKey;
	private readonly string _foreign;
	private readonly string _anchor;
	#endregion

	#region 构造函数
	public DataAssociationLink(IDataEntityComplexProperty owner, string foreign, string anchor = null)
	{
		if(string.IsNullOrEmpty(foreign))
			throw new ArgumentNullException(nameof(foreign));

		_owner = owner ?? throw new ArgumentNullException(nameof(owner));
		_foreign = foreign;
		_anchor = string.IsNullOrEmpty(anchor) ? foreign : anchor;
		_foreignKey = null;
	}
	#endregion

	#region 公共属性
	/// <summary>获取关联元素的外键属性。</summary>
	public IDataEntitySimplexProperty ForeignKey => _foreignKey ??= (IDataEntitySimplexProperty)_owner.Foreign.Properties[_foreign];

	/// <summary>获取关联元素的外键属性名。</summary>
	public string Foreign => _foreign;

	/// <summary>获取关联元素的锚点。</summary>
	public string Anchor => _anchor;
	#endregion

	#region 公共方法
	public IDataEntityProperty[] GetAnchors()
	{
		var parts = _anchor.Split('.');
		var result = new IDataEntityProperty[parts.Length];
		var entity = _owner.Entity;
		IDataEntityProperty property;

		for(int i = 0; i < parts.Length - 1; i++)
		{
			if(entity.Properties.TryGetValue(parts[i], out property) && property.IsComplex)
			{
				result[i] = property;
				entity = ((IDataEntityComplexProperty)property).Foreign;
			}
			else
				throw new DataException(string.Format(Properties.Resources.DataAssociation_InvalidAnchor_Message, _anchor, _owner));
		}

		if(entity.Properties.TryGetValue(parts[^1], out property) && property.IsSimplex)
			result[^1] = property;
		else
			throw new DataException(string.Format(Properties.Resources.DataAssociation_InvalidAnchor_Message, _anchor, _owner));

		return result;
	}
	#endregion

	#region 重写方法
	public override string ToString() => string.IsNullOrEmpty(_anchor) ? _foreign : $"{_anchor}->{_foreign}";
	#endregion
}

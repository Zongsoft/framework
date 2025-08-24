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
 * Copyright (C) 2025-2025 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Intelligences library.
 *
 * The Zongsoft.Intelligences is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Intelligences is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Intelligences library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;

using Zongsoft.Services;
using Zongsoft.Resources;

namespace Zongsoft.Intelligences;

public class Assistant : IAssistant
{
	#region 成员字段
	private string _description;
	#endregion

	#region 构造函数
	public Assistant(string name, string driver, string description = null)
	{
		this.Name = name ?? throw new ArgumentNullException(nameof(name));
		this.Driver = driver ?? throw new ArgumentNullException(nameof(driver));
		this.Description = description;
	}
	#endregion

	#region 公共属性
	public string Name { get; }
	public string Driver { get; }
	public IChatService Chatting { get; init; }
	public IModelService Modeling { get; init; }
	public string Description
	{
		get => _description ?? ResourceUtility.GetResourceString(this.Chatting?.GetType() ?? this.Modeling?.GetType() ?? this.GetType(), $"{this.Driver}.{nameof(this.Description)}");
		set => _description = value;
	}
	#endregion

	#region 显式属性
	IChatService IServiceAccessor<IChatService>.Value => this.Chatting;
	IModelService IServiceAccessor<IModelService>.Value => this.Modeling;
	#endregion
}

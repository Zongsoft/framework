/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
 *
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
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

namespace Zongsoft.Transitions
{
	public abstract class State
	{
		#region 成员字段
		private DateTime? _timestamp;
		private string _description;
		private IStateDiagram _diagram;
		#endregion

		#region 构造函数
		protected State(IStateDiagram diagram)
		{
			if(diagram == null)
				throw new ArgumentNullException(nameof(diagram));

			_diagram = diagram;
			_timestamp = DateTime.Now;
			_description = null;
		}

		protected State(IStateDiagram diagram, string description)
		{
			if(diagram == null)
				throw new ArgumentNullException(nameof(diagram));

			_diagram = diagram;
			_timestamp = DateTime.Now;
			_description = description;
		}

		protected State(IStateDiagram diagram, DateTime? timestamp, string description = null)
		{
			if(diagram == null)
				throw new ArgumentNullException(nameof(diagram));

			_diagram = diagram;
			_timestamp = timestamp;
			_description = description;
		}
		#endregion

		#region 公共属性
		public IStateDiagram Diagram
		{
			get
			{
				return _diagram;
			}
			protected set
			{
				if(value == null)
					throw new ArgumentNullException();

				_diagram = value;
			}
		}

		public DateTime? Timestamp
		{
			get
			{
				return _timestamp;
			}
			set
			{
				_timestamp = value;
			}
		}

		public string Description
		{
			get
			{
				return _description;
			}
			set
			{
				_description = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
			}
		}
		#endregion

		#region 抽象成员
		internal protected abstract bool Match(State state);
		#endregion
	}
}

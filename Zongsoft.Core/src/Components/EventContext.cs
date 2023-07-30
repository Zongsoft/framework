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
using System.Collections.Generic;

namespace Zongsoft.Components
{
	public class EventContext
	{
		#region 成员字段
		private IDictionary<string, object> _parameters;
		#endregion

		#region 构造函数
		public EventContext(EventRegistryBase registry, string name, IEnumerable<KeyValuePair<string, object>> parameters = null)
		{
			this.Registry = registry ?? throw new ArgumentNullException(nameof(registry));
			this.Name = name;
			this.QualifiedName = string.IsNullOrEmpty(registry.Name) ? name : $"{registry.Name}:{name}";

			if(parameters != null)
			{
				_parameters = parameters is IDictionary<string, object> dictionary ?
					dictionary :
					new Dictionary<string, object>(parameters, StringComparer.OrdinalIgnoreCase);
			}
		}
		#endregion

		#region 公共属性
		[System.Text.Json.Serialization.JsonIgnore]
		[Serialization.SerializationMember(Ignored = true)]
		public EventRegistryBase Registry { get; }
		public string Name { get; }
		public string QualifiedName { get; }

		[System.Text.Json.Serialization.JsonIgnore]
		[Serialization.SerializationMember(Ignored = true)]
		public bool HasParameters { get => _parameters != null; }
		public IDictionary<string, object> Parameters
		{
			get
			{
				if(_parameters == null)
					System.Threading.Interlocked.CompareExchange(ref _parameters, new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase), null);

				return _parameters;
			}
		}
		#endregion
	}

	public class EventContext<TArgument> : EventContext
	{
		#region 构造函数
		public EventContext(EventRegistryBase registry, string name, TArgument argument, IEnumerable<KeyValuePair<string, object>> parameters = null) : base(registry, name, parameters)
		{
			this.Argument = argument;
		}
		#endregion

		#region 公共属性
		public TArgument Argument { get; set; }
		#endregion
	}
}

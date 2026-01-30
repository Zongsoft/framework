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
using System.Collections;
using System.Collections.Generic;

namespace Zongsoft.Data.Metadata;

public class DataCommandScriptor(IDataCommand command) : IDataCommandScriptor
{
	private readonly IDataCommand _command = command ?? throw new ArgumentNullException(nameof(command));
	private readonly Dictionary<string, string> _scripts = new(StringComparer.OrdinalIgnoreCase);

	protected IDataCommand Command => _command;
	public IReadOnlySet<string> Drivers => new DriverCollection(_scripts);
	public string GetScript(string driver) => driver != null && _scripts.TryGetValue(driver, out var script) ? script : null;
	public bool SetScript(string driver, string text)
	{
		if(string.IsNullOrEmpty(driver))
			return false;

		_scripts[driver] = text;
		return true;
	}

	private sealed class DriverCollection(Dictionary<string, string> scripts) : IReadOnlySet<string>
	{
		private readonly Dictionary<string, string> _scripts = scripts;

		public int Count => _scripts.Count;
		public bool Contains(string name) => _scripts.ContainsKey(name);
		public IEnumerator<string> GetEnumerator() => _scripts.Keys.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => _scripts.Keys.GetEnumerator();

		bool IReadOnlySet<string>.IsProperSubsetOf(IEnumerable<string> other) => (new HashSet<string>(_scripts.Keys, _scripts.Comparer)).IsProperSubsetOf(other);
		bool IReadOnlySet<string>.IsProperSupersetOf(IEnumerable<string> other) => (new HashSet<string>(_scripts.Keys, _scripts.Comparer)).IsProperSupersetOf(other);
		bool IReadOnlySet<string>.IsSubsetOf(IEnumerable<string> other) => (new HashSet<string>(_scripts.Keys, _scripts.Comparer)).IsSubsetOf(other);
		bool IReadOnlySet<string>.IsSupersetOf(IEnumerable<string> other) => (new HashSet<string>(_scripts.Keys, _scripts.Comparer)).IsSupersetOf(other);
		bool IReadOnlySet<string>.Overlaps(IEnumerable<string> other) => (new HashSet<string>(_scripts.Keys, _scripts.Comparer)).Overlaps(other);
		bool IReadOnlySet<string>.SetEquals(IEnumerable<string> other) => (new HashSet<string>(_scripts.Keys, _scripts.Comparer)).SetEquals(other);
	}
}

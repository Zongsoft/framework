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
 * Copyright (C) 2020-2026 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Hardwares library.
 *
 * The Zongsoft.Hardwares is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Hardwares is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Hardwares library. If not, see <http://www.gnu.org/licenses/>.
 */

namespace Zongsoft.Hardwares;

internal sealed class UniqueHardware : IO.Hardwares.Hardware
{
	private readonly string _identifier;

	public UniqueHardware(
		string identifier,
		string name,
		string code,
		string type,
		string model,
		string serie,
		string category,
		IO.Hardwares.IHardwareDriver driver = null,
		System.Collections.Generic.IEnumerable<IO.Hardwares.HardwareProperty> properties = null,
		System.Collections.Generic.IEnumerable<IO.Hardwares.HardwareComponent> components = null) : base(name, code, type, model, serie, category, driver, properties, components)
	{
		_identifier = HardwareUtility.Normalize(identifier);
	}

	public override bool HasUnique(out string identifier)
	{
		identifier = _identifier;
		return !string.IsNullOrEmpty(identifier);
	}
}

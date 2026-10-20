using System;
using System.Collections.Generic;

namespace Zongsoft.Tests;

public struct PropertyToken<T>(int ordinal, Func<T, object> getter, Action<T, object> setter)
{
	public readonly int Ordinal = ordinal;
	public readonly Func<T, object> Getter = getter;
	public readonly Action<T, object> Setter = setter;
}

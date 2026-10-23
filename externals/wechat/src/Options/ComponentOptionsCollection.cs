using System;
using System.Collections.ObjectModel;

namespace Zongsoft.Externals.Wechat.Options;

public class ComponentOptionsCollection() : KeyedCollection<string, ComponentOptions>(StringComparer.Ordinal)
{
	protected override string GetKeyForItem(ComponentOptions item) => item.Name;
}

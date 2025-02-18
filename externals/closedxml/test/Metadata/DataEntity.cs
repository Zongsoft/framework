using System;

namespace Zongsoft.Data.Metadata;

internal class DataEntity(string @namespace, string name, bool immutable = false) : DataEntityBase(@namespace, name, null, immutable)
{
	public DataEntity(string name, params IDataEntityProperty[] properties) : this(null, name)
	{
		if(properties != null && properties.Length > 0)
		{
			foreach(var property in properties)
				this.Properties.Add(property);
		}
	}

	public new void SetKey(params ReadOnlySpan<string> keys) => base.SetKey(keys);
}

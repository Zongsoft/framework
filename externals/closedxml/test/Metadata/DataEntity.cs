using System;
using System.Collections.Generic;

namespace Zongsoft.Data.Metadata;

internal class DataEntity : DataEntityBase
{
	public DataEntity(string name, params IDataEntityProperty[] properties) : this(null, name)
	{
		if(properties != null && properties.Length > 0)
		{
			foreach(var property in properties)
				this.Properties.Add(property);
		}
	}

    public DataEntity(string @namespace, string name, bool immutable = false) : base(@namespace, name, null, immutable)
	{
		this.Properties = new DataEntityPropertyCollection(this);
	}
}

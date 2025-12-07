using System;
using System.Linq;

using Zongsoft.Common;

namespace Zongsoft.Data.Tests.Models;

public class AddressConditionConverter : ConditionConverter
{
	public override ICondition Convert(ConditionConverterContext context)
	{
		if(context.Value == null)
			return null;

		static ICondition GetCondition(string name, uint id)
		{
			if(id == 0)
				return Condition.Equal(name, 0u);

			return Zongsoft.Data.Range.Create((HierarchyVector32)id).ToCondition(name);
		}

		if(context.Names.Length == 1)
			return GetCondition(context.GetFullName(), Zongsoft.Common.Convert.ConvertValue<uint>(context.Value));

		return ConditionCollection.Or(context.Names.Select(name => GetCondition(context.GetFullName(name), Zongsoft.Common.Convert.ConvertValue<uint>(context.Value))));
	}
}

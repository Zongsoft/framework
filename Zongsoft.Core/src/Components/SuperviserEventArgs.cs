using System;

namespace Zongsoft.Components;

public class SuperviserEventArgs<T> : EventArgs
{
	#region 构造函数
	public SuperviserEventArgs(IObservable<T> observable)
	{
		this.Key = null;
		this.Observable = observable;
	}

	public SuperviserEventArgs(object key, IObservable<T> observable)
	{
		this.Key = key;
		this.Observable = observable;
	}
	#endregion

	#region 公共属性
	public object Key { get; }
	public IObservable<T> Observable { get; }
	#endregion
}

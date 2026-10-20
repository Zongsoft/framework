using System;
using System.Linq;
using System.Collections.Generic;

using Xunit;

namespace Zongsoft.Collections.Tests;

public class QueueTest
{
	[Fact]
	public void Test()
	{
		var queue = new Queue(32);

		Assert.Equal(32, queue.Capacity);
		Assert.Empty(queue);
	}

	[Fact]
	public void TestClear()
	{
		var queue = new Queue();

		Assert.Empty(queue);

		for(int i = 0; i < 100; i++)
		{
			queue.Enqueue(i.ToString());
		}

		Assert.Equal(100, queue.Count);
	}

	[Fact]
	public void TestTrimToSize()
	{
		var queue = new Queue(64);

		Assert.Empty(queue);
		Assert.Equal(64, queue.Capacity);

		queue.Enqueue("No.1");
		queue.Enqueue("No.2");

		Assert.Equal(2, queue.Count);
		Assert.Equal(64, queue.Capacity);

		queue.TrimToSize();

		Assert.Equal(2, queue.Count);
		Assert.Equal(2, queue.Capacity);
	}

	[Fact]
	public void TestToArray()
	{
		var queue = new Queue();
		var array = queue.ToArray();

		Assert.Empty(array);

		queue.Enqueue("No.1");
		queue.Enqueue("No.2");
		queue.Enqueue("No.3");

		array = queue.ToArray();
		Assert.Equal(3, array.Length);
	}

	[Fact]
	public void TestDequeue()
	{
		var queue = new Queue(100);

		for(int i = 0; i < 100; i++)
		{
			queue.Enqueue("No." + (i + 1).ToString());
		}

		var result = queue.Dequeue();
		Assert.Equal("No.1", result);
		result = queue.Dequeue();
		Assert.Equal("No.2", result);

		var index = 3;
		var items = queue.DequeueMany(8);

		foreach(var item in items)
		{
			Assert.Equal("No." + (index++).ToString(), item);
		}
	}

	[Fact]
	public void TestEnqueue()
	{
		var queue = new Queue();

		Assert.Empty(queue);

		queue.Enqueue("No.1");
		queue.Enqueue("No.2");
		queue.Enqueue("No.3");

		Assert.Equal(3, queue.Count);

		queue.Enqueue(1);
		queue.Enqueue(DateTime.Now);
		queue.Enqueue(Guid.NewGuid());

		Assert.Equal(6, queue.Count);

		queue.EnqueueMany(new object[] { "xyz", new Zongsoft.Tests.Person(), 123 });

		Assert.Equal(9, queue.Count);
	}

	[Fact]
	public void TestPeek()
	{
		var queue = new Queue();

		for(int i = 0; i < 100; i++)
		{
			queue.Enqueue(i.ToString());
		}

		Assert.Equal(100, queue.Count);
		Assert.Equal("0", queue.Peek());
		Assert.Equal("0", queue.Peek());
		Assert.Equal("0", queue.Peek());
		Assert.Equal(100, queue.Count);

		var items = queue.Take(0, 10);
		var index = 0;

		Assert.Equal(100, queue.Count);

		foreach(var item in items)
		{
			Assert.Equal(index++.ToString(), item);
		}

		Assert.Equal(10, index);
	}

	[Fact]
	public void TestTake()
	{
		var queue = new Queue();

		for(int i = 0; i < 100; i++)
		{
			queue.Enqueue(i.ToString());
		}

		Assert.Equal(100, queue.Count);
		Assert.Equal("0", queue.Take(0));
		Assert.Equal("1", queue.Take(1));
		Assert.Equal("2", queue.Take(2));
		Assert.Equal(100, queue.Count);

		var items = queue.Take(10, 10);
		var index = 10;

		Assert.Equal(100, queue.Count);

		foreach(var item in items)
		{
			Assert.Equal(index++.ToString(), item);
		}

		Assert.Equal(20, index);
	}

	[Fact]
	public void TestDequeueManyOnWrappedFullQueue()
	{
		var queue = new Queue(4);

		queue.Enqueue("A");
		queue.Enqueue("B");
		queue.Enqueue("C");
		queue.Enqueue("D");

		//移除前两个元素，使队头后移
		Assert.Equal("A", queue.Dequeue());
		Assert.Equal("B", queue.Dequeue());

		//继续入队使队列重新填满并发生回绕(Head == Tail)
		queue.Enqueue("E");
		queue.Enqueue("F");

		Assert.Equal(4, queue.Count);

		//出队数量超出可用元素数，应忽略该参数值而应用可用的元素数
		var items = queue.DequeueMany(10).Cast<object>().ToArray();

		Assert.Equal(4, items.Length);
		Assert.Equal(new object[] { "C", "D", "E", "F" }, items);
		Assert.Empty(queue);
	}

	[Fact]
	public void TestTakeWithOffsetBeyondSize()
	{
		var queue = new Queue(4);

		queue.Enqueue("A");
		queue.Enqueue("B");
		queue.Enqueue("C");

		//偏移值未超出可用元素数时返回对应位置的元素
		Assert.Equal("C", queue.Take(2));

		//偏移值超出可用元素数时返回最后一个元素
		Assert.Equal("C", queue.Take(3));
		Assert.Equal(3, queue.Count);

		//批量获取的偏移值超出可用元素数时应返回空集合
		Assert.Empty(queue.Take(3, 5));
		Assert.Equal(3, queue.Count);
	}

	[Fact]
	public void TestGetEnumerator()
	{
		var queue = new Queue();

		for(int i = 0; i < 100; i++)
		{
			queue.Enqueue(i.ToString());
		}

		int index = 0;

		foreach(var item in queue)
		{
			Assert.Equal(index++.ToString(), item);
		}
	}

	[Fact]
	public void TestCopyTo()
	{
		var queue = new Queue();

		for(int i = 0; i < 100; i++)
		{
			queue.Enqueue(i.ToString());
		}

		var array = new object[queue.Count];
		queue.CopyTo(array, 0);

		Assert.Equal(queue.Count, array.Length);

		for(int i = 0; i < array.Length; i++)
		{
			Assert.Equal(i.ToString(), array[i].ToString());
		}
	}
}

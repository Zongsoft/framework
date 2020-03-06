using System;
using System.Collections.Generic;

using Xunit;

namespace Zongsoft.Collections.Tests
{
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
}

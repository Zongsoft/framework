using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

namespace Zongsoft.Components.Tests
{
	public class EventDescriptorTest
	{
		private MyEventTrigger _trigger = new MyEventTrigger();

		[Fact]
		public void TestEventHandler1()
		{
			var name = nameof(_trigger.MyEvent);
			var descriptor = new EventDescriptor<EventArgs>(name);
			Assert.Equal(name, descriptor.Name);
			Assert.Null(descriptor.Title);
			Assert.Null(descriptor.Description);
			Assert.Empty(descriptor.Handlers);

			//Bind
			descriptor.Bind(_trigger, nameof(_trigger.MyEvent));
			descriptor.Handlers.Add(new MyEventHandler1());
			Assert.Equal(1, descriptor.Handlers.Count);
			Assert.Throws<MyHandlerException>(_trigger.OnMyEvent);

			//Unbind
			descriptor.Unbind(_trigger, nameof(_trigger.MyEvent));
			Assert.False(_trigger.HasMyEvent);
		}

		[Fact]
		public void TestEventHandler2()
		{
			var name = nameof(_trigger.MyEventWithArgs);
			var descriptor = new EventDescriptor<MyEventArgs>(name);
			Assert.Equal(name, descriptor.Name);
			Assert.Null(descriptor.Title);
			Assert.Null(descriptor.Description);
			Assert.Empty(descriptor.Handlers);

			//Bind
			descriptor.Bind(_trigger, nameof(_trigger.MyEventWithArgs));
			descriptor.Handlers.Add(new MyEventHandler2());
			Assert.Equal(1, descriptor.Handlers.Count);
			Assert.Throws<MyHandlerException>(_trigger.OnMyEventWithArgs);

			//Unbind
			descriptor.Unbind(_trigger, nameof(_trigger.MyEventWithArgs));
			Assert.False(_trigger.HasMyEventWithArgs);
		}

		[Fact]
		public void TestAction()
		{
			var name = nameof(_trigger.Action0);
			var descriptor = new EventDescriptor(name);
			Assert.Equal(name, descriptor.Name);
			Assert.Null(descriptor.Title);
			Assert.Null(descriptor.Description);
			Assert.Empty(descriptor.Handlers);

			//Bind
			descriptor.Bind(_trigger, nameof(_trigger.Action0));
			Assert.NotNull(_trigger.Action0);

			descriptor.Handlers.Add(new MyHandlerGeneric());
			Assert.Equal(1, descriptor.Handlers.Count);
			Assert.Throws<MyHandlerException>(_trigger.OnAction0);

			//Unbind
			descriptor.Unbind(_trigger, nameof(_trigger.Action0));
			Assert.Null(_trigger.Action0);
		}

		[Fact]
		public void TestAction1_Object()
		{
			var name = nameof(_trigger.Action1_Object);
			var descriptor = new EventDescriptor<object>(name);
			Assert.Equal(name, descriptor.Name);
			Assert.Null(descriptor.Title);
			Assert.Null(descriptor.Description);
			Assert.Empty(descriptor.Handlers);

			//Bind
			descriptor.Bind(_trigger, nameof(_trigger.Action1_Object));
			Assert.NotNull(_trigger.Action1_Object);

			descriptor.Handlers.Add(new MyHandlerObject());
			Assert.Equal(1, descriptor.Handlers.Count);
			Assert.Throws<MyHandlerException>(_trigger.OnAction1_Object);

			//Unbind
			descriptor.Unbind(_trigger, nameof(_trigger.Action1_Object));
			Assert.Null(_trigger.Action1_Object);
		}

		[Fact]
		public void TestAction1_Generic()
		{
			var name = nameof(_trigger.Action1_Generic);
			var descriptor = new EventDescriptor<MyRequest>(name);
			Assert.Equal(name, descriptor.Name);
			Assert.Null(descriptor.Title);
			Assert.Null(descriptor.Description);
			Assert.Empty(descriptor.Handlers);

			//Bind
			descriptor.Bind(_trigger, nameof(_trigger.Action1_Generic));
			Assert.NotNull(_trigger.Action1_Generic);

			descriptor.Handlers.Add(new MyHandlerGeneric());
			Assert.Equal(1, descriptor.Handlers.Count);
			Assert.Throws<MyHandlerException>(_trigger.OnAction1_Generic);

			//Unbind
			descriptor.Unbind(_trigger, nameof(_trigger.Action1_Generic));
			Assert.Null(_trigger.Action1_Generic);
		}

		[Fact]
		public void TestAction2_Object()
		{
			var name = nameof(_trigger.Action2_Object);
			var descriptor = new EventDescriptor<object>(name);
			Assert.Equal(name, descriptor.Name);
			Assert.Null(descriptor.Title);
			Assert.Null(descriptor.Description);
			Assert.Empty(descriptor.Handlers);

			//Bind
			descriptor.Bind(_trigger, nameof(_trigger.Action2_Object));
			Assert.NotNull(_trigger.Action2_Object);

			descriptor.Handlers.Add(new MyHandlerObject());
			Assert.Equal(1, descriptor.Handlers.Count);
			Assert.Throws<MyHandlerException>(_trigger.OnAction2_Object);

			//Unbind
			descriptor.Unbind(_trigger, nameof(_trigger.Action2_Object));
			Assert.Null(_trigger.Action2_Object);
		}

		[Fact]
		public void TestAction2_Generic()
		{
			var name = nameof(_trigger.Action2_Generic);
			var descriptor = new EventDescriptor<MyRequest>(name);
			Assert.Equal(name, descriptor.Name);
			Assert.Null(descriptor.Title);
			Assert.Null(descriptor.Description);
			Assert.Empty(descriptor.Handlers);

			//Bind
			descriptor.Bind(_trigger, nameof(_trigger.Action2_Generic));
			Assert.NotNull(_trigger.Action2_Generic);

			descriptor.Handlers.Add(new MyHandlerGeneric());
			Assert.Equal(1, descriptor.Handlers.Count);
			Assert.Throws<MyHandlerException>(_trigger.OnAction2_Generic);

			//Unbind
			descriptor.Unbind(_trigger, nameof(_trigger.Action2_Generic));
			Assert.Null(_trigger.Action2_Generic);
		}

		[Fact]
		public void TestAction3_Object()
		{
			var name = nameof(_trigger.Action3_Object);
			var descriptor = new EventDescriptor<object>(name);
			Assert.Equal(name, descriptor.Name);
			Assert.Null(descriptor.Title);
			Assert.Null(descriptor.Description);
			Assert.Empty(descriptor.Handlers);

			//Bind
			descriptor.Bind(_trigger, nameof(_trigger.Action3_Object));
			Assert.NotNull(_trigger.Action3_Object);

			descriptor.Handlers.Add(new MyHandlerObject());
			Assert.Equal(1, descriptor.Handlers.Count);
			Assert.Throws<MyHandlerException>(_trigger.OnAction3_Object);

			//Unbind
			descriptor.Unbind(_trigger, nameof(_trigger.Action3_Object));
			Assert.Null(_trigger.Action3_Object);
		}

		[Fact]
		public void TestAction3_Generic()
		{
			var name = nameof(_trigger.Action3_Generic);
			var descriptor = new EventDescriptor<MyRequest>(name);
			Assert.Equal(name, descriptor.Name);
			Assert.Null(descriptor.Title);
			Assert.Null(descriptor.Description);
			Assert.Empty(descriptor.Handlers);

			//Bind
			descriptor.Bind(_trigger, nameof(_trigger.Action3_Generic));
			Assert.NotNull(_trigger.Action3_Generic);

			descriptor.Handlers.Add(new MyHandlerGeneric());
			Assert.Equal(1, descriptor.Handlers.Count);
			Assert.Throws<MyHandlerException>(_trigger.OnAction3_Generic);

			//Unbind
			descriptor.Unbind(_trigger, nameof(_trigger.Action3_Generic));
			Assert.Null(_trigger.Action3_Generic);
		}

		public class MyRequest
		{
			public MyRequest() { }
			public MyRequest(string name, object value)
			{
				this.Name = name;
				this.Value = value;
			}

            public string Name { get; set; }
			public object Value { get; set; }
		}

		public class MyEventArgs : EventArgs
		{
            public MyEventArgs(string name, object value)
            {
                this.Name = name;
				this.Value = value;
            }

            public string Name { get; set; }
			public object Value { get; set; }
		}

		public class MyEventTrigger
		{
			public event EventHandler MyEvent;
			public event EventHandler<MyEventArgs> MyEventWithArgs;

			public bool HasMyEvent => MyEvent != null;
			public bool HasMyEventWithArgs => MyEventWithArgs != null;

			public void OnMyEvent() => this.MyEvent?.Invoke(this, EventArgs.Empty);
			public void OnMyEventWithArgs() => this.MyEventWithArgs?.Invoke(this, new MyEventArgs("MY_NAME", "MY_VALUE"));

			public Action Action0 { get; set; }
			public Action<object> Action1_Object { get; set; }
			public Action<MyRequest> Action1_Generic { get; set; }
			public Action<object, object> Action2_Object { get; set; }
			public Action<object, MyRequest> Action2_Generic { get; set; }
			public Action<object, object, IDictionary<string, object>> Action3_Object { get; set; }
			public Action<object, MyRequest, IDictionary<string, object>> Action3_Generic { get; set; }

			public void OnAction0() => this.Action0?.Invoke();
			public void OnAction1_Object() => this.Action1_Object?.Invoke("request");
			public void OnAction1_Generic() => this.Action1_Generic?.Invoke(new MyRequest());
			public void OnAction2_Object() => this.Action2_Object?.Invoke(this, "request");
			public void OnAction2_Generic() => this.Action2_Generic?.Invoke(this, new MyRequest());
			public void OnAction3_Object() => this.Action3_Object?.Invoke(this, "request", new Dictionary<string, object>());
			public void OnAction3_Generic() => this.Action3_Generic?.Invoke(this, new MyRequest(), new Dictionary<string, object>());
		}

		public class MyEventHandler1 : HandlerBase<EventArgs>
		{
			protected override ValueTask OnHandleAsync(object caller, EventArgs request, IDictionary<string, object> parameters, CancellationToken cancellation)
			{
				throw new MyHandlerException(request, parameters);
			}
		}

		public class MyEventHandler2 : HandlerBase<MyEventArgs>
		{
			protected override ValueTask OnHandleAsync(object caller, MyEventArgs request, IDictionary<string, object> parameters, CancellationToken cancellation)
			{
				throw new MyHandlerException(request, parameters);
			}
		}

		public class MyHandlerObject : HandlerBase<object>
		{
			protected override ValueTask OnHandleAsync(object caller, object request, IDictionary<string, object> parameters, CancellationToken cancellation)
			{
				throw new MyHandlerException(request, parameters);
			}
		}

		public class MyHandlerGeneric : HandlerBase<MyRequest>
		{
			protected override ValueTask OnHandleAsync(object caller, MyRequest request, IDictionary<string, object> parameters, CancellationToken cancellation)
			{
				throw new MyHandlerException(request, parameters);
			}
		}

		public class MyHandlerException : ApplicationException
		{
            public MyHandlerException(object request, IDictionary<string, object> parameters = null) : base($"The handler error.")
            {
				this.Request = request;
				this.Parameters = parameters;
            }

            public object Request { get; }
			public IDictionary<string, object> Parameters { get; }
		}
	}
}

﻿using System;
using System.Reflection;
using System.Timers;
using Autofac;
using NUnit.Framework;
using SharpTestsEx;
using Stardust.Node;
using Stardust.Node.Interfaces;
using Stardust.Node.Timers;
using Stardust.Node.Workers;

namespace NodeTest
{
	[TestFixture]
	class NodeModuleTest
	{
		[SetUp]
		public void SetUp()
		{
			var nodeConfiguration = new NodeConfiguration(new Uri("http://localhost:5000"),
			                                              new Uri("http://localhost:5000"),
			                                              Assembly.Load("NodeTest.JobHandlers"),
			                                              "test",
			                                              1);

			var builder = new ContainerBuilder();
			builder.RegisterModule(new NodeModule(nodeConfiguration));

			_container = builder.Build();
		}

		private IContainer _container;

		[Test]
		public void ShouldResolveObjects()
		{
			using (var scope = _container.BeginLifetimeScope())
			{
				scope.Resolve<NodeController>().Should().Not.Be.Null();
				scope.Resolve<NodeConfiguration>().Should().Not.Be.Null();
				scope.Resolve<TrySendJobDetailToManagerTimer>().Should().Not.Be.Null();
				scope.Resolve<TrySendNodeStartUpNotificationToManagerTimer>().Should().Not.Be.Null();
				scope.Resolve<Timer>().Should().Not.Be.Null();
				scope.Resolve<TrySendJobFaultedToManagerTimer>().Should().Not.Be.Null();
				scope.Resolve<TrySendJobCanceledToManagerTimer>().Should().Not.Be.Null();
			}
		}

		[Test]
		public void ShouldResolveInterfaces()
		{
			using (var scope = _container.BeginLifetimeScope())
			{
				scope.Resolve<IHttpSender>().Should().Not.Be.Null(); 
				scope.Resolve<IInvokeHandler>().Should().Not.Be.Null(); 
				scope.Resolve<IWorkerWrapper>().Should().Not.Be.Null(); 

				_container.IsRegistered<IHttpSender>().Should().Be.True();
				_container.IsRegistered<IInvokeHandler>().Should().Be.True();
				_container.IsRegistered<IWorkerWrapper>().Should().Be.True();
			}
		}
	}
}

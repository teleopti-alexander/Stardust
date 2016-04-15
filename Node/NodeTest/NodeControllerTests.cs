﻿using System;
using System.Configuration;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Web.Http;
using Newtonsoft.Json;
using NodeTest.Fakes;
using NodeTest.Fakes.InvokeHandlers;
using NodeTest.Fakes.Timers;
using NodeTest.JobHandlers;
using NUnit.Framework;
using Stardust.Node.API;
using Stardust.Node.Entities;
using Stardust.Node.Interfaces;
using Stardust.Node.Workers;

namespace NodeTest
{
	[TestFixture]
	public class NodeControllerTests
	{
		[SetUp]
		public void SetUp()
		{
			var parameters = new TestJobParams("hejhopp",
			                                   "i lingonskogen");

			var ser = JsonConvert.SerializeObject(parameters);

			_jobQueueItemEntity = new JobQueueItemEntity
			{
				JobId = Guid.NewGuid(),
				Name = "JobToDo Name",
				Serialized = ser,
				Type = "NodeTest.JobHandlers.TestJobParams"
			};

			_nodeStartupNotification = new NodeStartupNotificationToManagerFake(_nodeConfigurationFake,
			                                                                    new FakeHttpSender());
			_pingToManagerFake = new PingToManagerFake();

			_trySendJobDetailToManagerTimerFake =
				new TrySendJobDetailToManagerTimerFake(_nodeConfigurationFake,
				                                         new FakeHttpSender(),
				                                         1000);

			_sendJobDoneTimer = new SendJobDoneTimerFake(_nodeConfigurationFake,
			                                             _trySendJobDetailToManagerTimerFake,
			                                             new FakeHttpSender());

			_sendJobCanceledTimer = new SendJobCanceledTimerFake(_nodeConfigurationFake,
			                                                     _trySendJobDetailToManagerTimerFake,
			                                                     new FakeHttpSender());

			_sendJobFaultedTimer = new SendJobFaultedTimerFake(_nodeConfigurationFake,
			                                                   _trySendJobDetailToManagerTimerFake,
			                                                   new FakeHttpSender());
		}

		[TestFixtureSetUp]
		public void TestFixtureSetUp()
		{
			var baseAddress = new Uri(ConfigurationManager.AppSettings["BaseAddress"]);

			var managerLocation = new Uri(ConfigurationManager.AppSettings["ManagerLocation"]);

			var handlerAssembly = Assembly.Load(ConfigurationManager.AppSettings["HandlerAssembly"]);

			var nodeName = ConfigurationManager.AppSettings["NodeName"];

			var pingToManagerSeconds =
				Convert.ToDouble(ConfigurationManager.AppSettings["PingToManagerSeconds"]);

			_nodeConfigurationFake = new NodeConfiguration(baseAddress,
			                                                   managerLocation,
			                                                   handlerAssembly,
			                                                   nodeName,
			                                                   pingToManagerSeconds);
		}

		private NodeConfiguration _nodeConfigurationFake;
		private IWorkerWrapper _workerWrapper;
		private NodeController _nodeController;
		private JobQueueItemEntity _jobQueueItemEntity;
		private PingToManagerFake _pingToManagerFake;
		private NodeStartupNotificationToManagerFake _nodeStartupNotification;
		private SendJobDoneTimerFake _sendJobDoneTimer;
		private SendJobCanceledTimerFake _sendJobCanceledTimer;
		private SendJobFaultedTimerFake _sendJobFaultedTimer;
		private TrySendJobDetailToManagerTimerFake _trySendJobDetailToManagerTimerFake;

		[Test]
		public void CancelJobShouldReturnNotFoundWhenCancellingJobWhenIdle()
		{
			_workerWrapper = new WorkerWrapper(new ShortRunningInvokeHandlerFake(),
			                                   _nodeConfigurationFake,
			                                   _nodeStartupNotification,
			                                   _pingToManagerFake,
			                                   _sendJobDoneTimer,
			                                   _sendJobCanceledTimer,
			                                   _sendJobFaultedTimer,
			                                   _trySendJobDetailToManagerTimerFake);

			_nodeController = new NodeController(_workerWrapper, _nodeConfigurationFake)
			{
				Request = new HttpRequestMessage()
			};

			IHttpActionResult actionResultCancel = _nodeController.TryCancelJob(_jobQueueItemEntity.JobId);

			Assert.IsTrue(actionResultCancel.ExecuteAsync(new CancellationToken())
							  .Result.StatusCode ==
						  HttpStatusCode.NotFound);
		}

		[Test]
		public void CancelJobShouldReturnNotFoundWhenCancellingWrongJob()
		{
			_workerWrapper = new WorkerWrapper(new ShortRunningInvokeHandlerFake(),
			                                   _nodeConfigurationFake,
			                                   _nodeStartupNotification,
			                                   _pingToManagerFake,
			                                   _sendJobDoneTimer,
			                                   _sendJobCanceledTimer,
			                                   _sendJobFaultedTimer,
			                                   _trySendJobDetailToManagerTimerFake);

			_nodeController = new NodeController(_workerWrapper, _nodeConfigurationFake)
			{
				Request = new HttpRequestMessage()
			};

			var wrongJobToDo = new JobQueueItemEntity
			{
				JobId = Guid.NewGuid(),
				Name = "Another name",
				Type = "NodeTest.JobHandlers.TestJobParams",
				Serialized = "Serialized data"
			};

			var actionResult = _nodeController.StartJob(_jobQueueItemEntity);

			Assert.IsTrue(actionResult.ExecuteAsync(new CancellationToken())
							  .Result.StatusCode ==
						  HttpStatusCode.OK);

			actionResult = _nodeController.TryCancelJob(wrongJobToDo.JobId);

			Assert.IsTrue(actionResult.ExecuteAsync(new CancellationToken())
							  .Result.StatusCode ==
						  HttpStatusCode.NotFound);
		}

		[Test]
		public void CancelJobShouldReturnOkWhenSuccessful()
		{
			_workerWrapper = new WorkerWrapper(new LongRunningInvokeHandlerFake(),
			                                   _nodeConfigurationFake,
			                                   _nodeStartupNotification,
			                                   _pingToManagerFake,
			                                   _sendJobDoneTimer,
			                                   _sendJobCanceledTimer,
			                                   _sendJobFaultedTimer,
			                                   _trySendJobDetailToManagerTimerFake);

			_nodeController = new NodeController(_workerWrapper, _nodeConfigurationFake)
			{
				Request = new HttpRequestMessage()
			};

			_nodeController.StartJob(_jobQueueItemEntity);

			_trySendJobDetailToManagerTimerFake.WaitHandle.Wait(1500);

			var actionResult = _nodeController.TryCancelJob(_jobQueueItemEntity.JobId);

			Assert.IsTrue(actionResult.ExecuteAsync(new CancellationToken())
							  .Result.StatusCode ==
						  HttpStatusCode.OK);
		}

		[Test]
		public void ShouldReturnBadRequestWhenJobDefinitionIsNullCancelJob()
		{
			_workerWrapper = new WorkerWrapper(new ShortRunningInvokeHandlerFake(),
			                                   _nodeConfigurationFake,
			                                   _nodeStartupNotification,
			                                   _pingToManagerFake,
			                                   _sendJobDoneTimer,
			                                   _sendJobCanceledTimer,
			                                   _sendJobFaultedTimer,
			                                   _trySendJobDetailToManagerTimerFake);

			_nodeController = new NodeController(_workerWrapper, _nodeConfigurationFake)
			{
				Request = new HttpRequestMessage(),
				Configuration = new HttpConfiguration()
			};

			var actionResult = _nodeController.TryCancelJob(Guid.Empty);

			Assert.IsTrue(actionResult.ExecuteAsync(new CancellationToken())
							  .Result.StatusCode ==
						  HttpStatusCode.BadRequest);
		}

		[Test]
		public void ShouldReturnBadRequestWhenJobDefinitionIsNullStartJob()
		{
			_workerWrapper = new WorkerWrapper(new ShortRunningInvokeHandlerFake(),
			                                   _nodeConfigurationFake,
			                                   _nodeStartupNotification,
			                                   _pingToManagerFake,
			                                   _sendJobDoneTimer,
			                                   _sendJobCanceledTimer,
			                                   _sendJobFaultedTimer,
			                                   _trySendJobDetailToManagerTimerFake);

			_nodeController = new NodeController(_workerWrapper, _nodeConfigurationFake)
			{
				Request = new HttpRequestMessage(),
				Configuration = new HttpConfiguration()
			};


			var actionResult = _nodeController.StartJob(null);

			Assert.IsTrue(actionResult.ExecuteAsync(new CancellationToken())
							  .Result.StatusCode ==
						  HttpStatusCode.BadRequest);
		}

		[Test]
		public void StartJobShouldReturnConflictWhenAlreadyProcessingJob()
		{
			_workerWrapper = new WorkerWrapper(new ShortRunningInvokeHandlerFake(),
			                                   _nodeConfigurationFake,
			                                   _nodeStartupNotification,
			                                   _pingToManagerFake,
			                                   _sendJobDoneTimer,
			                                   _sendJobCanceledTimer,
			                                   _sendJobFaultedTimer,
			                                   _trySendJobDetailToManagerTimerFake);

			_nodeController = new NodeController(_workerWrapper, _nodeConfigurationFake)
			{
				Request = new HttpRequestMessage(),
				Configuration = new HttpConfiguration()
			};

			var parameters = new TestJobParams("hejhopp",
			                                   "i lingonskogen");
			var ser = JsonConvert.SerializeObject(parameters);

			var jobToDo2 = new JobQueueItemEntity
			{
				JobId = Guid.NewGuid(),
				Name = "Another name",
				Serialized = ser,
				Type = "NodeTest.JobHandlers.TestJobParams"
			};

			_nodeController.StartJob(_jobQueueItemEntity);

			var actionResult = _nodeController.StartJob(jobToDo2);

			Assert.IsTrue(actionResult.ExecuteAsync(new CancellationToken())
							  .Result.StatusCode ==
						  HttpStatusCode.Conflict);
		}

		[Test]
		public void StartJobShouldReturnOkIfNotRunningJobAlready()
		{
			_workerWrapper = new WorkerWrapper(new ShortRunningInvokeHandlerFake(),
			                                   _nodeConfigurationFake,
			                                   _nodeStartupNotification,
			                                   _pingToManagerFake,
			                                   _sendJobDoneTimer,
			                                   _sendJobCanceledTimer,
			                                   _sendJobFaultedTimer,
			                                   _trySendJobDetailToManagerTimerFake);

			_nodeController = new NodeController(_workerWrapper, _nodeConfigurationFake)
			{
				Request = new HttpRequestMessage()
			};

			var actionResult = _nodeController.StartJob(_jobQueueItemEntity);

			Assert.IsTrue(actionResult.ExecuteAsync(new CancellationToken())
							  .Result.StatusCode ==
						  HttpStatusCode.OK);
		}
	}
}
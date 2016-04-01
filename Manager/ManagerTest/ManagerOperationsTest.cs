﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Results;
using log4net.Config;
using ManagerTest.Database;
using ManagerTest.Fakes;
using NUnit.Framework;
using SharpTestsEx;
using Stardust.Manager;
using Stardust.Manager.ActionResults;
using Stardust.Manager.Interfaces;
using Stardust.Manager.Models;
using Stardust.Manager.Validations;

namespace ManagerTest
{
	[ManagerOperationTests]
	[TestFixture]
	public class ManagerOperationsTest 
	{
		public ManagerController Target;
		public IJobRepository JobRepository;
		public IWorkerNodeRepository NodeRepository;
		public INodeManager NodeManager;
		public IManagerConfiguration ManagerConfiguration;
		public FakeHttpSender HttpSender;
		private DatabaseHelper _databaseHelper;
		private readonly Uri _nodeUri1 = new Uri("http://localhost:9050/");
		private readonly Uri _nodeUri2 = new Uri("http://localhost:9051/");

		private void ThisNodeIsBusy(string url)
		{
			HttpSender.BusyNodesUrl.Add(url);
		}

		[TestFixtureSetUp]
		public void TextFixtureSetUp()
		{
			_databaseHelper = new DatabaseHelper();
			_databaseHelper.Create();
#if DEBUG
			var configurationFile = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;
			XmlConfigurator.ConfigureAndWatch(new FileInfo(configurationFile));
#endif
		
		}

		[SetUp]
		public void Setup()
		{
			_databaseHelper.TryClearDatabase();
		}

		[Test]
		public void ResetJobsOnFalseClaimOnHeartBeatIfItsFree()
		{
			var jobId = Guid.NewGuid();
			var userName = "ManagerTests";
			var job = new JobDefinition
			{
				Id = jobId,
				Name = "job",
				UserName = userName,
				Serialized = "Fake Serialized",
				Type = "Fake Type"
			};

			JobRepository.AddJobDefinition(job);
			NodeRepository.Add(new WorkerNode { Url = _nodeUri1 });

			JobRepository.CheckAndAssignNextJob(HttpSender);

			Target.Heartbeat(_nodeUri1);
			HttpSender.CalledNodes.First()
				.Key.Should()
				.Contain(_nodeUri1.ToString());
		}

		[Test]
		public void ShouldBeAbleToAcknowledgeWhenJobIsReceived()
		{
			var job = new JobRequestModel
			{
				Name = "ShouldBeAbleToAcknowledgeWhenJobIsReceived",
				Serialized = "ngt",
				Type = "bra",
				UserName = "ManagerTests"
			};
			var result = Target.DoThisJob(job);
			result.Should()
				.Not.Be.Null();
		}

		[Test]
		public void ShouldBeAbleToCancelJobOnNode()
		{
			NodeRepository.Add(new WorkerNode { Url = _nodeUri1 });
			NodeRepository.Add(new WorkerNode { Url = _nodeUri2 });
			Target.Heartbeat(_nodeUri1);
			Target.Heartbeat(_nodeUri2);

			var jobId = Guid.NewGuid();
			JobRepository.AddJobDefinition(new JobDefinition {Id = jobId, Serialized = "", Name = "", Type = "", UserName = "ManagerTests"});
			JobRepository.CheckAndAssignNextJob(HttpSender);
			HttpSender.CalledNodes.Clear();
			Target.CancelThisJob(jobId);
			HttpSender.CalledNodes.Count()
				.Should()
				.Be.EqualTo(1);
		}

		[Test]
		public void ShouldBeAbleToPersistManyJobs()
		{
			var jobRequestModels = new List<JobRequestModel>();

			for (var i = 0; i < 50; i++)
			{
				var jobRequestModel = new JobRequestModel
				{
					Name = "Name data " + i,
					Serialized = "Serialized",
					Type = "Type",
					UserName = "User name"
				};

				jobRequestModels.Add(jobRequestModel);
			}

			var tasks = new List<Task>();

			foreach (var jobRequestModel in jobRequestModels)
			{
				var model = jobRequestModel;


				tasks.Add(new Task(() =>
				{
					var response=Target.DoThisJob(model);

				}));
			}

			Parallel.ForEach(tasks,
			                 task => { task.Start(); });

			Task.WaitAll(tasks.ToArray());

			var faultedExists = tasks.Exists(task => task.IsFaulted);

			Assert.IsFalse(faultedExists);
		}


		[Test]
		public void ShouldBeAbleToPersistBadRequestResonsToHistoryDetail()
		{
			var job = new JobRequestModel
			{
				Name = "ShouldBeAbleToPersistNewJob",
				Serialized = "ngtbara",
				Type = "typngtannat",
				UserName = "ManagerTests"
			};

			var response=Target.DoThisJob(job);

			JobRepository.GetAllJobDefinitions()
				.Count.Should()
				.Be.EqualTo(1);
		}

		[Test]
		public void ShouldBeAbleToPersistNewJob()
		{
			var job = new JobRequestModel
			{
				Name = "ShouldBeAbleToPersistNewJob",
				Serialized = "ngtbara",
				Type = "typngtannat",
				UserName = "ManagerTests"
			};

			var response=Target.DoThisJob(job);

			if (response is BadRequestWithReasonPhrase)
			{
				Assert.Fail("Invalid job request model.");
			}
			else
			{
				JobRepository.GetAllJobDefinitions()
					.Count.Should()
					.Be.EqualTo(1);
			}
		}


		[Test]
		public void ShouldGetUniqueJobIdWhilePersistingJob()
		{
			var response=Target.DoThisJob(new JobRequestModel
			{
				Name = "ShouldGetUniqueJobIdWhilePersistingJob",
				Serialized = "ngt",
				Type = "bra",
				UserName = "ManagerTests"
			});

			if (response is BadRequestWithReasonPhrase)
			{
				Assert.Fail("Invalid job request model 1.");	
			}

			response = Target.DoThisJob(new JobRequestModel
			{
				Name = "ShouldGetUniqueJobIdWhilePersistingJob",
				Serialized = "ngt",
				Type = "bra",
				UserName = "ManagerTests"
			});

			if (response is BadRequestWithReasonPhrase)
			{
				Assert.Fail("Invalid job request model 2.");
			}
			else
			{
				JobRepository.GetAllJobDefinitions()
					.Count.Should()
					.Be.EqualTo(2);

			}
		}

		[Test]
		public void ShouldNotRemoveARunningJobFromRepo()
		{
			var jobId = Guid.NewGuid();

			var job = new JobDefinition
			{
				Name = "Name",
				Serialized = "Serialized",
				Type = "Type",
				UserName = "ManagerTests",
				Id = jobId
			};

			var validator = new Validator();

			var response =
				validator.ValidateObject(job, new HttpRequestMessage());

			if (response is BadRequestWithReasonPhrase)
			{
				Assert.Fail("Job defintion object is invalid.");
			}
			else
			{
				JobRepository.AddJobDefinition(job);
				NodeRepository.Add(new WorkerNode
				{
					Url = _nodeUri1
				});
				JobRepository.CheckAndAssignNextJob(HttpSender);
				ThisNodeIsBusy(_nodeUri1.ToString());
				Target.CancelThisJob(jobId);
				JobRepository.GetAllJobDefinitions()
					.Count.Should()
					.Be.EqualTo(1);
			}
		}

		[Test]
		public void ShouldRemoveAQueuedJob()
		{
			var jobId = Guid.NewGuid();

			var job = new JobDefinition
			{
				Name = "Name",
				Serialized = "Serialized",
				Type = "Type",
				UserName = "ManagerTests",
				Id = jobId
			};

			var validator = new Validator();

			var response =
				validator.ValidateObject(job, new HttpRequestMessage());

			if (response is BadRequestWithReasonPhrase)
			{
				Assert.Fail("Job defintion object is invalid.");
			}
			else
			{
				JobRepository.AddJobDefinition(job);
				Target.CancelThisJob(jobId);
				JobRepository.GetAllJobDefinitions()
					.Count.Should()
					.Be.EqualTo(0);
			}
		}

		[Test]
		public void ShouldReturnBadRequestIfJobRequestModelNameIsNull()
		{
			var job = new JobRequestModel
			{
				Name = null,
				Serialized = "Serialized",
				Type = "Type",
				UserName = "UserName"
			};

			IHttpActionResult response=Target.DoThisJob(job);

			Assert.IsInstanceOf(typeof(BadRequestWithReasonPhrase),response);

		}

		[Test]
		public void ShouldReturnBadRequestIfHeartbeatGetsAnInvalidUri()
		{
			var response = Target.Heartbeat(null);

			Assert.IsInstanceOf(typeof(BadRequestWithReasonPhrase), response);
		}

		[Test]
		public void ShouldReturnBadRequestIfJobFailedGetsInvalidJobFailedMode()
		{
			JobFailedModel jobFailedModel=new JobFailedModel();

			var response = Target.JobFailed(jobFailedModel);

			Assert.IsInstanceOf(typeof(BadRequestWithReasonPhrase), response);
		}

		[Test]
		public void ShouldReturnBadRequestIfJobFailedGetsANull()
		{
			var response = Target.JobFailed(null);

			Assert.IsInstanceOf(typeof(BadRequestWithReasonPhrase), response);
		}


		[Test]
		public void ShouldReturnBadRequestIJobDoneGetsAnInvalidUri()
		{
			var response = Target.JobDone(Guid.Empty);

			Assert.IsInstanceOf(typeof(BadRequestWithReasonPhrase), response);
		}

		[Test]
		public void ShouldReturnBadRequestIfJobHistoryDetailsGetsAnInvalidGuid()
		{
			var response = Target.JobHistoryDetails(Guid.Empty);

			Assert.IsInstanceOf(typeof(BadRequestWithReasonPhrase), response);
		}

		[Test]
		public void ShouldReturnBadRequestIfJobHistoryGetsAnInvalidGuid()
		{
			var response = Target.JobHistory(Guid.Empty);

			Assert.IsInstanceOf(typeof(BadRequestWithReasonPhrase), response);
		}

		[Test]
		public void ShouldReturnBadRequestIfCancelThisJobGetsAnInvalidGuid()
		{
			var response=Target.CancelThisJob(Guid.Empty);

			Assert.IsInstanceOf(typeof(BadRequestWithReasonPhrase), response);
		}

		[Test]
		public void ShouldReturnBadRequestIfJobRequestModelNameIsEmptyString()
		{
			var job = new JobRequestModel
			{
				Name = string.Empty,
				Serialized = "Serialized",
				Type = "Type",
				UserName = "UserName"
			};

			IHttpActionResult response = Target.DoThisJob(job);

			Assert.IsInstanceOf(typeof(BadRequestWithReasonPhrase), response);

		}

		[Test]
		public void ShouldReturnBadRequestIfJobRequestModelSerializedIsNull()
		{
			var job = new JobRequestModel
			{
				Name = "Name",
				Serialized = null,
				Type = "Type",
				UserName = "UserName"
			};

			IHttpActionResult response = Target.DoThisJob(job);

			Assert.IsInstanceOf(typeof(BadRequestWithReasonPhrase), response);

		}

		[Test]
		public void ShouldReturnBadRequestIfJobRequestModelSerializedIsEmptyString()
		{
			var job = new JobRequestModel
			{
				Name = "Name",
				Serialized = string.Empty,
				Type = "Type",
				UserName = "UserName"
			};

			IHttpActionResult response = Target.DoThisJob(job);

			Assert.IsInstanceOf(typeof(BadRequestWithReasonPhrase), response);

		}

		[Test]
		public void ShouldReturnBadRequestIfJobRequestModelTypeIsNull()
		{
			var job = new JobRequestModel
			{
				Name = "Name",
				Serialized = "Serialized",
				Type = null,
				UserName = "UserName"
			};

			IHttpActionResult response = Target.DoThisJob(job);

			Assert.IsInstanceOf(typeof(BadRequestWithReasonPhrase), response);

		}

		[Test]
		public void ShouldReturnBadRequestIfJobRequestModelTypeIsEmptyString()
		{
			var job = new JobRequestModel
			{
				Name = "Name",
				Serialized = "Serialized",
				Type = string.Empty,
				UserName = "UserName"
			};

			IHttpActionResult response = Target.DoThisJob(job);

			Assert.IsInstanceOf(typeof(BadRequestWithReasonPhrase), response);

		}

		[Test]
		public void ShouldReturnBadRequestIfJobRequestModelUserNameIsNull()
		{
			var job = new JobRequestModel
			{
				Name = "Name",
				Serialized = "Serialized",
				Type = "Type",
				UserName = null
			};

			IHttpActionResult response = Target.DoThisJob(job);

			Assert.IsInstanceOf(typeof(BadRequestWithReasonPhrase), response);

		}

		[Test]
		public void ShouldReturnBadRequestIfJobRequestModelUserNameIsEmtpyString()
		{
			var job = new JobRequestModel
			{
				Name = "Name",
				Serialized = "Serialized",
				Type = "Type",
				UserName = string.Empty
			};

			IHttpActionResult response = Target.DoThisJob(job);

			Assert.IsInstanceOf(typeof(BadRequestWithReasonPhrase), response);

		}

		[Test]
		public void ShouldReturnConflictIfNodeIsBusy()
		{
			var job = new JobRequestModel
			{
				Name = "ShouldBeAbleToSendNewJobToAvailableNode",
				Serialized = "ngt",
				Type = "bra",
				UserName = "ManagerTests"
			};
			ThisNodeIsBusy(_nodeUri1.ToString());

			Target.Heartbeat(_nodeUri1);

			Target.DoThisJob(job);

			HttpSender.CalledNodes.Count.Should()
				.Be.EqualTo(0);
		}

		[Test]
		public void ShouldReturnIdOfPersistedJob()
		{
			var newJobId =
				((OkNegotiatedContentResult<Guid>)
					Target.DoThisJob(new JobRequestModel
					{
						Name = "ShouldReturnIdOfPersistedJob",
						Serialized = "ngt",
						Type = "bra",
						UserName = "ManagerTests"
					})).Content;
			newJobId.Should()
				.Not.Be.Null();
		}

		[Test]
		public void ShouldReturnJobHistoryFromJobId()
		{
			var job = new JobRequestModel {Name = "Name", Serialized = "Ser", Type = "Type", UserName = "ManagerTests"};

			var doJobResult = Target.DoThisJob(job);

			var okNegotiatedDoJobResult = doJobResult as OkNegotiatedContentResult<Guid>;
			var jobId = okNegotiatedDoJobResult.Content;

			var getResult = Target.JobHistory(jobId);

			var okNegotiatedGetResult = getResult as OkNegotiatedContentResult<JobHistory>;
			var jobHistory = okNegotiatedGetResult.Content;
			Assert.IsNotNull(jobHistory);
		}

		[Test]
		public void ShouldSendOkWhenJobDoneSignalReceived()
		{
			var jobId = Guid.NewGuid();
			var job = new JobDefinition
			{
				Id = jobId,
				AssignedNode = _nodeUri1.ToString(),
				Name = "job",
				Serialized = "",
				Type = "",
				UserName = "ShouldSendOkWhenJobDoneSignalReceived"
			};
			JobRepository.AddJobDefinition(job);
			var result = Target.JobDone(job.Id);
			result.Should()
				.Not.Be.Null();
		}
	}
}
﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using log4net.Config;
using Manager.Integration.Test.Constants;
using Manager.Integration.Test.Helpers;
using Manager.Integration.Test.Notifications;
using Manager.Integration.Test.Tasks;
using Manager.Integration.Test.Timers;
using Manager.Integration.Test.Validators;
using Manager.IntegrationTest.Console.Host.Diagnostics;
using NUnit.Framework;

namespace Manager.Integration.Test
{
	[TestFixture]
	public class OneManagerAndOneNodeTests
	{
		private static readonly ILog Logger =
			LogManager.GetLogger(typeof (OneManagerAndOneNodeTests));

		private bool _clearDatabase = true;
		private string _buildMode = "Debug";

		private string ManagerDbConnectionString { get; set; }
		private Task Task { get; set; }
		private AppDomainTask AppDomainTask { get; set; }
		private CancellationTokenSource CancellationTokenSource { get; set; }

		private void logMessage(string message)
		{
			LogHelper.LogDebugWithLineNumber(message, Logger);
		}

		[TestFixtureSetUp]
		public void TestFixtureSetUp()
		{
			AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

			ManagerDbConnectionString =
				ConfigurationManager.ConnectionStrings["ManagerConnectionString"].ConnectionString;

			var configurationFile = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;
			XmlConfigurator.ConfigureAndWatch(new FileInfo(configurationFile));
			logMessage("Start TestFixtureSetUp");
			
#if (DEBUG)
			// Do nothing.
#else
            _clearDatabase = true;
            _buildMode = "Release";
#endif

			if (_clearDatabase)
			{
				DatabaseHelper.TryClearDatabase(ManagerDbConnectionString);
			}
			CancellationTokenSource = new CancellationTokenSource();

			AppDomainTask = new AppDomainTask(_buildMode);
			Task = AppDomainTask.StartTask(numberOfManagers: 1,
			                               numberOfNodes: 1,
			                               cancellationTokenSource: CancellationTokenSource);

			Thread.Sleep(TimeSpan.FromSeconds(2));
			logMessage("Finished TestFixtureSetUp");
		}
		
		private void CurrentDomain_UnhandledException(object sender,
		                                              UnhandledExceptionEventArgs e)
		{
			var exp = e.ExceptionObject as Exception;
			if (exp != null)
			{
				LogHelper.LogFatalWithLineNumber(exp.Message,
				                                 Logger,
				                                 exp);
			}
		}

		[TestFixtureTearDown]
		public void TestFixtureTearDown()
		{
			logMessage("Start TestFixtureTearDown");
			if (AppDomainTask != null)
				AppDomainTask.Dispose();
			logMessage("Finished TestFixtureTearDown");
		}

		

		[Test]
		public void CancelWrongJobsTest()
		{
			logMessage("Start.");

			var createNewJobRequests = JobHelper.GenerateTestJobParamsRequests(1);
			logMessage("( " + createNewJobRequests.Count + " ) jobs will be created.");

			var timeout =
				JobHelper.GenerateTimeoutTimeInMinutes(createNewJobRequests.Count,
				                                       2);
			//--------------------------------------------
			// Notify when node is up.
			//--------------------------------------------
			logMessage("Waiting for node to start.");

			var sqlNotiferCancellationTokenSource = new CancellationTokenSource();
			var sqlNotifier = new SqlNotifier(ManagerDbConnectionString);

			var task = sqlNotifier.CreateNotifyWhenNodesAreUpTask(1,
																  sqlNotiferCancellationTokenSource,
																  IntegerValidators.Value1IsEqualToValue2Validator);
			task.Start();

			sqlNotifier.NotifyWhenAllNodesAreUp.Wait(timeout);
			sqlNotifier.Dispose();

			logMessage("Node have started.");

			//--------------------------------------------
			// Start actual test.
			//--------------------------------------------
			var jobManagerTaskCreators = new List<JobManagerTaskCreator>();

			var checkJobHistoryStatusTimer = new CheckJobHistoryStatusTimer(createNewJobRequests.Count,
			                                                                StatusConstants.SuccessStatus,
			                                                                StatusConstants.DeletedStatus,
			                                                                StatusConstants.FailedStatus,
			                                                                StatusConstants.CanceledStatus);
			foreach (var jobRequestModel in createNewJobRequests)
			{
				var jobManagerTaskCreator = new JobManagerTaskCreator(checkJobHistoryStatusTimer);
				jobManagerTaskCreator.CreateNewJobToManagerTask(jobRequestModel);
				jobManagerTaskCreators.Add(jobManagerTaskCreator);
			}

			var startJobTaskHelper = new StartJobTaskHelper();
			var taskHlp = startJobTaskHelper.ExecuteCreateNewJobTasks(jobManagerTaskCreators,
			                                                          new CancellationTokenSource(),
			                                                          timeout);

			checkJobHistoryStatusTimer.GuidAddedEventHandler += (sender,
			                                                     args) =>
			{
				//-----------------------------------
				// Wait for job to start.
				//-----------------------------------
				var nodeStartedNotifier =
					new NodeStatusNotifier(ManagerDbConnectionString);
				nodeStartedNotifier.StartJobDefinitionStatusNotifier(args.Guid,
				                                                     "Started",
				                                                     new CancellationTokenSource());
				nodeStartedNotifier.JobDefinitionStatusNotify.Wait(timeout);

				//-----------------------------------
				// Send wrong id to cancel.
				//-----------------------------------
				var newGuid = Guid.NewGuid();

				var jobManagerTaskCreator = new JobManagerTaskCreator(checkJobHistoryStatusTimer);
				jobManagerTaskCreator.CreateDeleteJobToManagerTask(newGuid);
				jobManagerTaskCreator.StartAndWaitDeleteJobToManagerTask(timeout);
				jobManagerTaskCreator.Dispose();

				nodeStartedNotifier.Dispose();
			};

			checkJobHistoryStatusTimer.ManualResetEventSlim.Wait(timeout);

			Assert.IsTrue(checkJobHistoryStatusTimer.Guids.Count == createNewJobRequests.Count);
			Assert.IsTrue(checkJobHistoryStatusTimer.Guids.All(pair => pair.Value == StatusConstants.SuccessStatus));

			taskHlp.Dispose();

			foreach (var jobManagerTaskCreator in jobManagerTaskCreators)
			{
				jobManagerTaskCreator.Dispose();
			}

			logMessage("Finished.");
		}

		[Test]
		public void CreateSeveralRequestShouldReturnBothCancelAndDeleteStatusesTest()
		{
			logMessage("Start.");

			var createNewJobRequests =
				JobHelper.GenerateLongRunningParamsRequests(1);

			var timeout = JobHelper.GenerateTimeoutTimeInMinutes(createNewJobRequests.Count,
			                                                     2);
			//--------------------------------------------
			// Start actual test.
			//--------------------------------------------
			var jobManagerTaskCreators = new List<JobManagerTaskCreator>();

			var checkJobHistoryStatusTimer = new CheckJobHistoryStatusTimer(createNewJobRequests.Count,
			                                                                StatusConstants.SuccessStatus,
			                                                                StatusConstants.DeletedStatus,
			                                                                StatusConstants.FailedStatus,
			                                                                StatusConstants.CanceledStatus);
			foreach (var jobRequestModel in createNewJobRequests)
			{
				var jobManagerTaskCreator = new JobManagerTaskCreator(checkJobHistoryStatusTimer);
				jobManagerTaskCreator.CreateNewJobToManagerTask(jobRequestModel);
				jobManagerTaskCreators.Add(jobManagerTaskCreator);
			}

			var startJobTaskHelper = new StartJobTaskHelper();

			var taskHlp = startJobTaskHelper.ExecuteCreateNewJobTasks(jobManagerTaskCreators,
			                                                          CancellationTokenSource,
			                                                          timeout);
			checkJobHistoryStatusTimer.GuidAddedEventHandler += (sender,
			                                                     args) =>
			{
				Task.Factory.StartNew(() =>
				{
					var nodeStartedNotifier =
						new NodeStatusNotifier(ManagerDbConnectionString);
					nodeStartedNotifier.StartJobDefinitionStatusNotifier(args.Guid,
					                                                     "Started",
					                                                     CancellationTokenSource);
					nodeStartedNotifier.JobDefinitionStatusNotify.Wait(timeout);

					var jobManagerTaskCreator = new JobManagerTaskCreator(checkJobHistoryStatusTimer);
					jobManagerTaskCreator.CreateDeleteJobToManagerTask(args.Guid);
					jobManagerTaskCreator.StartAndWaitDeleteJobToManagerTask(timeout);
					
					nodeStartedNotifier.Dispose();
					jobManagerTaskCreator.Dispose();
				},
				                      CancellationTokenSource.Token);
			};

			checkJobHistoryStatusTimer.ManualResetEventSlim.Wait(timeout);

			var condition =
				checkJobHistoryStatusTimer.Guids.Count == createNewJobRequests.Count;

			Assert.IsTrue(condition,
			              "Must have equal number of rows.");
			Assert.IsFalse(checkJobHistoryStatusTimer.Guids.Any(pair => pair.Value == StatusConstants.SuccessStatus),
			               "Invalid SUCCESS status.");
			Assert.IsFalse(checkJobHistoryStatusTimer.Guids.Any(pair => pair.Value == StatusConstants.NullStatus),
			               "Invalid NULL status.");
			Assert.IsFalse(checkJobHistoryStatusTimer.Guids.Any(pair => pair.Value == StatusConstants.EmptyStatus),
			               "Invalid EMPTY status.");

			var allCancelOrDeleteStatus =
				checkJobHistoryStatusTimer.Guids.All(pair => pair.Value == StatusConstants.CanceledStatus ||
				                                             pair.Value == StatusConstants.DeletedStatus);

			Assert.IsTrue(allCancelOrDeleteStatus,
			              "All rows shall have CANCEL or DELETE status.");

			taskHlp.Dispose();

			foreach (var jobManagerTaskCreator in jobManagerTaskCreators)
			{
				jobManagerTaskCreator.Dispose();
			}

			logMessage("Finished.");
		}


		[Test]
		public void JobShouldHaveStatusFailedIfFailedTest()
		{
			logMessage("Start.");

			var createNewJobRequests = JobHelper.GenerateFailingJobParamsRequests(1);

			var timeout =
				JobHelper.GenerateTimeoutTimeInMinutes(createNewJobRequests.Count,
				                                       2);
			//--------------------------------------------
			// Start actual test.
			//--------------------------------------------
			var jobManagerTaskCreators = new List<JobManagerTaskCreator>();

			var checkJobHistoryStatusTimer = new CheckJobHistoryStatusTimer(createNewJobRequests.Count,
			                                                                StatusConstants.SuccessStatus,
			                                                                StatusConstants.DeletedStatus,
			                                                                StatusConstants.FailedStatus,
			                                                                StatusConstants.CanceledStatus);
			foreach (var jobRequestModel in createNewJobRequests)
			{
				var jobManagerTaskCreator = new JobManagerTaskCreator(checkJobHistoryStatusTimer);
				jobManagerTaskCreator.CreateNewJobToManagerTask(jobRequestModel);
				jobManagerTaskCreators.Add(jobManagerTaskCreator);
			}

			var startJobTaskHelper = new StartJobTaskHelper();
			var taskHlp = startJobTaskHelper.ExecuteCreateNewJobTasks(jobManagerTaskCreators,
			                                                          CancellationTokenSource,
			                                                          timeout);

			checkJobHistoryStatusTimer.ManualResetEventSlim.Wait(timeout);

			Assert.IsTrue(checkJobHistoryStatusTimer.Guids.Count == createNewJobRequests.Count);
			Assert.IsTrue(checkJobHistoryStatusTimer.Guids.All(pair => pair.Value == StatusConstants.FailedStatus));

			taskHlp.Dispose();
			foreach (var jobManagerTaskCreator in jobManagerTaskCreators)
			{
				jobManagerTaskCreator.Dispose();
			}

			logMessage("Finished.");
		}

		/// <summary>
		///     DO NOT FORGET TO RUN COMMAND BELOW AS ADMINISTRATOR.
		///     netsh http add urlacl url=http://+:9050/ user=everyone listen=yes
		/// </summary>
		[Test]
		public void ShouldBeAbleToCreateASuccessJobRequestTest()
		{
			logMessage("Start.");

			var createNewJobRequests = JobHelper.GenerateTestJobParamsRequests(1);

			logMessage("( " + createNewJobRequests.Count + " ) jobs will be created.");

			var timeout =
				JobHelper.GenerateTimeoutTimeInMinutes(createNewJobRequests.Count,
				                                       2);
			//--------------------------------------------
			// Start actual test.
			//--------------------------------------------
			var jobManagerTaskCreators = new List<JobManagerTaskCreator>();
			var checkJobHistoryStatusTimer = new CheckJobHistoryStatusTimer(createNewJobRequests.Count,
			                                                                StatusConstants.SuccessStatus,
			                                                                StatusConstants.DeletedStatus,
			                                                                StatusConstants.FailedStatus,
			                                                                StatusConstants.CanceledStatus);
			foreach (var jobRequestModel in createNewJobRequests)
			{
				var jobManagerTaskCreator = new JobManagerTaskCreator(checkJobHistoryStatusTimer);
				jobManagerTaskCreator.CreateNewJobToManagerTask(jobRequestModel);
				jobManagerTaskCreators.Add(jobManagerTaskCreator);
			}

			var startJobTaskHelper = new StartJobTaskHelper();
			var managerIntegrationStopwatch = new ManagerIntegrationStopwatch();

			var taskHlp = startJobTaskHelper.ExecuteCreateNewJobTasks(jobManagerTaskCreators,
			                                                          CancellationTokenSource,
			                                                          timeout);
			
			checkJobHistoryStatusTimer.ManualResetEventSlim.Wait(timeout);
			var elapsedTime =
				managerIntegrationStopwatch.GetTotalElapsedTimeInSeconds();

			logMessage("Job took : " + elapsedTime + " seconds.");

			Assert.IsTrue(checkJobHistoryStatusTimer.Guids.Count == createNewJobRequests.Count,
			              "Number of requests must be equal.");

			Assert.IsTrue(checkJobHistoryStatusTimer.Guids.All(pair => pair.Value == StatusConstants.SuccessStatus));

			CancellationTokenSource.Cancel();
			taskHlp.Dispose();

			foreach (var jobManagerTaskCreator in jobManagerTaskCreators)
			{
				jobManagerTaskCreator.Dispose();
			}
			logMessage("Finished.");
		}
	}
}
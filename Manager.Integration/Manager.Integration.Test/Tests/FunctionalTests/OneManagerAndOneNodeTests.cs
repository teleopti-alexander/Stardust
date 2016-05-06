﻿using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Manager.Integration.Test.Database;
using Manager.Integration.Test.Helpers;
using Manager.Integration.Test.Initializers;
using Manager.Integration.Test.Models;
using Manager.Integration.Test.Timers;
using Manager.IntegrationTest.Console.Host.Helpers;
using Manager.IntegrationTest.Console.Host.Log4Net;
using NUnit.Framework;

namespace Manager.Integration.Test.Tests.FunctionalTests
{
	[TestFixture]
	public class OneManagerAndOneNodeTests : InitialzeAndFinalizeOneManagerAndOneNodeWait
	{
		public override void SetUp()
		{
			DatabaseHelper.TruncateJobQueueTable(ManagerDbConnectionString);
			DatabaseHelper.TruncateJobTable(ManagerDbConnectionString);
			DatabaseHelper.TruncateJobDetailTable(ManagerDbConnectionString);
		}

		public ManualResetEventSlim ManualResetEventSlim { get; set; }

		private void LogMessage(string message)
		{
			this.Log().DebugWithLineNumber(message);
		}

		public ManagerUriBuilder MangerUriBuilder { get; set; }

		public HttpSender HttpSender { get; set; }

		private readonly object _lockSendHttpCancelJobEventHandler = new object();

		private void SendHttpCancelJobEventHandler(object sender, ObservableCollection<Job> jobs)
		{
			if (!jobs.Any())
			{
				return;
			}

			try
			{
				Monitor.Enter(_lockSendHttpCancelJobEventHandler);

				foreach (var job in 
					jobs.Where(job => job.Started != null && job.Ended == null))
				{
					var cancelJobUri =
						MangerUriBuilder.GetCancelJobUri(job.JobId);

					var response =
						HttpSender.DeleteAsync(cancelJobUri).Result;

					while (response.StatusCode != HttpStatusCode.OK)
					{
						response = HttpSender.DeleteAsync(cancelJobUri).Result;
					}
				}
			}

			finally
			{
				Monitor.Exit(_lockSendHttpCancelJobEventHandler);
			}
		}

		private void SetManualResetEventSlimWhenAllJobsEndedEventHandler(object sender, ObservableCollection<Job> jobs)
		{
			if (!jobs.Any())
			{
				return;
			}

			if (jobs.All(job => job.Started != null && job.Ended != null))
			{
				if (!ManualResetEventSlim.IsSet)
				{
					ManualResetEventSlim.Set();
				}
			}
		}

		[Test]
		public void CancelWrongJobsTest()
		{
			LogMessage("Start test.");

			var startedTest = DateTime.UtcNow;

			var createNewJobRequests =
				JobHelper.GenerateTestJobTimerRequests(1, TimeSpan.FromSeconds(30));

			LogMessage("( " + createNewJobRequests.Count + " ) jobs will be created.");

			var timeout =
				JobHelper.GenerateTimeoutTimeInMinutes(createNewJobRequests.Count,
				                                       2);

			HttpSender = new HttpSender();
			MangerUriBuilder = new ManagerUriBuilder();
			ManualResetEventSlim = new ManualResetEventSlim();

			var addToJobQueueUri = MangerUriBuilder.GetAddToJobQueueUri();

			//---------------------------------------------------------
			// Database validator.
			//---------------------------------------------------------
			var checkTablesInManagerDbTimer =
				new CheckTablesInManagerDbTimer(ManagerDbConnectionString, 2000);

			Task<HttpResponseMessage> taskCancelJob = null;


			checkTablesInManagerDbTimer.ReceivedJobItem += (sender, items) =>
			{
				if (items.Any() &&
				    items.All(job => job.Started != null && job.Ended == null))
				{
					if (taskCancelJob == null)
					{
						taskCancelJob = new Task<HttpResponseMessage>(() =>
						{
							var cancelJobUri =
								MangerUriBuilder.GetCancelJobUri(Guid.NewGuid());

							var response =
								HttpSender.DeleteAsync(cancelJobUri).Result;

							Assert.AreEqual(response.StatusCode, HttpStatusCode.OK);
							
							return response;
						});

						taskCancelJob.Start();
					}
				}
			};

			checkTablesInManagerDbTimer.ReceivedJobItem += (sender, items) =>
			{
				if (items.Any() &&
				    items.All(job => job.Started != null && job.Ended != null))
				{
					if (!ManualResetEventSlim.IsSet)
					{
						ManualResetEventSlim.Set();
					}
				}
			};

			var jobQueueItem = createNewJobRequests.First();

			CancellationTokenSource = new CancellationTokenSource();

			var addToJobQueueTask = new Task(() =>
			{
				var numberOfTries = 0;

				while (true)
				{
					numberOfTries++;

					try
					{
						var response =
							HttpSender.PostAsync(addToJobQueueUri, jobQueueItem).Result;

						if (response.IsSuccessStatusCode || numberOfTries == 10)
						{
							return;
						}
					}

					catch (AggregateException aggregateException)
					{
						if (aggregateException.InnerException is HttpRequestException)
						{
							// try again.
						}
					}

					Thread.Sleep(TimeSpan.FromSeconds(10));
				}
			}, CancellationTokenSource.Token);

			checkTablesInManagerDbTimer.JobTimer.Start();

			addToJobQueueTask.Start();

			ManualResetEventSlim.Wait(timeout);

			Assert.IsTrue(checkTablesInManagerDbTimer.ManagerDbRepository.Jobs.All(job => job.Result.StartsWith("success", StringComparison.InvariantCultureIgnoreCase)));

			checkTablesInManagerDbTimer.Dispose();

			var endedTest = DateTime.UtcNow;

			var description =
				string.Format("Creates {0} CANCEL WRONG jobs with {1} manager and {2} nodes.",
				              createNewJobRequests.Count,
				              NumberOfManagers,
				              NumberOfNodes);

			DatabaseHelper.AddPerformanceData(ManagerDbConnectionString,
			                                  description,
			                                  startedTest,
			                                  endedTest);

			LogMessage("Finished test.");
		}

		[Test]
		public void CreateRequestShouldReturnCancelStatusWhenJobHasStartedAndBeenCanceled()
		{
			LogMessage("Start test");

			var startedTest = DateTime.UtcNow;

			HttpSender = new HttpSender();
			MangerUriBuilder = new ManagerUriBuilder();
			ManualResetEventSlim = new ManualResetEventSlim();

			var timeout = TimeSpan.FromMinutes(5);

			//---------------------------------------------------------
			// Database validator.
			//---------------------------------------------------------
			var checkTablesInManagerDbTimer =
				new CheckTablesInManagerDbTimer(ManagerDbConnectionString, 2000);

			checkTablesInManagerDbTimer.ReceivedJobItem +=
				SetManualResetEventSlimWhenAllJobsEndedEventHandler;

			checkTablesInManagerDbTimer.ReceivedJobItem +=
				SendHttpCancelJobEventHandler;

			checkTablesInManagerDbTimer.JobTimer.Start();

			//---------------------------------------------------------
			// HTTP Request.
			//---------------------------------------------------------
			var addToJobQueueUri = MangerUriBuilder.GetAddToJobQueueUri();

			var jobQueueItem =
				JobHelper.GenerateTestJobTimerRequests(1, TimeSpan.FromMinutes(5)).First();

			CancellationTokenSource = new CancellationTokenSource();

			var addToJobQueueTask = new Task(() =>
			{
				var numberOfTries = 0;

				while (true)
				{
					numberOfTries++;

					try
					{
						var response =
							HttpSender.PostAsync(addToJobQueueUri, jobQueueItem).Result;

						if (response.IsSuccessStatusCode || numberOfTries == 10)
						{
							return;
						}
					}

					catch (AggregateException aggregateException)
					{
						if (aggregateException.InnerException is HttpRequestException)
						{
							// try again.
						}
					}

					Thread.Sleep(TimeSpan.FromSeconds(10));
				}
			}, CancellationTokenSource.Token);

			addToJobQueueTask.Start();
			addToJobQueueTask.Wait(timeout);

			ManualResetEventSlim.Wait(timeout);

			checkTablesInManagerDbTimer.Dispose();

			var endedTest = DateTime.UtcNow;

			var description =
				string.Format("Creates {0} Test Timer jobs with {1} manager and {2} nodes.",
				              1,
				              NumberOfManagers,
				              NumberOfNodes);

			DatabaseHelper.AddPerformanceData(ManagerDbConnectionString,
			                                  description,
			                                  startedTest,
			                                  endedTest);

			LogMessage("Finished test.");
		}

		[Test]
		public void JobShouldHaveStatusFailedIfFailedTest()
		{
			LogMessage("Start test.");

			var startedTest = DateTime.UtcNow;

			var httpSender = new HttpSender();
			var mangerUriBuilder = new ManagerUriBuilder();
			var uri = mangerUriBuilder.GetAddToJobQueueUri();

			var createFailingJobs =
				JobHelper.GenerateFailingJobParamsRequests(1);

			var timeout =
				JobHelper.GenerateTimeoutTimeInMinutes(createFailingJobs.Count,
				                                       2);

			ManualResetEventSlim = new ManualResetEventSlim();

			//---------------------------------------------------------
			// Database validator.
			//---------------------------------------------------------
			var checkTablesInManagerDbTimer =
				new CheckTablesInManagerDbTimer(ManagerDbConnectionString, 2000);

			checkTablesInManagerDbTimer.ReceivedJobItem += (sender, items) =>
			{
				if (items.Any() &&
				    items.All(job => job.Started != null && job.Ended != null))
				{
					if (!ManualResetEventSlim.IsSet)
					{
						ManualResetEventSlim.Set();
					}
				}
			};

			checkTablesInManagerDbTimer.JobTimer.Start();

			var task1 = new Task(() =>
			{
				foreach (var jobQueueItem in createFailingJobs)
				{
					var numberOfTries = 0;

					while (true)
					{
						numberOfTries++;

						try
						{
							var response = httpSender.PostAsync(uri,
							                                    jobQueueItem).Result;

							if (response.IsSuccessStatusCode || numberOfTries == 10)
							{
								break;
							}
						}
						catch (AggregateException aggregateException)
						{
							if (aggregateException.InnerException is HttpRequestException)
							{
								// try again.
							}
						}

						Thread.Sleep(TimeSpan.FromSeconds(10));
					}
				}
			});

			task1.Start();
			task1.Wait();

			ManualResetEventSlim.Wait(timeout);

			Assert.IsTrue(!checkTablesInManagerDbTimer.ManagerDbRepository.JobQueueItems.Any(), "Job queue must be empty.");
			Assert.IsTrue(checkTablesInManagerDbTimer.ManagerDbRepository.Jobs.Any(), "Jobs must have been added.");
			Assert.IsTrue(checkTablesInManagerDbTimer.ManagerDbRepository.
				              Jobs.All(job => job.Result.StartsWith("fail", StringComparison.InvariantCultureIgnoreCase)));

			var endedTest = DateTime.UtcNow;

			var description =
				string.Format("Creates {0} FAILED jobs with {1} manager and {2} nodes.",
				              createFailingJobs.Count,
				              NumberOfManagers,
				              NumberOfNodes);

			DatabaseHelper.AddPerformanceData(ManagerDbConnectionString,
			                                  description,
			                                  startedTest,
			                                  endedTest);


			checkTablesInManagerDbTimer.Dispose();

			LogMessage("Finished test.");
		}

		/// <summary>
		///     DO NOT FORGET TO RUN COMMAND BELOW AS ADMINISTRATOR.
		///     netsh http add urlacl url=http://+:9050/ user=everyone listen=yes
		/// </summary>
		[Test]
		public void ShouldBeAbleToCreateASuccessJobRequestTest()
		{
			this.Log().DebugWithLineNumber("Start test.");

			var startedTest = DateTime.UtcNow;

			var timeout = TimeSpan.FromMinutes(5);
			ManualResetEventSlim = new ManualResetEventSlim();

			//---------------------------------------------------------
			// Database validator.
			//---------------------------------------------------------
			var checkTablesInManagerDbTimer =
				new CheckTablesInManagerDbTimer(ManagerDbConnectionString, 2000);

			checkTablesInManagerDbTimer.ReceivedJobItem += (sender, items) =>
			{
				if (items.Any() &&
				    items.All(job => job.Started != null && job.Ended != null))
				{
					if (!ManualResetEventSlim.IsSet)
					{
						ManualResetEventSlim.Set();
					}
				}
			};

			checkTablesInManagerDbTimer.JobTimer.Start();

			//---------------------------------------------------------
			// HTTP Request.
			//---------------------------------------------------------
			HttpSender = new HttpSender();
			MangerUriBuilder = new ManagerUriBuilder();

			var addToJobQueueUri = MangerUriBuilder.GetAddToJobQueueUri();

			var jobQueueItem =
				JobHelper.GenerateTestJobTimerRequests(1, TimeSpan.FromSeconds(5)).First();

			CancellationTokenSource = new CancellationTokenSource();

			var addToJobQueueTask = new Task(() =>
			{
				var numberOfTries = 0;

				while (true)
				{
					numberOfTries++;

					try
					{
						var response =
							HttpSender.PostAsync(addToJobQueueUri, jobQueueItem).Result;

						if (response.IsSuccessStatusCode || numberOfTries == 10)
						{
							return;
						}
					}

					catch (AggregateException aggregateException)
					{
						if (aggregateException.InnerException is HttpRequestException)
						{
							// try again.
						}
					}

					Thread.Sleep(TimeSpan.FromSeconds(10));
				}
			}, CancellationTokenSource.Token);

			addToJobQueueTask.Start();
			addToJobQueueTask.Wait(timeout);

			ManualResetEventSlim.Wait(timeout);

			Assert.IsTrue(!checkTablesInManagerDbTimer.ManagerDbRepository.JobQueueItems.Any(), "Should not be any jobs left in queue.");
			Assert.IsTrue(checkTablesInManagerDbTimer.ManagerDbRepository.Jobs.Any(), "There should be jobs in jobs table.");
			Assert.IsTrue(checkTablesInManagerDbTimer.ManagerDbRepository.
				              Jobs.All(job => job.Result.StartsWith("success", StringComparison.InvariantCultureIgnoreCase)));

			checkTablesInManagerDbTimer.Dispose();

			var endedTest = DateTime.UtcNow;

			var description =
				string.Format("Creates {0} Test Timer jobs with {1} manager and {2} nodes.",
				              1,
				              NumberOfManagers,
				              NumberOfNodes);

			DatabaseHelper.AddPerformanceData(ManagerDbConnectionString,
			                                  description,
			                                  startedTest,
			                                  endedTest);

			this.Log().DebugWithLineNumber("Finished test.");
		}
	}
}
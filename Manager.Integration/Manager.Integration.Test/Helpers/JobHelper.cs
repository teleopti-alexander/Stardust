﻿using System;
using System.Collections.Generic;
using System.Threading;
using Manager.Integration.Test.Data;
using Manager.Integration.Test.Models;
using Newtonsoft.Json;

namespace Manager.Integration.Test.Helpers
{
	public static class JobHelper
	{
		public static void GiveNodesTimeToInitialize(int numberOfSeconds = 10)
		{
			var time = TimeSpan.FromSeconds(numberOfSeconds);

			Thread.Sleep(time);
		}

		public static TimeSpan GenerateTimeoutTimeInMinutes(int numberOfRequest,
		                                                    int latencyPerRequestInMinutes = 1)
		{
			if (numberOfRequest <= 0)
			{
				throw new ArgumentException("numberOfRequest");
			}

			return new TimeSpan(0, numberOfRequest*latencyPerRequestInMinutes, 0);
		}

		public static TimeSpan GenerateTimeoutTimeInSeconds(int numberOfRequest,
		                                                    int latencyPerRequestInSeconds = 1)
		{
			if (numberOfRequest <= 0)
			{
				throw new ArgumentException("numberOfRequest");
			}

			return new TimeSpan(0,
			                    0,
			                    numberOfRequest*latencyPerRequestInSeconds);
		}

		public static List<JobQueueItem> GenerateLongRunningParamsRequests(int numberOfJobRequests)
		{
			List<JobQueueItem> requestModels = null;

			if (numberOfJobRequests > 0)
			{
				requestModels = new List<JobQueueItem>();

				var loggedInUser = SecurityHelper.GetLoggedInUser();

				for (var i = 1; i <= numberOfJobRequests; i++)
				{
					var longRunningJobParams = new LongRunningJobParams("Job name " + i);

					var longRunningJobParamsJson = JsonConvert.SerializeObject(longRunningJobParams);

					var job = new JobQueueItem
					{
						Name = "Job Name " + i,
						Serialized = longRunningJobParamsJson,
						Type = "NodeTest.JobHandlers.LongRunningJobParams",
						CreatedBy = loggedInUser
					};

					requestModels.Add(job);
				}
			}

			return requestModels;
		}

		public static List<JobQueueItem> GenerateFailingJobParamsRequests(int numberOfJobRequests)
		{
			List<JobQueueItem> requestModels = null;

			if (numberOfJobRequests > 0)
			{
				requestModels = new List<JobQueueItem>();

				var loggedInUser = SecurityHelper.GetLoggedInUser();

				for (var i = 1; i <= numberOfJobRequests; i++)
				{
					var failingJobParams = new FailingJobParams("Error message " + i);

					var failingJobParamsJson = JsonConvert.SerializeObject(failingJobParams);

					var job = new JobQueueItem
					{
						Name = "Job Name " + i,
						Serialized = failingJobParamsJson,
						Type = "NodeTest.JobHandlers.FailingJobParams",
						CreatedBy = loggedInUser
					};

					requestModels.Add(job);
				}
			}

			return requestModels;
		}

		public static List<JobQueueItem> GenerateFastJobParamsRequests(int numberOfJobRequests)
		{
			List<JobQueueItem> requestModels = null;

			if (numberOfJobRequests > 0)
			{
				requestModels = new List<JobQueueItem>();

				var loggedInUser = SecurityHelper.GetLoggedInUser();

				for (var i = 1; i <= numberOfJobRequests; i++)
				{
					var fastJobParams = new FastJobParams("Name " + i);

					var fastJobParamsJson = JsonConvert.SerializeObject(fastJobParams);

					var job = new JobQueueItem
					{
						Name = "Job Name " + i,
						Serialized = fastJobParamsJson,
						Type = "NodeTest.JobHandlers.FastJobParams",
						CreatedBy = loggedInUser
					};

					requestModels.Add(job);
				}
			}

			return requestModels;
		}

		public static List<JobQueueItem> GenerateInvalidTestJobParamsRequests(int numberOfJobRequests)
		{
			List<JobQueueItem> requestModels = null;

			if (numberOfJobRequests > 0)
			{
				requestModels = new List<JobQueueItem>();

				var loggedInUser = SecurityHelper.GetLoggedInUser();

				for (var i = 1; i <= numberOfJobRequests; i++)
				{
					var testJobParams = new TestJobParams("Dummy data " + i,
					                                      "Name data " + i);

					var testJobParamsJson = JsonConvert.SerializeObject(testJobParams);

					var job = new JobQueueItem
					{
						Name = "Job Name " + i,
						Serialized = testJobParamsJson,
						Type = null,
						CreatedBy = loggedInUser
					};

					requestModels.Add(job);
				}
			}

			return requestModels;
		}

		public static List<JobQueueItem> GenerateTestReportProgressJobParamsRequests(int numberOfJobRequests)
		{
			List<JobQueueItem> requestModels = null;

			if (numberOfJobRequests > 0)
			{
				requestModels = new List<JobQueueItem>();

				var loggedInUser = SecurityHelper.GetLoggedInUser();

				for (var i = 1; i <= numberOfJobRequests; i++)
				{
					var testJobParams = new TestJobParams("Dummy data " + i,
					                                      "Name data " + i);

					var testJobParamsJson = JsonConvert.SerializeObject(testJobParams);

					var job = new JobQueueItem
					{
						Name = "Job Name " + i,
						Serialized = testJobParamsJson,
						Type = "NodeTest.JobHandlers.TestReportProgressJobParams",
						CreatedBy = loggedInUser
					};

					requestModels.Add(job);
				}
			}

			return requestModels;
		}

		public static List<JobQueueItem> GenerateTestJobTimerRequests(int numberOfJobRequests,
		                                                              TimeSpan duration)
		{
			List<JobQueueItem> requestModels = null;

			if (numberOfJobRequests > 0)
			{
				requestModels = new List<JobQueueItem>();

				var loggedInUser = SecurityHelper.GetLoggedInUser();

				for (var i = 1; i <= numberOfJobRequests; i++)
				{
					var testJobTimerParams = new TestJobTimerParams("Name " + i,
					                                                duration);

					var testJobTimerParamsToJson = JsonConvert.SerializeObject(testJobTimerParams);

					var job = new JobQueueItem
					{
						Name = "Job Name " + i,
						Serialized = testJobTimerParamsToJson,
						Type = "NodeTest.JobHandlers.TestJobTimerParams",
						CreatedBy = loggedInUser
					};

					requestModels.Add(job);
				}
			}

			return requestModels;
		}

		public static List<JobQueueItem> GenerateTestJobParamsRequests(int numberOfJobRequests)
		{
			List<JobQueueItem> requestModels = null;

			if (numberOfJobRequests > 0)
			{
				requestModels = new List<JobQueueItem>();

				var loggedInUser = SecurityHelper.GetLoggedInUser();

				for (var i = 1; i <= numberOfJobRequests; i++)
				{
					var testJobParams = new TestJobParams("Dummy data " + i,
					                                      "Name data " + i);

					var testJobParamsJson = JsonConvert.SerializeObject(testJobParams);

					var job = new JobQueueItem
					{
						Name = "Job Name " + i,
						Serialized = testJobParamsJson,
						Type = "NodeTest.JobHandlers.TestJobParams",
						CreatedBy = loggedInUser
					};

					requestModels.Add(job);
				}
			}

			return requestModels;
		}
	}
}
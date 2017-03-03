﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;
using Stardust.Manager.Extensions;
using Stardust.Manager.Helpers;
using Stardust.Manager.Interfaces;
using Stardust.Manager.Models;

namespace Stardust.Manager
{
	public class JobRepository : IJobRepository
	{
		private readonly string _connectionString;
		private readonly RetryPolicy _retryPolicy;
		private readonly IHttpSender _httpSender;
		private readonly JobRepositoryCommandExecuter _jobRepositoryCommandExecuter;
		private readonly object _requeueJobLock = new object();

		public JobRepository(ManagerConfiguration managerConfiguration,
		                     RetryPolicyProvider retryPolicyProvider,
		                     IHttpSender httpSender, 
							 JobRepositoryCommandExecuter jobRepositoryCommandExecuter)
		{
			if (retryPolicyProvider == null)
			{
				throw new ArgumentNullException("retryPolicyProvider");
			}

			if (retryPolicyProvider.GetPolicy() == null)
			{
				throw new ArgumentNullException("retryPolicyProvider.GetPolicy");
			}

			_connectionString = managerConfiguration.ConnectionString;
			_httpSender = httpSender;
			_jobRepositoryCommandExecuter = jobRepositoryCommandExecuter;

			_retryPolicy = retryPolicyProvider.GetPolicy();
		}

		public void AddItemToJobQueue(JobQueueItem jobQueueItem)
		{
			try
			{
				using (var sqlConnection = new SqlConnection(_connectionString))
				{
					sqlConnection.OpenWithRetry(_retryPolicy);
					_jobRepositoryCommandExecuter.InsertIntoJobQueue(jobQueueItem, sqlConnection);
				}
			}
			catch (Exception exp)
			{
				this.Log().ErrorWithLineNumber(exp.Message, exp);
				throw;
			}
		}

		public List<JobQueueItem> GetAllItemsInJobQueue()
		{
			try
			{
				List<JobQueueItem> listToReturn;
				using (var sqlConnection = new SqlConnection(_connectionString))
				{
					sqlConnection.OpenWithRetry(_retryPolicy);
					listToReturn = _jobRepositoryCommandExecuter.SelectAllItemsInJobQueue(sqlConnection);
				}
				return listToReturn;
			}
			catch (Exception exp)
			{
				this.Log().ErrorWithLineNumber(exp.Message, exp);
				throw;
			}
		}


		private void DeleteJobQueueItemByJobId(Guid jobId)
		{
			try
			{
				using (var sqlConnection = new SqlConnection(_connectionString))
				{
					sqlConnection.OpenWithRetry(_retryPolicy);
					_jobRepositoryCommandExecuter.DeleteJobFromJobQueue(jobId, sqlConnection);
				}
			}
			catch (Exception exp)
			{
				this.Log().ErrorWithLineNumber(exp.Message, exp);
				throw;
			}
		}

		
		public void AssignJobToWorkerNode()
		{
			try
			{
				List<Uri> allAliveWorkerNodesUri;
				using (var sqlConnection = new SqlConnection(_connectionString))
				{
					sqlConnection.OpenWithRetry(_retryPolicy);
					allAliveWorkerNodesUri = _jobRepositoryCommandExecuter.SelectAllAliveWorkerNodes(sqlConnection);
				}

				if (!allAliveWorkerNodesUri.Any()) return;

				foreach (var uri in allAliveWorkerNodesUri)
				{
					AssignJobToWorkerNodeWorker(uri);
				}
			}
			catch (Exception exp)
			{
				this.Log().ErrorWithLineNumber(exp.Message, exp);
			}
		}

		public void CancelJobByJobId(Guid jobId)
		{
			try
			{
				if (DoesJobQueueItemExists(jobId))
				{
					DeleteJobQueueItemByJobId(jobId);
				}
				else
				{
					CancelJobByJobIdWorker(jobId);
				}
			}
			catch (Exception exp)
			{
				this.Log().ErrorWithLineNumber(exp.Message, exp);
				throw;
			}
		}

		public void UpdateResultForJob(Guid jobId, string result, DateTime ended)
		{
		
			using (var sqlConnection = new SqlConnection(_connectionString))
			{
				sqlConnection.OpenWithRetry(_retryPolicy);
				_jobRepositoryCommandExecuter.UpdateResult(jobId, result, ended, sqlConnection);

				var finishDetail = "Job finished";
				if (result == "Canceled")
				{
					finishDetail = "Job was canceled";
				}
				else if (result == "Fatal Node Failure" || result == "Failed")
				{
					finishDetail = "Job Failed";
				}

			 _jobRepositoryCommandExecuter.InsertJobDetail(jobId, finishDetail, sqlConnection);
			}
		}

		public void CreateJobDetailByJobId(Guid jobId, string detail, DateTime created)
		{
			using (var sqlConnection = new SqlConnection(_connectionString))
			{
				sqlConnection.OpenWithRetry(_retryPolicy);
				_jobRepositoryCommandExecuter.InsertJobDetail(jobId, detail, sqlConnection);
			}
		}



		public JobQueueItem GetJobQueueItemByJobId(Guid jobId)
		{
			try
			{
				JobQueueItem jobQueueItem;
				using (var sqlConnection = new SqlConnection(_connectionString))
				{
					sqlConnection.OpenWithRetry(_retryPolicy);
					jobQueueItem = _jobRepositoryCommandExecuter.SelectJobQueueItem(jobId, sqlConnection);
				}
				return jobQueueItem;
			}
			catch (Exception exp)
			{
				this.Log().ErrorWithLineNumber(exp.Message, exp);
				throw;
			}
		}

		public Job GetJobByJobId(Guid jobId)
		{
			try
			{
				Job job;
				using (var sqlConnection = new SqlConnection(_connectionString))
				{
					sqlConnection.OpenWithRetry(_retryPolicy);
					job = _jobRepositoryCommandExecuter.SelectJob(jobId, sqlConnection);
				}
				return job;
			}
			catch (Exception exp)
			{
				this.Log().ErrorWithLineNumber(exp.Message, exp);
				throw;
			}
		}


		public IList<Job> GetAllJobs()
		{
			try
			{
				
				var jobs = new List<Job>();
				using (var sqlConnection = new SqlConnection(_connectionString))
				{
					sqlConnection.OpenWithRetry(_retryPolicy);
					jobs = _jobRepositoryCommandExecuter.SelectAllJobs(sqlConnection);
				}
				return jobs;
			}
			catch (Exception exp)
			{
				this.Log().ErrorWithLineNumber(exp.Message, exp);
				throw;
			}
		}


		public IList<Job> GetAllExecutingJobs()
		{
			try
			{
				List<Job> jobs;
				using (var sqlConnection = new SqlConnection(_connectionString))
				{
					sqlConnection.OpenWithRetry(_retryPolicy);
					jobs = _jobRepositoryCommandExecuter.SelectAllExecutingJobs(sqlConnection);
				}
				return jobs;
			}
			catch (Exception exp)
			{
				this.Log().ErrorWithLineNumber(exp.Message, exp);
				throw;
			}
		}


		public IList<JobDetail> GetJobDetailsByJobId(Guid jobId)
		{
			try
			{
				List<JobDetail> jobDetails;
				using (var sqlConnection = new SqlConnection(_connectionString))
				{
					sqlConnection.OpenWithRetry(_retryPolicy);
					jobDetails = _jobRepositoryCommandExecuter.SelectJobDetails(jobId, sqlConnection);
				}
				return jobDetails;
			}
			catch (Exception exp)
			{
				this.Log().ErrorWithLineNumber(exp.Message, exp);
				throw;
			}
		}



		public void RequeueJobThatDidNotEndByWorkerNodeUri(string workerNodeUri)
		{
			try
			{
				lock (_requeueJobLock)
				{
					using (var sqlConnection = new SqlConnection(_connectionString))
					{
						sqlConnection.OpenWithRetry(_retryPolicy);
						using (var sqlTransaction = sqlConnection.BeginTransaction())
						{
							var job = _jobRepositoryCommandExecuter.SelectExecutingJob(workerNodeUri, sqlConnection, sqlTransaction);

							if (job == null) return;
							var jobQueueItem = new JobQueueItem
							{
								Created = job.Created,
								CreatedBy = job.CreatedBy,
								JobId = job.JobId,
								Serialized = job.Serialized,
								Name = job.Name,
								Type = job.Type
							};

							_jobRepositoryCommandExecuter.InsertIntoJobQueue(jobQueueItem, sqlConnection, sqlTransaction);
							_jobRepositoryCommandExecuter.DeleteJob(jobQueueItem.JobId, sqlConnection, sqlTransaction);
							sqlTransaction.Commit();
						}
					}
				}
			}
			catch (Exception exp)
			{
				this.Log().ErrorWithLineNumber(exp.Message, exp);
				throw;
			}
		}


		private void CancelJobByJobIdWorker(Guid jobId)
		{
			try
			{
				using (var sqlConnection = new SqlConnection(_connectionString))
				{
					sqlConnection.OpenWithRetry(_retryPolicy);
					using (var sqlTransaction = sqlConnection.BeginTransaction())
					{
						var sentToWorkerNodeUri = _jobRepositoryCommandExecuter.SelectWorkerNode(jobId, sqlConnection, sqlTransaction);
						if (sentToWorkerNodeUri == null) return;

						var builderHelper = new NodeUriBuilderHelper(sentToWorkerNodeUri);
						var uriCancel = builderHelper.GetCancelJobUri(jobId);
						var response = _httpSender.DeleteAsync(uriCancel).Result;

						if (response != null && response.IsSuccessStatusCode)
						{
							_jobRepositoryCommandExecuter.UpdateResult(jobId,"Canceled", DateTime.UtcNow, sqlConnection, sqlTransaction);
							_jobRepositoryCommandExecuter.DeleteJobFromJobQueue(jobId, sqlConnection, sqlTransaction);

							sqlTransaction.Commit();
						}
						else
						{
							this.Log().ErrorWithLineNumber("Could not send cancel to node. JobId: " + jobId);
						}
					}
				}
			}
			catch (Exception exp)
			{
				this.Log().ErrorWithLineNumber(exp.Message, exp);
				throw;
			}
		}



		private void AssignJobToWorkerNodeWorker(Uri availableNode)
		{
			try
			{
				using (var sqlConnection = new SqlConnection(_connectionString))
				{
					sqlConnection.OpenWithRetry(_retryPolicy);
					var jobQueueItem = _jobRepositoryCommandExecuter.AcquireJobQueueItem(sqlConnection);

					if (jobQueueItem == null)
					{
						sqlConnection.Close();
						return;
					}

					var builderHelper = new NodeUriBuilderHelper(availableNode);
					var urijob = builderHelper.GetJobTemplateUri();
					var response = _httpSender.PostAsync(urijob, jobQueueItem).Result;

					if (response != null && (response.IsSuccessStatusCode || response.StatusCode.Equals(HttpStatusCode.BadRequest)))
					{
						var sentToWorkerNodeUri = availableNode.ToString();
						using (var sqlTransaction = sqlConnection.BeginTransaction())
						{
							_jobRepositoryCommandExecuter.InsertIntoJob(jobQueueItem, sentToWorkerNodeUri, sqlConnection, sqlTransaction);
							_jobRepositoryCommandExecuter.DeleteJobFromJobQueue(jobQueueItem.JobId, sqlConnection, sqlTransaction);
							_jobRepositoryCommandExecuter.InsertJobDetail(jobQueueItem.JobId, "Job Started", sqlConnection, sqlTransaction);
							sqlTransaction.Commit();
						}

						if (!response.IsSuccessStatusCode) return;

						urijob = builderHelper.GetUpdateJobUri(jobQueueItem.JobId);
						//what should happen if this response is not 200? 
						_httpSender.PutAsync(urijob, null);
					}
					else
					{
						using (var sqlTransaction = sqlConnection.BeginTransaction())
						{
							if (response == null)
							{
								_jobRepositoryCommandExecuter.UpdateWorkerNode(false, availableNode.ToString(), sqlConnection, sqlTransaction);
							}
							_jobRepositoryCommandExecuter.TagQueueItem(jobQueueItem.JobId, sqlConnection, sqlTransaction);
							sqlTransaction.Commit();
						}
					}
				}
			}
			catch (Exception exp)
			{
				this.Log().ErrorWithLineNumber(exp.Message, exp);
				throw;
			}
		}
		



		public bool DoesJobQueueItemExists(Guid jobId)
		{
			int count;
			using (var sqlConnection = new SqlConnection(_connectionString))
			{
				sqlConnection.OpenWithRetry(_retryPolicy);
				count =_jobRepositoryCommandExecuter.SelectCountJobQueueItem(jobId, sqlConnection);
			}
			return count == 1;
		}
	}
}
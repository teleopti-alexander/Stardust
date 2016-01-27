﻿using System;
using System.Collections.Generic;
using Stardust.Manager.Constants;
using Stardust.Manager.Interfaces;
using Stardust.Manager.Models;

namespace Stardust.Manager
{
	public class JobManager
	{
		private readonly IJobRepository _jobRepository;
		private readonly IWorkerNodeRepository _nodeRepository;
		private readonly IHttpSender _httpSender;

		public JobManager(IJobRepository jobRepository, IWorkerNodeRepository nodeRepository, IHttpSender httpSender)
		{
			_jobRepository = jobRepository;
			_nodeRepository = nodeRepository;
			_httpSender = httpSender;
		}

		public async void CheckAndAssignNextJob()
		{
			var availableNodes = _nodeRepository.LoadAllFreeNodes();
			var upNodes = new List<WorkerNode>();
			foreach (var availableNode in availableNodes)
			{
				var response = await _httpSender.PostAsync(availableNode.Url + NodeRouteConstants.IsAlive, "");
				if (response != null && response.IsSuccessStatusCode)
					upNodes.Add(availableNode);
			}
			_jobRepository.CheckAndAssignNextJob(upNodes, _httpSender);
		}

		public void Add(JobDefinition job)
		{
			_jobRepository.Add(job);
			CheckAndAssignNextJob();
		}

		public void CancelThisJob(Guid id)
		{
			_jobRepository.CancelThisJob(id, _httpSender);
		}

		public void SetEndResultOnJobAndRemoveIt(Guid jobId, string result)
		{
			_jobRepository.SetEndResultOnJob(jobId, result);
			_jobRepository.DeleteJob(jobId);
		}

		public void ReportProgress(JobProgressModel model)
		{
			_jobRepository.ReportProgress(model.JobId, model.ProgressDetail);
		}

	    public JobHistory GetJobHistory(Guid jobId)
	    {
	        return _jobRepository.History(jobId);
	    }
	}
}
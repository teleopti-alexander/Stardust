using System;
using System.Collections.Generic;
using System.Linq;
using Stardust.Manager.Interfaces;
using Stardust.Manager.Models;

namespace ManagerTest.Fakes
{
	public class FakeJobRepository : IJobRepository
	{
		private List<JobDefinition> _jobs = new List<JobDefinition>(); 
		public void Add(JobDefinition job)
		{
			_jobs.Add(job);
		}

		public List<JobDefinition> LoadAll()
		{
			return _jobs;
		}

		public void AssignDesignatedNode(Guid id, string url)
		{
			var job = _jobs.FirstOrDefault(x => x.Id == id);
			job.AssignedNode = url;
		}
		public void DeleteJob(Guid jobId)
		{
			var j = _jobs.FirstOrDefault(x => x.Id.Equals(jobId));
			_jobs.Remove(j);
		}

		public void FreeJobIfNodeIsAssigned(string url)
		{
			var jobs = _jobs.FirstOrDefault(x => x.AssignedNode == url);
			if (jobs != null)
			{
				jobs.AssignedNode = "";
			}
		}

		public void CheckAndAssignNextJob(List<WorkerNode> availableNodes, IHttpSender httpSender)
		{
			throw new NotImplementedException();
		}

		public void CancelThisJob(Guid jobId, IHttpSender httpSender)
		{
			throw new NotImplementedException();
		}

		public void SetEndResultOnJob(Guid jobId, string result)
		{
			throw new NotImplementedException();
		}

		public void ReportProgress(Guid jobId, string detail)
		{
			throw new NotImplementedException();
		}

		public JobHistory History(Guid jobId)
		{
			throw new NotImplementedException();
		}
	}
}
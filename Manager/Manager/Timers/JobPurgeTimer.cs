﻿using System.Data.SqlClient;
using System.Timers;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;

namespace Stardust.Manager.Timers
{
	public class JobPurgeTimer : Timer
	{
		private readonly ManagerConfiguration _managerConfiguration;
		private readonly RetryPolicy _retryPolicy;
		private readonly string _connectionString;


		public JobPurgeTimer(RetryPolicyProvider retryPolicyProvider, ManagerConfiguration managerConfiguration) : base(managerConfiguration.PurgeJobsIntervalHours*60*60*1000)
		{
			_managerConfiguration = managerConfiguration;
			_retryPolicy = retryPolicyProvider.GetPolicy();
			_connectionString = managerConfiguration.ConnectionString;
			Elapsed += PurgeTimer_elapsed;
		}

		public virtual void PurgeTimer_elapsed(object sender, ElapsedEventArgs e)
		{
			Purge();
		}

		private void Purge()
		{
			using (var connection = new SqlConnection(_connectionString))
			{
				string deleteCommandText = "DELETE TOP(@batchsize) FROM [Stardust].[Job] WHERE Created < DATEADD(HOUR, -@hours, GETDATE())";
				connection.OpenWithRetry(_retryPolicy);
				using (var deleteCommand = new SqlCommand(deleteCommandText, connection))
				{
					deleteCommand.Parameters.AddWithValue("@hours", _managerConfiguration.PurgeJobsOlderThanHours);
					deleteCommand.Parameters.AddWithValue("@batchsize", _managerConfiguration.PurgeJobsBatchSize);

					deleteCommand.ExecuteNonQueryWithRetry(_retryPolicy);
				}
			}
		}
	}
}

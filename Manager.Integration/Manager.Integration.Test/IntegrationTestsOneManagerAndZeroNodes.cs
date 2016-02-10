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
using Manager.Integration.Test.Properties;
using Manager.Integration.Test.Scripts;
using Manager.Integration.Test.Timers;
using NUnit.Framework;

namespace Manager.Integration.Test
{
    [TestFixture]
    public class IntegrationTestsOneManagerAndZeroNodes
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof (IntegrationTestsOneManagerAndZeroNodes));

        private bool _clearDatabase = true;
        private bool _startUpManagerAndNodeManually = false;
        private string _buildMode = "Debug";


        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            var configurationFile = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;
            XmlConfigurator.ConfigureAndWatch(new FileInfo(configurationFile));

            TryCreateSqlLoggingTable();


#if (DEBUG)
            // Do nothing.
#else
            _clearDatabase = true;
            _startUpManagerAndNodeManually = false;
            _buildMode = "Release";
#endif

            if (_clearDatabase)
            {
                DatabaseHelper.TryClearDatabase();
            }


            if (!_startUpManagerAndNodeManually)
            {
                var task = AppDomainHelper.CreateAppDomainForManagerIntegrationConsoleHost(_buildMode,
                                                                                           NumberOfNodesToStart);

                task.Start();

                JobHelper.GiveNodesTimeToInitialize();
            }
        }

        private static void TryCreateSqlLoggingTable()
        {
            LogHelper.LogInfoWithLineNumber("Run sql script to create logging file started.",
                                            Logger);

            FileInfo scriptFile =
                new FileInfo(Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase,
                                          Settings.Default.CreateLoggingTableSqlScriptLocationAndFileName));

            ScriptExecuteHelper.ExecuteScriptFile(scriptFile,
                                                  ConfigurationManager.ConnectionStrings["ManagerConnectionString"].ConnectionString);

            LogHelper.LogInfoWithLineNumber("Run sql script to create logging file finished.",
                                            Logger);
        }

        [TearDown]
        public void TearDown()
        {
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            if (AppDomainHelper.AppDomains != null &&
                AppDomainHelper.AppDomains.Any())
            {
                foreach (var appDomain in AppDomainHelper.AppDomains.Values)
                {
                    try
                    {
                        AppDomain.Unload(appDomain);
                    }

                    catch (AppDomainUnloadedException)
                    {
                    }

                    catch (Exception exp)
                    {
                        LogHelper.LogErrorWithLineNumber(exp.Message,
                                                         Logger,
                                                         exp);
                    }
                }
            }
        }

        private const int NumberOfNodesToStart = 0;

        [Test]
        public void JobShouldJustBeQueuedIfNoNodes()
        {
            LogHelper.LogInfoWithLineNumber("Start test.",
                                            Logger);

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

            List<JobRequestModel> requests = JobHelper.GenerateTestJobParamsRequests(1);

            var timeout = JobHelper.GenerateTimeoutTimeInSeconds(requests.Count,
                                                                 30);

            var managerApiHelper = new ManagerApiHelper(new CheckJobHistoryStatusTimer(requests.Count,
                                                                                       5000,
                                                                                       cancellationTokenSource,
                                                                                       StatusConstants.SuccessStatus,
                                                                                       StatusConstants.DeletedStatus,
                                                                                       StatusConstants.FailedStatus,
                                                                                       StatusConstants.CanceledStatus));

            List<Task> tasks = new List<Task>();

            foreach (var jobRequestModel in requests)
            {
                tasks.Add(managerApiHelper.CreateManagerDoThisTask(jobRequestModel));
            }

            Parallel.ForEach(tasks,
                             task => { task.Start(); });

            managerApiHelper.CheckJobHistoryStatusTimer.Start();

            managerApiHelper.CheckJobHistoryStatusTimer.ManualResetEventSlim.Wait(timeout);

            Assert.IsTrue(managerApiHelper.CheckJobHistoryStatusTimer.Guids.Count > 0);

            managerApiHelper.Dispose();
            
        }
    }
}
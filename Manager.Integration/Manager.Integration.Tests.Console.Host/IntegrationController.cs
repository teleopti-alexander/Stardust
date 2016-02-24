﻿using System;
using System.Web.Http;
using log4net;
using Manager.IntegrationTest.Console.Host.Helpers;

namespace Manager.IntegrationTest.Console.Host
{
	public class IntegrationController : ApiController
	{
		private static readonly ILog Logger =
			LogManager.GetLogger(typeof (IntegrationController));

		public IntegrationController()
		{
			WhoAmI = "[INTEGRATION CONTROLLER, " + Environment.MachineName.ToUpper() + "]";

			LogHelper.LogInfoWithLineNumber(Logger,
			                                WhoAmI);
		}

		public string WhoAmI { get; set; }

		[HttpPost, Route("appdomain/nodes")]
		public IHttpActionResult StartNewNode()
		{
			LogHelper.LogDebugWithLineNumber(Logger,
			                                 "Called API controller.");

			string friendlyname;

			Program.StartNewNode(out friendlyname);

			return Ok(friendlyname);
		}

		[HttpDelete, Route("appdomain/nodes/{id}")]
		public IHttpActionResult DeleteNode(string id)
		{
			LogHelper.LogDebugWithLineNumber(Logger,
			                                 "Called API controller.");

			if (string.IsNullOrEmpty(id))
			{
				LogHelper.LogWarningWithLineNumber(Logger,
				                                   "Bad request, id : " + id);

				return BadRequest(id);
			}

			LogHelper.LogInfoWithLineNumber(Logger,
			                                "Try shut down Node with id : " + id);

			var success = Program.ShutDownNodeAppDomain(id);

			if (success)
			{
				LogHelper.LogInfoWithLineNumber(Logger,
				                                "Node has been shut down, with id : " + id);

				return Ok(id);
			}

			LogHelper.LogWarningWithLineNumber(Logger,
			                                   "Id not found, id : " + id);

			return NotFound();
		}

		[HttpGet, Route("appdomain/nodes")]
		public IHttpActionResult GetAllNodes()
		{
			LogHelper.LogDebugWithLineNumber(Logger,
			                                 "Called API controller.");

			var appDomainsList = Program.GetAllNodes();

			return Ok(appDomainsList);
		}
	}
}
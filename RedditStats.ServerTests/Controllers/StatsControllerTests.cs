using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RedditStats.Server.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedditStats.Server.Controllers.Tests
{
	[TestClass()]
	public class StatsControllerTests
	{
		[TestMethod()]
		public async Task GetTest()
		{
			var mockLoggerFactory = new LoggerFactory();
			var mockLogger = new Logger<StatsController>(mockLoggerFactory);
			var mockController = new StatsController(mockLogger);
			var startTime = DateTimeOffset.Now.ToUnixTimeSeconds() - 6000;

			var result = await mockController.Get(startTime);

			Assert.IsNotNull(result);
			Assert.IsNotNull(result.Posts);
			Assert.IsNotNull(result.Users);
			Assert.IsTrue(result.Posts.Any());
			Assert.IsTrue(result.Users.Any());
		}
	}
}
using API.Controllers;
using API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;

namespace API.Tests
{
    [TestClass]
    public class ApiUT
    {
        private string apiWebsiteURL = null;
        private string mongoURL = null;
        private MongoEntityRepository entityRepo;

        public ApiUT()
        {
            var config = new ConfigurationBuilder()
              .AddJsonFile("appsettings.json", true, true)
              .Build();
            apiWebsiteURL = config["ApiWebsiteURL"];
            mongoURL = config["MongoURL"];
            entityRepo = new MongoEntityRepository(mongoURL);
        }


        [TestMethod]
        public void ApiTest()
        {
            var controller = new ApiController(entityRepo);
            var result = controller.Get(EntityEnum.profile).Result as OkObjectResult;
            Assert.AreEqual(result.StatusCode, 200);
        }
    }
}

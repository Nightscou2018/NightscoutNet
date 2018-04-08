using API.Controllers;
using API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace API.Tests
{
    [TestClass]
    public class ApiUT
    {
        private const string JSON_CONTENT_TYPE = "application/json";
        JsonWriterSettings jsonWriterSettings = new JsonWriterSettings { OutputMode = JsonOutputMode.Strict };

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
        [TestCategory("Api")]
        public void ApiTest()
        {
            string entry = GetEntry();

            PutEntry(entry);

            ListProfiles();
        }

        private void ListProfiles()
        {
            var controller = new ApiController(entityRepo);
            var result = controller.Get(EntityEnum.profile, count: 10).Result as ContentResult;
            Assert.IsNotNull(result);
            Assert.AreEqual(result.ContentType, JSON_CONTENT_TYPE);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.Content));
        }

        private string GetEntry()
        {
            var client = new RestClient();
            client.BaseUrl = new Uri(apiWebsiteURL);

            var request = new RestRequest("api/entries/58e9e37b6096d830c4588ab2", Method.GET);
            var response = client.Execute(request);

            Assert.IsTrue(response.IsSuccessful);
            Assert.IsFalse(string.IsNullOrWhiteSpace(response.Content));

            return response.Content;
        }

        private void PutEntry(string originalEntry)
        {
            var bson = BsonDocument.Parse(originalEntry);

            long ticks = DateTime.Now.Ticks;
            bson.SetElement(new BsonElement("test", ticks));

            var client = new RestClient();
            client.BaseUrl = new Uri(apiWebsiteURL);

            var request = new RestRequest("api/entries/58e9e37b6096d830c4588ab2", Method.PUT);
            request.AddJsonBody(bson.ToJson(jsonWriterSettings));
            var response = client.Execute(request);

            Assert.IsTrue(response.IsSuccessful);
        }
    }
}

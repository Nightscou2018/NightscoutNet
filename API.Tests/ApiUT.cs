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
            var entries = ListEntries();

            var entry = entries[0];
            var entryGot = GetEntry(entry.GetElement("_id").Value.AsObjectId);

            PutEntry(entryGot);

            var idDoc = PostEntry(entry);

            DeleteEntry(idDoc, force: false);

            DeleteEntry(idDoc, force: true);

            ListProfiles();
        }

        private List<BsonDocument> ListEntries()
        {
            var client = new RestClient();
            client.BaseUrl = new Uri(apiWebsiteURL);

            var request = new RestRequest("api/entries?count=10", Method.GET);
            var response = client.Execute(request);

            Assert.IsTrue(response.IsSuccessful);
            Assert.IsFalse(string.IsNullOrWhiteSpace(response.Content));

            var bson = BsonDocument.Parse(response.Content);
            var countEl = bson.GetElement("count");
            Assert.AreEqual(countEl.Value, 10);

            var itemsEl = bson.GetElement("items");

            List<BsonDocument> list = new List<BsonDocument>();
            foreach (var item in itemsEl.Value.AsBsonArray)
            {
                list.Add(item.AsBsonDocument);
            }
            Assert.AreEqual(list.Count, 10);
            return list;
        }

        private void ListProfiles()
        {
            var controller = new ApiController(entityRepo);
            var result = controller.Get(
                EntityEnum.profile, count: 10, includeDeleted: false, fromModified: null)
                .Result as ContentResult;
            Assert.IsNotNull(result);
            Assert.AreEqual(result.ContentType, JSON_CONTENT_TYPE);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.Content));
        }

        private BsonDocument GetEntry(ObjectId objectId)
        {
            var client = new RestClient();
            client.BaseUrl = new Uri(apiWebsiteURL);

            var request = new RestRequest($"api/entries/{objectId.ToString()}", Method.GET);
            var response = client.Execute(request);

            Assert.IsTrue(response.IsSuccessful);
            Assert.IsFalse(string.IsNullOrWhiteSpace(response.Content));

            return BsonDocument.Parse(response.Content);
        }

        private void PutEntry(BsonDocument bson)
        {
            long ticks = DateTime.Now.Ticks;
            bson.SetElement(new BsonElement("test", ticks));

            var client = new RestClient();
            client.BaseUrl = new Uri(apiWebsiteURL);

            var request = new RestRequest($"api/entries", Method.PUT);
            request.AddJsonBody(bson.ToJson(jsonWriterSettings));
            var response = client.Execute(request);

            Assert.IsTrue(response.IsSuccessful);
        }

        private BsonDocument PostEntry(BsonDocument source)
        {
            var clone = source.DeepClone().AsBsonDocument;

            if (clone.Contains("_id"))
            {
                clone.Remove("_id");
            }

            var client = new RestClient();
            client.BaseUrl = new Uri(apiWebsiteURL);

            var request = new RestRequest($"api/entries", Method.POST);
            request.AddJsonBody(clone.ToJson(jsonWriterSettings));
            var response = client.Execute(request);

            Assert.IsTrue(response.IsSuccessful);
            Assert.IsFalse(string.IsNullOrWhiteSpace(response.Content));

            var bson = BsonDocument.Parse(response.Content);

            return bson;
        }

        private void DeleteEntry(BsonDocument idDoc, bool force)
        {
            var id = idDoc.GetValue("_id").AsObjectId;

            var client = new RestClient();
            client.BaseUrl = new Uri(apiWebsiteURL);

            var request = new RestRequest($"api/entries/{id.ToString()}?force={force}", Method.DELETE);
            var response = client.Execute(request);

            Assert.IsTrue(response.IsSuccessful);
        }
    }
}

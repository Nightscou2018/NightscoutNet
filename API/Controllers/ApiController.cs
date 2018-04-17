using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using API.Services;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Bson.IO;

namespace API.Controllers
{
    public class ApiController : Controller
    {
        private const string JSON_CONTENT_TYPE = "application/json";
        private ICollectionRepository collectionRepo;
        JsonWriterSettings jsonWriterSettings = new JsonWriterSettings { OutputMode = JsonOutputMode.Strict };

        public ApiController(ICollectionRepository collectionRepo)
        {
            this.collectionRepo = collectionRepo;
        }

        [HttpGet("/api/{collection}")]
        public async Task<IActionResult> Get(CollectionEnum collection, int? count, bool? includeDeleted, DateTime? fromModified)
        {
            if (collection == CollectionEnum.undefined)
                return NotFound();

            var list = await collectionRepo.List(collection, count, includeDeleted ?? false, fromModified);
            var bson = new BsonDocument();

            bson.Add("count", list.Count);

            var array = new BsonArray(list);
            bson.Add("items", array);

            return Content(bson.ToJson(jsonWriterSettings), JSON_CONTENT_TYPE);
        }

        [HttpGet("/api/list")]
        public async Task<IActionResult> List(string collections, int? count, bool? includeDeleted, DateTime? fromModified)
        {
            if (string.IsNullOrWhiteSpace(collections))
                return NotFound();

            var bson = new BsonDocument();

            foreach (var colString in collections.Split(Const.PARAM_SEPARATORS))
            {
                object colObject;
                if (!Enum.TryParse(typeof(CollectionEnum), colString, out colObject))
                {
                    return NotFound();
                }
                var collection = (CollectionEnum)colObject;

                var list = await collectionRepo.List(collection, count, includeDeleted ?? false, fromModified);

                var array = new BsonArray(list);
                bson.Add(collection.ToString().ToLower(), array);
            }

            return Content(bson.ToJson(jsonWriterSettings), JSON_CONTENT_TYPE);
        }

        [HttpGet("/api/{collection}/{id}")]
        public async Task<IActionResult> Get(CollectionEnum collection, string id)
        {
            try
            {
                if (collection == CollectionEnum.undefined)
                    return NotFound();

                var bson = await collectionRepo.Get(collection, id);

                if (bson == null)
                {
                    return NotFound();
                }
                else
                {
                    return Content(bson.ToJson(jsonWriterSettings), JSON_CONTENT_TYPE);
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }

        [HttpPost("/api/{collection}")]
        public async Task<IActionResult> Post(CollectionEnum collection)
        {
            try
            {
                if (collection == CollectionEnum.undefined)
                    return NotFound();

                // TODO authorization

                string json = null;
                using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
                {
                    json = await reader.ReadToEndAsync();
                }

                var bson = BsonDocument.Parse(json);

                SetStatus(bson, StatusEnum.New);

                var objectId = await collectionRepo.Create(collection, bson);

                var idDoc = new BsonDocument();
                idDoc.Add("_id", objectId);

                return Content(idDoc.ToJson(jsonWriterSettings), JSON_CONTENT_TYPE);
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }

        [HttpPut("/api/{collection}")]
        public async Task<IActionResult> Put(CollectionEnum collection, [FromBody]string value)
        {
            try
            {
                if (collection == CollectionEnum.undefined)
                    return NotFound();

                // TODO authorization

                var bson = BsonDocument.Parse(value);

                SetStatus(bson, StatusEnum.Modified);

                if (!await collectionRepo.Update(collection, bson))
                {
                    return NotFound();
                }

                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }


        [HttpDelete("/api/{collection}/{id}")]
        public async Task<IActionResult> Delete(CollectionEnum collection, string id, bool force = false)
        {
            try
            {
                if (collection == CollectionEnum.undefined)
                    return NotFound();

                // TODO authorization

                if (!await collectionRepo.Delete(collection, id))
                {
                    return NotFound();
                }

                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }


        private void SetStatus(BsonDocument bson, StatusEnum status)
        {
            BsonElement elStatus = new BsonElement(Const.STATUS_ELEMENT, status.ToString().ToLower());
            bson.SetElement(elStatus);

            BsonElement elModified = new BsonElement(Const.MODIFIED_ELEMENT, DateTime.Now);
            bson.SetElement(elModified);
        }
    }
}

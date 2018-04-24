using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using API.Helpers;
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
        public async Task<IActionResult> Get(CollectionEnum collection, int? count, DateTime? fromDate, long? fromMs)
        {
            // TODO authorization

            if (collection == CollectionEnum.undefined)
                return NotFound();

            fromDate = fromDate ?? DateTimeHelper.FromEpoch(fromMs) ?? Const.MIN_DATE;
            count = count ?? Const.DEFAULT_COUNT;

            var list = await collectionRepo.List(collection, fromDate, count, includeDeleted: false);
            var bson = new BsonDocument();

            bson.Add("count", list.Count);

            var array = new BsonArray(list);
            bson.Add("items", array);

            return Content(bson.ToJson(jsonWriterSettings), JSON_CONTENT_TYPE);
        }

        [HttpPost("/api/delta")]
        public async Task<IActionResult> Delta(int? count)
        {
            try
            {
                // TODO authorization

                var outBson = new BsonDocument();
                count = count ?? Const.DEFAULT_COUNT;

                BsonDocument inBson;
                if (!BsonDocument.TryParse(await ReadBody(), out inBson))
                {
                    return BadRequest();
                }

                foreach (var inElement in inBson.Elements)
                {
                    CollectionEnum collection;
                    long fromMs;
                    int? specificCount = null;
                    try
                    {
                        collection = (CollectionEnum)Enum.Parse(typeof(CollectionEnum), inElement.Name);
                        var colParams = inElement.Value.AsBsonDocument;
                        fromMs = colParams.GetValue(Const.FROM).AsInt64;
                        if (colParams.Contains(Const.COUNT))
                        {
                            specificCount = colParams.GetValue(Const.COUNT).AsInt32;
                        }
                    }
                    catch
                    {
                        return BadRequest();
                    }

                    var list = await collectionRepo.List(collection, DateTimeHelper.FromEpoch(fromMs), 
                        specificCount ?? count, includeDeleted: true);

                    if (list.Count > 0)
                    {
                        var outElement = new BsonDocument { { collection.ToString(), new BsonArray(list) } };
                        outBson.AddRange(outElement);
                    }
                }

                return Content(outBson.ToJson(jsonWriterSettings), JSON_CONTENT_TYPE);
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }

        [HttpGet("/api/{collection}/{id}")]
        public async Task<IActionResult> Get(CollectionEnum collection, string id)
        {
            try
            {
                if (collection == CollectionEnum.undefined)
                    return NotFound();

                // TODO authorization

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

                var bson = BsonDocument.Parse(await ReadBody());

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
        public async Task<IActionResult> Put(CollectionEnum collection)
        {
            try
            {
                if (collection == CollectionEnum.undefined)
                    return NotFound();

                // TODO authorization

                var bson = BsonDocument.Parse(await ReadBody());

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


        private async Task<string> ReadBody()
        {
            using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                return await reader.ReadToEndAsync();
            }
        }
    }
}

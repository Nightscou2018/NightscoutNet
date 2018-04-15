using System;
using System.Collections.Generic;
using System.Linq;
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
        private IEntityRepository entityRepo;
        JsonWriterSettings jsonWriterSettings = new JsonWriterSettings { OutputMode = JsonOutputMode.Strict };

        public ApiController(IEntityRepository entityRepo)
        {
            this.entityRepo = entityRepo;
        }

        [HttpGet("/api/{entity}")]
        public async Task<IActionResult> Get(EntityEnum entity, int? count, bool? includeDeleted, DateTime? fromModified)
        {
            if (entity == EntityEnum.undefined)
                return NotFound();

            var list = await entityRepo.List(entity, count, includeDeleted ?? false, fromModified);
            var bson = new BsonDocument();

            bson.Add("count", list.Count);

            var array = new BsonArray(list);
            bson.Add("items", array);

            return Content(bson.ToJson(jsonWriterSettings), JSON_CONTENT_TYPE);
        }

        [HttpGet("/api/list")]
        public async Task<IActionResult> List(string entities, int? count, bool? includeDeleted, DateTime? fromModified)
        {
            if (string.IsNullOrWhiteSpace(entities))
                return NotFound();

            var bson = new BsonDocument();

            foreach (var entityString in entities.Split(Constants.PARAM_SEPARATORS))
            {
                object entityObject;
                if (!Enum.TryParse(typeof(EntityEnum), entityString, out entityObject))
                {
                    return NotFound();
                }
                var entity = (EntityEnum)entityObject;

                var list = await entityRepo.List(entity, count, includeDeleted ?? false, fromModified);

                var array = new BsonArray(list);
                bson.Add(entity.ToString().ToLower(), array);
            }           

            return Content(bson.ToJson(jsonWriterSettings), JSON_CONTENT_TYPE);
        }

        [HttpGet("/api/{entity}/{id}")]
        public async Task<IActionResult> Get(EntityEnum entity, string id)
        {
            try
            {
                if (entity == EntityEnum.undefined)
                    return NotFound();

                var bson = await entityRepo.Get(entity, id);

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

        [HttpPost("/api/{entity}")]
        public async Task<IActionResult> Post(EntityEnum entity, [FromBody]string value)
        {
            try
            {
                if (entity == EntityEnum.undefined)
                    return NotFound();

                var bson = BsonDocument.Parse(value);

                SetStatus(bson, StatusEnum.New);

                var objectId = await entityRepo.Create(entity, bson);

                var idDoc = new BsonDocument();
                idDoc.Add("_id", objectId);

                return Content(idDoc.ToJson(jsonWriterSettings), JSON_CONTENT_TYPE);
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }

        [HttpPut("/api/{entity}")]
        public async Task<IActionResult> Put(EntityEnum entity, [FromBody]string value)
        {
            try
            {
                if (entity == EntityEnum.undefined)
                    return NotFound();

                var bson = BsonDocument.Parse(value);

                SetStatus(bson, StatusEnum.Modified);

                if (!await entityRepo.Update(entity, bson))
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


        [HttpDelete("/api/{entity}/{id}")]
        public async Task<IActionResult> Delete(EntityEnum entity, string id, bool force = false)
        {
            try
            {
                if (entity == EntityEnum.undefined)
                    return NotFound();

                if (force)
                {
                    if (!await entityRepo.Delete(entity, id))
                    {
                        return NotFound();
                    }
                }
                else
                {
                    var bson = await entityRepo.Get(entity, id);

                    SetStatus(bson, StatusEnum.Deleted);

                    if (!await entityRepo.Update(entity, bson))
                    {
                        return NotFound();
                    }
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
            BsonElement elStatus = new BsonElement(Constants.STATUS_ELEMENT, status.ToString().ToLower());
            bson.SetElement(elStatus);

            BsonElement elModified = new BsonElement(Constants.MODIFIED_ELEMENT, DateTime.Now);
            bson.SetElement(elModified);
        }
    }
}

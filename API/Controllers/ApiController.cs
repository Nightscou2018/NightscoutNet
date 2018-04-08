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
        public async Task<IActionResult> Get(EntityEnum entity, int? count)
        {
            if (entity == EntityEnum.undefined)
                return NotFound();

            var list = await entityRepo.List(entity, count);
            var bson = new BsonDocument();

            bson.Add("count", list.Count);

            var array = new BsonArray(list);
            bson.Add("items", array);

            return Content(bson.ToJson(jsonWriterSettings), JSON_CONTENT_TYPE);
        }

        [HttpGet("/api/{entity}/{id}")]
        public async Task<IActionResult> Get(EntityEnum entity, string id)
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

        [HttpPost("/api/{entity}")]
        public void Post(EntityEnum entity, [FromBody]string value)
        {
        }

        [HttpPut("/api/{entity}/{id}")]
        public async Task<IActionResult> Put(EntityEnum entity, string id, [FromBody]string value)
        {
            if (entity == EntityEnum.undefined)
                return NotFound();

            var bson = BsonDocument.Parse(value);

            await entityRepo.Put(entity, id, bson);

            return Ok();
        }

        [HttpDelete("/api/{entity}/{id}")]
        public void Delete(EntityEnum entity, int id)
        {
        }
    }
}

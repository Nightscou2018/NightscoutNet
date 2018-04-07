using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Services;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;

namespace API.Controllers
{
    public class ApiController : Controller
    {
        private IEntityRepository entityRepo;

        public ApiController(IEntityRepository entityRepo)
        {
            this.entityRepo = entityRepo;
        }

        [HttpGet("/api/{entity}")]
        public async Task<IActionResult> Get(EntityEnum entity)
        {
            var list = await entityRepo.List(entity);
            var bson = new BsonDocument();

            bson.Add("count", list.Count);

            var array = new BsonArray(list);
            bson.Add("items", array);

            return Ok(bson.ToString());
        }

        [HttpGet("/api/{entity}/{id}")]
        public string Get(EntityEnum entity, int id)
        {
            return "value";
        }

        [HttpPost("/api/{entity}")]
        public void Post(EntityEnum entity, [FromBody]string value)
        {
        }

        [HttpPut("/api/{entity}/{id}")]
        public void Put(EntityEnum entity, int id, [FromBody]string value)
        {
        }

        [HttpDelete("/api/{entity}/{id}")]
        public void Delete(EntityEnum entity, int id)
        {
        }
    }
}

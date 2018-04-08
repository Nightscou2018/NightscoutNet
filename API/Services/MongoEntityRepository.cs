using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.Services
{
    public class MongoEntityRepository : IEntityRepository
    {
        private MongoUrl mongoURL = null;
        private IMongoDatabase mongoDB;

        public MongoEntityRepository(string mongoURL)
        {
            this.mongoURL = new MongoUrl(mongoURL);
            var url = new MongoUrl(mongoURL);
            var client = new MongoClient(mongoURL);
            mongoDB = client.GetDatabase(this.mongoURL.DatabaseName);
        }


        public async Task<List<BsonDocument>> List(EntityEnum entity, int? count)
        {
            if (count == null)
            {
                count = 50;
            }

            var col = mongoDB.GetCollection<BsonDocument>(entity.ToString());
            var list = await col.Find(new BsonDocument()).Limit(count).ToListAsync();
            return list;
        }

        public async Task<BsonDocument> Get(EntityEnum entity, string id)
        {
            var col = mongoDB.GetCollection<BsonDocument>(entity.ToString());

            var filter_id = Builders<BsonDocument>.Filter.Eq("_id", ObjectId.Parse(id));

            var items = await col.FindAsync(filter_id);

            return items.FirstOrDefault();
        }

        public async Task Put(EntityEnum entity, string id, BsonDocument doc)
        {
            var col = mongoDB.GetCollection<BsonDocument>(entity.ToString());

            var filter_id = Builders<BsonDocument>.Filter.Eq("_id", ObjectId.Parse(id));

            var result = await col.FindOneAndReplaceAsync(filter_id, doc);
        }
    }
}

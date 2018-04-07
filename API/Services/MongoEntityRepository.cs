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
        private string mongoDBName = null;
        private MongoClient client;
        private IMongoDatabase mongoDB;

        public MongoEntityRepository(string mongoURL)
        {
            this.mongoURL = new MongoUrl(mongoURL);
            var url = new MongoUrl(mongoURL);
            client = new MongoClient(mongoURL);
            mongoDB = client.GetDatabase(this.mongoURL.DatabaseName);
        }


        public async Task<List<BsonDocument>> List(EntityEnum entity, int count)
        {
            var col = mongoDB.GetCollection<BsonDocument>(entity.ToString());
            var list = await col.Find(new BsonDocument()).Limit(count).ToListAsync();
            return list;
        }
    }
}

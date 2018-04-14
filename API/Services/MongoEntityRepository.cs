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

        public void Init()
        {
            foreach (EntityEnum entity in Enum.GetValues(typeof(EntityEnum)))
            {
                if (entity != EntityEnum.undefined)
                {
                    var col = mongoDB.GetCollection<BsonDocument>(entity.ToString());

                    col.Indexes.CreateOne(new BsonDocument(Constants.MODIFIED_ELEMENT, 1), 
                        new CreateIndexOptions() { Sparse = true });

                    col.Indexes.CreateOne(new BsonDocument(Constants.STATUS_ELEMENT, 1),
                        new CreateIndexOptions() { Sparse = true });
                }
            }
        }

        public async Task<List<BsonDocument>> List(EntityEnum entity, int? count, bool includeDeleted, DateTime? fromModified)
        {
            if (count == null)
            {
                count = 50;
            }

            var col = mongoDB.GetCollection<BsonDocument>(entity.ToString());

            var builder = Builders<BsonDocument>.Filter;
            FilterDefinition<BsonDocument> filter = null;

            if (!includeDeleted)
            {
                filter = AddFilter(filter, builder.Ne(Constants.STATUS_ELEMENT, StatusEnum.Deleted.ToString().ToLower()));
            }

            if (fromModified != null)
            {
                filter = AddFilter(filter, builder.Gte(Constants.MODIFIED_ELEMENT, fromModified.Value));
            }

            var list = await col.Find(filter ?? new BsonDocument())
                .SortBy(bson => bson[Constants.MODIFIED_ELEMENT])
                .Limit(count)
                .ToListAsync();
            return list;
        }

        private FilterDefinition<BsonDocument> AddFilter(FilterDefinition<BsonDocument> filter, 
            FilterDefinition<BsonDocument> newFilterClause)
        {
            return
                filter == null 
                    ? newFilterClause
                    : filter & newFilterClause;
        }

        public async Task<BsonDocument> Get(EntityEnum entity, string id)
        {
            var col = mongoDB.GetCollection<BsonDocument>(entity.ToString());

            var filter_id = Builders<BsonDocument>.Filter.Eq("_id", ObjectId.Parse(id));

            var items = await col.FindAsync(filter_id);

            return items.FirstOrDefault();
        }

        public async Task<ObjectId> Create(EntityEnum entity, BsonDocument doc)
        {
            var col = mongoDB.GetCollection<BsonDocument>(entity.ToString());

            await col.InsertOneAsync(doc);

            var idElement = doc.GetElement("_id");

            return idElement.Value.AsObjectId;
        }

        public async Task<bool> Update(EntityEnum entity, BsonDocument doc)
        {
            var col = mongoDB.GetCollection<BsonDocument>(entity.ToString());

            var filter_id = Builders<BsonDocument>.Filter.Eq("_id", doc.GetValue("_id").AsObjectId);

            var result = await col.FindOneAndReplaceAsync(filter_id, doc);

            return result != null;
        }

        public async Task<bool> Delete(EntityEnum entity, string id)
        {
            var col = mongoDB.GetCollection<BsonDocument>(entity.ToString());

            var filter_id = Builders<BsonDocument>.Filter.Eq("_id", new ObjectId(id));

            var result = await col.FindOneAndDeleteAsync(filter_id);

            return result != null;
        }
    }
}

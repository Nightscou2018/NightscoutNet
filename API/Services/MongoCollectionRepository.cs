using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.Services
{
    public class MongoCollectionRepository : ICollectionRepository
    {
        private MongoUrl mongoURL = null;
        private IMongoDatabase mongoDB;

        public MongoCollectionRepository(string mongoURL)
        {
            this.mongoURL = new MongoUrl(mongoURL);
            var url = new MongoUrl(mongoURL);
            var client = new MongoClient(mongoURL);
            mongoDB = client.GetDatabase(this.mongoURL.DatabaseName);
        }

        public void Init()
        {
            var filter = new BsonDocument("name", CollectionEnum.deleted.ToString());
            var collections = mongoDB.ListCollections(new ListCollectionsOptions { Filter = filter });
            if (!collections.Any())
            {
                mongoDB.CreateCollection(CollectionEnum.deleted.ToString());
            }

            foreach (CollectionEnum collection in Enum.GetValues(typeof(CollectionEnum)))
            {
                if (collection != CollectionEnum.undefined)
                {
                    var col = mongoDB.GetCollection<BsonDocument>(collection.ToString());

                    col.Indexes.CreateOne(new BsonDocument(Const.MODIFIED_ELEMENT, 1),
                        new CreateIndexOptions() { Sparse = true });

                    col.Indexes.CreateOne(new BsonDocument(Const.STATUS_ELEMENT, 1),
                        new CreateIndexOptions() { Sparse = true });
                }
            }
        }

        public async Task<List<BsonDocument>> List(CollectionEnum collection, int? count, bool includeDeleted, DateTime? fromModified)
        {
            if (count == null)
            {
                count = 50;
            }

            var col = mongoDB.GetCollection<BsonDocument>(collection.ToString());

            var builder = Builders<BsonDocument>.Filter;
            FilterDefinition<BsonDocument> filter = null;

            if (fromModified != null)
            {
                filter = AddFilter(filter, builder.Gt(Const.MODIFIED_ELEMENT, fromModified.Value));
            }

            var list = await col.Find(filter ?? new BsonDocument())
                .SortBy(bson => bson[Const.MODIFIED_ELEMENT])
                .Limit(count)
                .ToListAsync();

            if (includeDeleted)
            {
                // TODO list deleted and merge
            }

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

        public async Task<BsonDocument> Get(CollectionEnum collection, string id)
        {
            var col = mongoDB.GetCollection<BsonDocument>(collection.ToString());

            var filter_id = Builders<BsonDocument>.Filter.Eq(Const.ID, ObjectId.Parse(id));

            var items = await col.FindAsync(filter_id);

            return items.FirstOrDefault();
        }

        public async Task<ObjectId> Create(CollectionEnum collection, BsonDocument doc)
        {
            var col = mongoDB.GetCollection<BsonDocument>(collection.ToString());

            await col.InsertOneAsync(doc);

            var idElement = doc.GetElement(Const.ID);

            return idElement.Value.AsObjectId;
        }

        public async Task<bool> Update(CollectionEnum collection, BsonDocument doc)
        {
            var col = mongoDB.GetCollection<BsonDocument>(collection.ToString());

            var filter_id = Builders<BsonDocument>.Filter.Eq(Const.ID, doc.GetValue(Const.ID).AsObjectId);

            var result = await col.FindOneAndReplaceAsync(filter_id, doc);

            return result != null;
        }

        public async Task<bool> Delete(CollectionEnum collection, string id)
        {
            var col = mongoDB.GetCollection<BsonDocument>(collection.ToString());
            var colDeleted = mongoDB.GetCollection<BsonDocument>(CollectionEnum.deleted.ToString());

            var filter_id = Builders<BsonDocument>.Filter.Eq("_id", new ObjectId(id));

            var documentToDelete = await col.FindOneAndDeleteAsync(filter_id);

            if (documentToDelete == null)
            {
                var alreadyDeleted = await colDeleted.FindAsync(filter_id);

                return alreadyDeleted.Any();

            }

            documentToDelete.SetElement(new BsonElement (Const.STATUS_ELEMENT, StatusEnum.Deleted.ToString().ToLower()));

            documentToDelete.SetElement(new BsonElement(Const.MODIFIED_ELEMENT, DateTime.Now));

            var deletionRecord = new BsonDocument {
                { Const.ID, documentToDelete.GetValue(Const.ID) },
                { Const.COLLECTION_ELEMENT, collection.ToString() },
                { "deleted", documentToDelete },
                { Const.STATUS_ELEMENT, StatusEnum.New.ToString().ToLower() },
                { Const.MODIFIED_ELEMENT, DateTime.Now }
            };

            await colDeleted.InsertOneAsync(deletionRecord);

            return true;
        }
    }
}

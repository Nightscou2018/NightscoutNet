using API.Helpers;
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
                }
            }
        }

        public async Task<List<BsonDocument>> List(CollectionEnum collection, DateTime? fromModified, int? count, bool includeDeleted)
        {
            count = count ?? Const.DEFAULT_COUNT;

            List<BsonDocument> list = null;
            var col = mongoDB.GetCollection<BsonDocument>(collection.ToString());
            var builder = Builders<BsonDocument>.Filter;
            FilterDefinition<BsonDocument> filter = null;

            if (fromModified != null)
            {
                filter = AddFilter(filter, builder.Gt(Const.MODIFIED_ELEMENT, DateTimeHelper.ToEpoch(fromModified.Value)));
            }

            list = await col.Find(filter ?? new BsonDocument())
                .SortBy(bson => bson[Const.MODIFIED_ELEMENT])
                .Limit(count)
                .ToListAsync();

            foreach (var item in list)
            {
                UnpackId(item);
            }

            list.AddRange(await ListFallback(collection, count, fromModified));

            if (includeDeleted)
            {
                await ListDeleted(collection, count, list, builder, filter);
            }

            return list
                .Take(count.Value)
                .OrderBy(bson => bson[Const.MODIFIED_ELEMENT])
                .ToList();
        }

        private async Task<List<BsonDocument>> ListFallback(CollectionEnum collection, int? count, DateTime? fromModified)
        {
            List<BsonDocument> list = new List<BsonDocument>();
            try
            {
                var col = mongoDB.GetCollection<BsonDocument>(collection.ToString());

                var builder = Builders<BsonDocument>.Filter;
                FilterDefinition<BsonDocument> filterFallback = null;

                if (collection == CollectionEnum.entries)
                {
                    filterFallback = AddFilter(filterFallback, builder.Gt(Const.DATE_ELEMENT,
                        DateTimeHelper.ToEpoch(fromModified)));
                }
                else
                {
                    filterFallback = AddFilter(filterFallback, builder.Gt(Const.CREATED_AT_ELEMENT,
                        fromModified.Value.ToString(Const.DATE_WEB_FORMAT)));
                }
                filterFallback = AddFilter(filterFallback, builder.Exists(Const.MODIFIED_ELEMENT, exists: false));

                List<BsonDocument> listFallback = null;
                if (collection == CollectionEnum.entries)
                {
                    listFallback = await col.Find(filterFallback)
                        .SortBy(bson => bson[Const.DATE_ELEMENT])
                        .Limit(count)
                        .ToListAsync();
                }
                else
                {
                    listFallback = await col.Find(filterFallback)
                        .SortBy(bson => bson[Const.CREATED_AT_ELEMENT])
                        .Limit(count)
                        .ToListAsync();
                }

                foreach (var fallbackDoc in listFallback)
                {
                    try
                    {
                        UnpackId(fallbackDoc);

                        var created = GetCreatedEpoch(collection, fallbackDoc);
                        if (created.HasValue)
                        {
                            SetDate(fallbackDoc, Const.MODIFIED_ELEMENT, created);
                        }
                        list.Add(fallbackDoc);
                    }
                    catch (Exception ex)
                    {
                        // TODO warning
                    }
                }
            }
            catch (Exception ex)
            {
                // TODO warning
            }

            return list;
        }

        private async Task ListDeleted(CollectionEnum collection, int? count,
            List<BsonDocument> listExisting,
            FilterDefinitionBuilder<BsonDocument> builder,
            FilterDefinition<BsonDocument> filterExisting)
        {
            var colDeleted = mongoDB.GetCollection<BsonDocument>(CollectionEnum.deleted.ToString());

            var filterDeleted = AddFilter(filterExisting, builder.Eq(Const.COLLECTION_ELEMENT, collection.ToString()));

            var listDeleted = await colDeleted.Find(filterDeleted ?? new BsonDocument())
              .SortBy(bson => bson[Const.MODIFIED_ELEMENT])
                .Limit(count)
                .Project(bson => bson.GetElement(Const.DELETED_ELEMENT))
                .ToListAsync();

            foreach (var valueDeleted in listDeleted)
            {
                var bsonDeleted = valueDeleted.Value.AsBsonDocument;
                UnpackId(bsonDeleted);

                listExisting.Add(bsonDeleted);
            }
        }

        public async Task<BsonDocument> Get(CollectionEnum collection, string id)
        {
            var col = mongoDB.GetCollection<BsonDocument>(collection.ToString());

            var filter_id = Builders<BsonDocument>.Filter.Eq(Const.ID, ObjectId.Parse(id));

            var items = await col.FindAsync(filter_id);

            var doc = items.FirstOrDefault();
            if (doc == null)
                return null;

            UnpackId(doc);

            var created = GetCreatedEpoch(collection, doc);
            if (created.HasValue)
            {
                SetDate(doc, Const.MODIFIED_ELEMENT, created);
            }

            return doc;
        }

        public async Task<string> Create(CollectionEnum collection, BsonDocument doc)
        {
            SetDate(doc, Const.MODIFIED_ELEMENT, DateTime.Now);

            var col = mongoDB.GetCollection<BsonDocument>(collection.ToString());

            await col.InsertOneAsync(doc);

            var idElement = doc.GetElement(Const.ID);

            return idElement.Value.AsObjectId.ToString();
        }

        public async Task<bool> Update(CollectionEnum collection, BsonDocument doc)
        {
            var modified = GetCreatedEpoch(collection, doc);
            if (modified.HasValue)
            {
                SetDate(doc, Const.CREATED_ELEMENT, modified);
            }

            SetDate(doc, Const.MODIFIED_ELEMENT, DateTime.Now);
            PackId(doc);

            var col = mongoDB.GetCollection<BsonDocument>(collection.ToString());

            var filter_id = Builders<BsonDocument>.Filter.Eq(Const.ID, new ObjectId(doc.GetValue(Const.ID).ToString()));

            var result = await col.FindOneAndReplaceAsync(filter_id, doc);

            return result != null;
        }

        public async Task<bool> Delete(CollectionEnum collection, string id)
        {
            var objectId = new ObjectId(id);
            var col = mongoDB.GetCollection<BsonDocument>(collection.ToString());
            var colDeleted = mongoDB.GetCollection<BsonDocument>(CollectionEnum.deleted.ToString());

            var filter_id = Builders<BsonDocument>.Filter.Eq(Const.ID, objectId);

            var documentToDelete = await col.FindOneAndDeleteAsync(filter_id);

            if (documentToDelete == null)
            {
                var alreadyDeleted = await colDeleted.FindAsync(filter_id);

                return alreadyDeleted.Any();
            }

            UnpackId(documentToDelete);

            var created = GetCreatedEpoch(collection, documentToDelete);
            if (created.HasValue && !documentToDelete.Contains(Const.CREATED_ELEMENT))
            {
                SetDate(documentToDelete, Const.CREATED_ELEMENT, created);
            }
            SetDate(documentToDelete, Const.MODIFIED_ELEMENT, DateTime.Now);
            SetDate(documentToDelete, Const.DELETED_ELEMENT, DateTime.Now);

            var deletionRecord = new BsonDocument {
                { Const.ID, objectId },
                { Const.COLLECTION_ELEMENT, collection.ToString() },
                { Const.DELETED_ELEMENT, documentToDelete },
            };
            SetDate(deletionRecord, Const.MODIFIED_ELEMENT, DateTime.Now);

            await colDeleted.InsertOneAsync(deletionRecord);

            return true;
        }

        public async Task<BsonDocument> GetLastProfileSwitch()
        {
            var collectionEnum = CollectionEnum.treatments;

            var col = mongoDB.GetCollection<BsonDocument>(collectionEnum.ToString());

            var filter = Builders<BsonDocument>.Filter.Eq(Const.EVENT_TYPE, Const.PROFILE_SWITCH);

            var items = await col.Find(filter)
                .SortByDescending(bson => bson[Const.CREATED_AT_ELEMENT])
                .Limit(1)
                .ToListAsync();

            var doc = items.FirstOrDefault();
            if (doc == null)
                return null;

            UnpackId(doc);

            var created = GetCreatedEpoch(collectionEnum, doc);
            if (created.HasValue)
            {
                SetDate(doc, Const.MODIFIED_ELEMENT, created);
            }

            return doc;
        }

        public async Task<BsonDocument> FindDuplicate(CollectionEnum collectionEnum, BsonDocument doc)
        {
            var col = mongoDB.GetCollection<BsonDocument>(collectionEnum.ToString());

            var builder = Builders<BsonDocument>.Filter;
            FilterDefinition<BsonDocument> filter = null;

            if (doc.Contains(Const.ID))
            {
                string id = doc.GetValue(Const.ID).AsString;

                filter = AddFilter(filter, builder.Eq(Const.ID, new ObjectId(id)));
            }

            if (collectionEnum == CollectionEnum.entries && doc.Contains(Const.DATE_ELEMENT))
            {
                double date = doc.GetValue(Const.DATE_ELEMENT).AsInt64;

                filter = AddFilter(filter, builder.Gt(Const.DATE_ELEMENT, date - 1000));
                filter = AddFilter(filter, builder.Lt(Const.DATE_ELEMENT, date + 1000));
            }

            if (filter == null)
                return null;

            var list = await col.Find(filter)    
                .Limit(1)
                .ToListAsync();

            if (list.Count > 0)
            {
                var existingDoc = list[0];
                UnpackId(existingDoc);
                return existingDoc;
            }
            else
            {
                return null;
            }
        }

        public async Task<BsonDocument> GetLastProfilesDocument()
        {
            var col = mongoDB.GetCollection<BsonDocument>(CollectionEnum.profile.ToString());

            var builder = Builders<BsonDocument>.Filter;
            FilterDefinition<BsonDocument> filter = null;

            var list = await col.Find(filter ?? new BsonDocument())
                .SortByDescending(bson => bson[Const.START_DATE])
                .Limit(1)
                .ToListAsync();

            if (list.Count > 0)
            {
                var existingDoc = list[0];
                UnpackId(existingDoc);
                return existingDoc;
            }
            else
            {
                return null;
            }
        }

        private FilterDefinition<BsonDocument> AddFilter(FilterDefinition<BsonDocument> filter,
            FilterDefinition<BsonDocument> newFilterClause)
        {
            return
                filter == null
                    ? newFilterClause
                    : filter & newFilterClause;
        }

        private void UnpackId(BsonDocument doc)
        {
            if (doc.Contains(Const.ID))
            {
                BsonValue idValue = doc.GetValue(Const.ID);
                if (idValue.IsObjectId)
                {
                    doc.Remove(Const.ID);
                    doc.Set(Const.ID, idValue.ToString());
                }
            }
        }

        private void PackId(BsonDocument doc)
        {
            if (doc.Contains(Const.ID))
            {
                BsonValue idValue = doc.GetValue(Const.ID);
                if (idValue.IsString)
                {
                    doc.Remove(Const.ID);
                    doc.Set(Const.ID, new ObjectId(idValue.AsString));
                }
            }
        }

        private long? GetCreatedEpoch(CollectionEnum collection, BsonDocument doc)
        {
            if (doc.Contains(Const.CREATED_ELEMENT))
            {
                return doc[Const.CREATED_ELEMENT].AsInt64;
            }

            if (doc.Contains(Const.MODIFIED_ELEMENT))
            {
                return doc[Const.MODIFIED_ELEMENT].AsInt64;
            }

            if (collection == CollectionEnum.entries && doc.Contains(Const.DATE_ELEMENT))
            {
                return (long)doc.GetValue(Const.DATE_ELEMENT).AsDouble;
            }

            if (doc.Contains(Const.CREATED_AT_ELEMENT))
            {
                string createdAtString = doc.GetValue(Const.CREATED_AT_ELEMENT).AsString;
                DateTime createdAt;
                if (DateTime.TryParse(createdAtString, out createdAt))
                {
                    return DateTimeHelper.ToEpoch(createdAt.ToUniversalTime());
                }
            }

            return null;
        }

        private void SetDate(BsonDocument bson, string elementName, DateTime? date)
        {
            BsonElement el = new BsonElement(elementName, DateTimeHelper.ToEpoch(date));
            bson.SetElement(el);
        }

        private void SetDate(BsonDocument bson, string elementName, long? epoch)
        {
            BsonElement el = new BsonElement(elementName, epoch);
            bson.SetElement(el);
        }
    }
}

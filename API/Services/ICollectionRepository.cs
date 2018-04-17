using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.Services
{
    public enum StatusEnum
    {
        New,
        Modified,
        Deleted
    }

    public enum CollectionEnum {
        undefined,
        devicestatus,
        entries,
        food,
        profile,
        treatments,
        deleted
    }

    public interface ICollectionRepository
    {
        void Init();

        Task<List<BsonDocument>> List(CollectionEnum entity, int? count, bool includeDeleted, DateTime? fromModified);

        Task<BsonDocument> Get(CollectionEnum entity, string id);

        Task<ObjectId> Create(CollectionEnum entity, BsonDocument bson);

        Task<bool> Update(CollectionEnum entity, BsonDocument doc);

        Task<bool> Delete(CollectionEnum entity, string id);
    }
}

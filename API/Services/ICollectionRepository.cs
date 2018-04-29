using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.Services
{
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

        Task<List<BsonDocument>> List(CollectionEnum collectionEnum, DateTime? fromModified, int? count, bool includeDeleted);

        Task<BsonDocument> Get(CollectionEnum collectionEnum, string id);

        Task<string> Create(CollectionEnum collectionEnum, BsonDocument bson);

        Task<bool> Update(CollectionEnum collectionEnum, BsonDocument doc);

        Task<bool> Delete(CollectionEnum collectionEnum, string id);

        Task<BsonDocument> FindDuplicate(CollectionEnum collectionEnum, BsonDocument doc);

        Task<BsonDocument> GetLastProfileSwitch();

        Task<BsonDocument> GetLastProfilesDocument();
    }
}

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

    public enum EntityEnum {
        undefined,
        devicestatus,
        entries,
        food,
        profile,
        treatments
    }

    public interface IEntityRepository
    {
        Task<List<BsonDocument>> List(EntityEnum entity, int? count);

        Task<BsonDocument> Get(EntityEnum entity, string id);

        Task<ObjectId> Create(EntityEnum entity, BsonDocument bson);

        Task<bool> Update(EntityEnum entity, BsonDocument doc);

        Task<bool> Delete(EntityEnum entity, string id);
    }
}

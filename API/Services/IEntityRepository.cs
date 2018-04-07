using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.Services
{
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
        Task<List<BsonDocument>> List(EntityEnum entity, int count = 50);
    }
}

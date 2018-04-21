using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace API.Services
{
    public class Const
    {
        public const string ID = "_id";

        public const string STATUS_ELEMENT = "state";

        public const string MODIFIED_ELEMENT = "modified";

        public static string COLLECTION_ELEMENT = "collection";

        public const string DELETED_ELEMENT = "deleted";

        public static readonly char[] PARAM_SEPARATORS = new char[] { ',', ';', '|' };

        public static readonly DateTime MIN_DATE = new DateTime(1900, 1, 1);
    }
}

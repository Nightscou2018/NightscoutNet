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

        public static readonly char[] PARAM_SEPARATORS = new char[] { ',', ';', '|' };
    }
}

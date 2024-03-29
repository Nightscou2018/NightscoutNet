﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace API.Services
{
    public class Const
    {
        public const string ID = "_id";

        public const int DEFAULT_COUNT = 100;

        public const string MODIFIED_ELEMENT = "modified";

        public const string CREATED_ELEMENT = "created";

        public const string DELETED_ELEMENT = "deleted";

        public const string FROM = "from";

        public const string COUNT = "count";

        public const string CREATED_AT_ELEMENT = "created_at";

        public const string DATE_ELEMENT = "date";

        public const string DATESTRING_ELEMENT = "dateString";

        public static string COLLECTION_ELEMENT = "collection";

        public static string EVENT_TYPE = "eventType";

        public static string PROFILE_SWITCH = "Profile Switch";

        public static string START_DATE = "startDate";

        public static readonly char[] PARAM_SEPARATORS = new char[] { ',', ';', '|' };

        public static readonly DateTime MIN_DATE = new DateTime(1900, 1, 1);

        public static string DATE_WEB_FORMAT = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'";

        public static string API_SECRET_HEADER = "api-secret";
    }
}

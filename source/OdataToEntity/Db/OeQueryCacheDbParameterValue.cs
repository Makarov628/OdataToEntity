﻿using System;

namespace OdataToEntity.Db
{
    public struct OeQueryCacheDbParameterValue
    {
        public OeQueryCacheDbParameterValue(String parameterName, Object parameterValue)
        {
            ParameterName = parameterName;
            ParameterValue = parameterValue;
        }

        public String ParameterName { get; }
        public Object ParameterValue { get; }
    }
}

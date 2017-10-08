﻿using Microsoft.EntityFrameworkCore;
using System;
using OdataToEntity.Db;
using System.Collections.Generic;

namespace OdataToEntity.EfCore
{
    public class OeEfCorePostgreSqlDataAdapter<T> : OeEfCoreDataAdapter<T> where T : DbContext
    {
        private sealed class OeEfCorePostgreSqlOperationAdapter : OeEfCoreOperationAdapter
        {
            public OeEfCorePostgreSqlOperationAdapter(Type dataContextType, OeEntitySetMetaAdapterCollection entitySetMetaAdapters)
                : base(dataContextType, entitySetMetaAdapters)
            {
            }

            public override OeAsyncEnumerator ExecuteProcedure(object dataContext, string operationName, IReadOnlyList<KeyValuePair<string, object>> parameters, Type returnType)
            {
                return base.ExecuteFunction(dataContext, operationName, parameters, returnType);
            }
        }

        public OeEfCorePostgreSqlDataAdapter() : this(null, null)
        {
        }
        public OeEfCorePostgreSqlDataAdapter(DbContextOptions options, OeQueryCache queryCache)
            : base(options, queryCache, new OeEfCorePostgreSqlOperationAdapter(typeof(T), _entitySetMetaAdapters))
        {
        }
    }
}

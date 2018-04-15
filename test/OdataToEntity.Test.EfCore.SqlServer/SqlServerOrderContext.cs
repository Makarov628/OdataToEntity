﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System;

namespace OdataToEntity.Test.Model
{
    public sealed partial class OrderContext : DbContext
    {
        internal OrderContext() : this(CreateOptions())
        {
        }

        public static OrderContext Create(String dummy)
        {
            return new OrderContext(CreateOptions());
        }
        internal static DbContextOptions CreateOptions()
        {
            var optionsBuilder = new DbContextOptionsBuilder<OrderContext>();
            optionsBuilder.UseSqlServer(@"Server=.\sqlexpress;Initial Catalog=OdataToEntity;Trusted_Connection=Yes;", opt => opt.UseRelationalNulls());
            return optionsBuilder.Options;
        }

        public static String GenerateDatabaseName() => "dummy";
    }

}
﻿using System;
using System.Data.Common;
using System.Data.SqlClient;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Nop.Core.Data;
using Nop.Data.Initializers;

namespace Nop.Data
{
    public class SqliteDataProvider : IDataProvider
    {
        private string dataConnectionString;

        DbContextOptions<DbContext> _options;

        public SqliteDataProvider(string dataConnectionString)
        {
            this.dataConnectionString = ReplaceDataDirectory(dataConnectionString);
            DbContextOptionsBuilder<DbContext> builder2 = new DbContextOptionsBuilder<DbContext>();
            builder2.UseSqlite(this.dataConnectionString);
            _options = builder2.Options;
        }

        public DbContextOptions<DbContext> GetOptions()
        {
            return _options;
        }

        /// <summary>
        /// Set database initializer
        /// </summary>
        public virtual void SetDatabaseInitializer(IDbContext context)
        {
            var initializer = new CreateDatabaseIfNotExists<NopObjectContext>();
            initializer.InitializeDatabase((NopObjectContext)context);
        }

        /// <summary>
        /// A value indicating whether this data provider supports stored procedures
        /// </summary>
        public virtual bool StoredProceduredSupported
        {
            get { return false; }
        }

        /// <summary>
        /// A value indicating whether this data provider supports backup
        /// </summary>
        public bool BackupSupported
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a support database parameter object (used by stored procedures)
        /// </summary>
        /// <returns>Parameter</returns>
        public virtual DbParameter GetParameter()
        {
            return new SqlParameter();
        }

        /// <summary>
        /// Maximum length of the data for HASHBYTES functions
        /// returns 0 if HASHBYTES function is not supported
        /// </summary>
        /// <returns>Length of the data for HASHBYTES functions</returns>
        public int SupportedLengthOfBinaryHash()
        {
            return 0; //HASHBYTES functions is missing in SQL CE
        }


        private static string ReplaceDataDirectory(string inputString)
        {
            string str = null;
            if (inputString != null)
                str = inputString.Trim();
            if (string.IsNullOrEmpty(inputString) || !inputString.Contains("|DataDirectory|"))
            {
                return str;
            }
            var data = AppDomain.CurrentDomain.GetData("DataDirectory") as string;
            if (string.IsNullOrEmpty(data))
            {
                data = AppDomain.CurrentDomain.BaseDirectory ?? Environment.CurrentDirectory;
            }
            if (string.IsNullOrEmpty(data))
            {
                data = string.Empty;
            }
            return inputString.Replace("|DataDirectory|", data);
        }
    }
}

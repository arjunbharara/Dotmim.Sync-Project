using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SynchronizationWithDotmim.Sync
{
    public  class DBHelp
    {
        public static  void AddNewColumnToAddressAsync(DbConnection c)
        {
            using (var command = c.CreateCommand())
            {
                command.CommandText = "ALTER TABLE Address add createDate datetime NULL";
                c.Open();
                command.ExecuteNonQuery();
                c.Close();
            }
        }

        public static void RemoveCreateDateColumnFromAddress(DbConnection c)
        {
            using (var command = c.CreateCommand())
            {
                command.CommandText = "ALTER TABLE Address DROP COLUMN createDate";
                c.Open();
                command.ExecuteNonQuery();
                c.Close();
            }
        }

    }
}

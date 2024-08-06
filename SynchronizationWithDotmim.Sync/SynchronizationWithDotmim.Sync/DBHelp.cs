using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
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

        public static int InsertOneAddressWithNewColumnAsync(SqlConnection c)
        {
            using (var command = c.CreateCommand())
            {
                command.CommandText = @"INSERT INTO [Address] 
                                    ([AddressLine1] ,[City],[StateProvince],[CountryRegion],[PostalCode], [createDate])
                                    VALUES 
                                    (@AddressLine1 ,@City, @StateProvince, @CountryRegion, @PostalCode, @createDate);
                                    Select SCOPE_IDENTITY() as AddressID";

                command.Parameters.AddWithValue("@AddressLine1", "1 barber avenue");
                command.Parameters.AddWithValue("@City", "Munitan");
                command.Parameters.AddWithValue("@StateProvince", "");
                command.Parameters.AddWithValue("@CountryRegion", "");
                command.Parameters.AddWithValue("@PostalCode", "0001");
                command.Parameters.AddWithValue("@createDate", DateTime.Now);

                c.Open();
                var addressId =  command.ExecuteScalar();
                c.Close();


                return Convert.ToInt32(addressId);
            }
        }


    }
}

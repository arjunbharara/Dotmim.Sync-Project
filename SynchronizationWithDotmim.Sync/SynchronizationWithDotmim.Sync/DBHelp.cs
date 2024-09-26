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

                command.Parameters.AddWithValue("@AddressLine1", "1 baner road");
                command.Parameters.AddWithValue("@City", "hinjewadi");
                command.Parameters.AddWithValue("@StateProvince", "");
                command.Parameters.AddWithValue("@CountryRegion", "");
                command.Parameters.AddWithValue("@PostalCode", "500049");
                command.Parameters.AddWithValue("@createDate", DateTime.Now);

                c.Open();
                var addressId =  command.ExecuteScalar();
                c.Close();


                return Convert.ToInt32(addressId);
            }
        }

        public static void EnsureDatabaseExists(string connectionString, string databaseName)
        {
            //connect to the master database
            string conn = connectionString.Replace("Initial Catalog=YourDatabaseName;", "Initial Catalog=master;");

            using (var connection = new SqlConnection(conn))
            {
                try
                {
                    connection.Open();

                    // Check if the database exists
                    bool databaseExists = false;
                    using (var command = new SqlCommand())
                    {
                        command.Connection = connection;
                        command.CommandText = @"IF EXISTS (SELECT name FROM sys.databases WHERE name = @databaseName)
                                            SELECT 1 ELSE SELECT 0;";
                        command.Parameters.AddWithValue("@databaseName", databaseName);

                        int result = (int)command.ExecuteScalar();
                        databaseExists = result == 1;
                    }

                    // If the database does not exist,create it
                    if (!databaseExists)
                    {
                        using (var command = new SqlCommand())
                        {
                            command.Connection = connection;
                            command.CommandText = $"CREATE DATABASE [{databaseName}]";
                            command.ExecuteNonQuery();
                            Console.WriteLine($"Database '{databaseName}' created successfully.");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Database '{databaseName}' already exists.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }


    }
}

using SynchronizationWithDotmim.Sync.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using System.Configuration;
using System.IO;
using System.Windows.Markup;
using Dotmim.Sync;
using System.Runtime.InteropServices;
using Microsoft.Build.Tasks;
using Microsoft.Win32;

namespace SynchronizationWithDotmim.Sync
{
    public  class Program
    {
        
        private static string secondaryConnectionString = null;
        private static string primaryConnectionString = null;
        private static readonly string scopeName = "HyworksScope";
        private static readonly string databaseName="AdventureWorks";
        private static string _serverType = null;
        private static string SchemaChange = "false";
        private static string connectionString=string.Empty;
        private static string ServerID = String.Empty;
        private static string Deprovision = null;
        //dependency injecton of service
        private static IDotmimSyncService syncService = new DotmimSyncService();
      
        public async static Task Main()
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            try {
                Console.WriteLine("Enter databse connection string");
                connectionString= Console.ReadLine();

                //assigning connection strings
                string sqlScript1 = @"
            SELECT 
                Server_Type,
                Connection_String,
                Server_Id
            FROM 
                databaseInfo; ";

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    SqlCommand cmd = new SqlCommand(sqlScript1, conn);
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string SType = reader["Server_Type"].ToString();
                            string connString = reader["Connection_String"].ToString();

                            if (SType == "primary")
                            {
                                primaryConnectionString = connString;
                                Console.WriteLine("primary " + connString);
                            }
                            else if (SType == "secondary")
                            {
                                secondaryConnectionString = connString;
                                Console.WriteLine("secondary " + connString);
                            }
                        }
                    }
                }

                //Initilizing the Databases with syncAgent.
                Console.WriteLine("Initializing the SyncAgent");
                syncService.InitializeAsync(secondaryConnectionString, primaryConnectionString);

                //setting the registry keys values
                assignKeys();

                if (Deprovision.Equals("true"))
                {
                   
                    await  syncService.DeprovisionAsync(scopeName);
                    return;
                }
                if (SchemaChange.Equals("true"))
                {
                    var tables = new string[] { "Department", "Employee", "Task" };
                    await DotmimSyncService.Reconfigure2(scopeName, connectionString, tables);
                   // await syncService.Recongiure(scopeName, connectionString, tables);
                    Console.WriteLine("Please start the syncing.");
                    return;
                }

                Task syncTask = Task.Run(async () =>
                {
                        while (!cts.Token.IsCancellationRequested)
                      {

                                try
                                {
                                await Sync();

                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine("Error during synchronization: " + ex.Message);
                                }
                            try
                            {
                                await Task.Delay(TimeSpan.FromSeconds(30), cts.Token);
                            }
                            catch (TaskCanceledException)
                            {
                                break;
                            }

                      }
                }, cts.Token);

                 Console.ReadKey();

                    // Cancel the synchronization loop
                    cts.Cancel();

                    // Wait for the task to complete
                    await syncTask;

                    Console.WriteLine("Synchronization stopped.");
              
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                // Prevent console from closing automatically
                Console.WriteLine("Press any key to exit...");
                Console.ReadLine();
            } 
        }
      
        //Assigning the Registry values
        public static void assignKeys()
        {
            //registry value names
            string valueName = "ServerId";
            string valueName2 = "SchemaChange";
            string valueName3 = "Deprovision";
            string registryKeyPath = @"DbSync";
            // Open the registry key
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(registryKeyPath))
            {
                if (key != null)
                {
                    // Retrieve the value
                    object value = key.GetValue(valueName);
                    object value2=key.GetValue(valueName2);
                    object value3=key.GetValue(valueName3);
                    if (value != null)
                    {

                        ServerID = value.ToString();
                       
                    }
                    else
                    {
                        Console.WriteLine(" ServerID Value not found.");
                    }
                    if (value2 != null)
                    {
                        SchemaChange = value2.ToString();
                        
                    }
                    else
                    {
                        Console.WriteLine("SchemaChange Value not found.");
                    }
                    if (value3 != null)
                    {
                        Deprovision=value3.ToString();
                    }
                    else
                    {
                        Console.WriteLine("Deprovision Value not found.");
                    }
                }
                else
                {
                    Console.WriteLine("Registry key not found.");
                }
            }
           
        }


        //Sync Method 
        public static  async Task Sync()
        {
            //checking if it is primary or secondary
            assignKeys(); 
            //Sql server script 
            string sqlScript = @"
            SELECT 
                Server_Type
               
            FROM 
                databaseInfo
            WHERE 
                Server_Id = @Server_Id;";

            //making connection to the database
            string serverType = String.Empty;
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand(sqlScript, conn);
                cmd.Parameters.AddWithValue("@Server_Id", ServerID);
                conn.Open();

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        serverType = reader["Server_Type"].ToString();
                        Console.WriteLine(serverType);
                    }
                    else
                    {
                        Console.WriteLine("No matching record found in the database.");
                    }
                }
            }

            //assining the scopeType for the firsttime
            if (_serverType == null)
            {
                _serverType = serverType;
            }
            // checking which side it is currently
            if (_serverType != serverType)
            {
                _serverType = serverType;
                changeMode();
            }
            else if (_serverType == "secondary"  && _serverType.Equals(serverType))
            {

                //database exist logic
                DBHelp.EnsureDatabaseExists(primaryConnectionString, databaseName);
                DBHelp.EnsureDatabaseExists(secondaryConnectionString, databaseName);
                //providing selected tables for setup
                var tables = new string[] { "Department", "Employee", "Task" };
                SyncResult res=await syncService.SyncDatabasesAsync(scopeName, tables);
                if (res.TotalChangesFailedToApplyOnServer > 0)
                {
                    syncService.SyncApplyRemoteFailed(res);
                }
                if (res.TotalChangesFailedToApplyOnClient > 0)
                {
                    syncService.SyncApplyLocalFailed(res);
                }
               
            }else if(_serverType=="primary" && _serverType.Equals(serverType))
            {
                Console.WriteLine("This is primary server.");
                return;
            }
           
        }
        //this method called when you change the mode of servers
        public static void changeMode()
        {
            Console.WriteLine("inside ChangeMode method");
            switchConnString(primaryConnectionString, secondaryConnectionString);
            syncService.ReInitialize(secondaryConnectionString, primaryConnectionString,scopeName);

        }

        public static void switchConnString(string ps, string sc)
        {
            string temp = ps;
            primaryConnectionString = sc;
            secondaryConnectionString = temp;
        }
    }
}

﻿using SynchronizationWithDotmim.Sync.Service;
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
    public class Program
    {

        private static string LocalConnectionString = null;
        private static string RemoteConnectionString = null;
        private static readonly string scopeName = "HyworksScope";
        private static readonly string scopeNameDCDR = "HyworksScope_DCDR";
      //private static string _serverType = null;
        private static string SchemaChange = "false";
        private static string connectionString = string.Empty;
        private static string ServerID = String.Empty;
        private static string Deprovision = "false";
        private static string DeprovisionDCDR = "false";
        //dependency injecton of service
        private static IDotmimSyncService syncService = new DotmimSyncService();

        public async static Task Main()
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            try
            {

                LocalConnectionString = ConfigurationManager.ConnectionStrings["LocalConnectionString"].ConnectionString;
                RemoteConnectionString = ConfigurationManager.ConnectionStrings["RemoteConnectionString"].ConnectionString;
                Console.WriteLine("Local connection string:" + LocalConnectionString);
                Console.WriteLine("Remote connection String:" + RemoteConnectionString);
                /*  Console.WriteLine("Enter databse connection string");
                  connectionString = Console.ReadLine();
                  //assigning connection strings
                  string sqlScript1 = @"
                 SELECT 
                     Connection_String,
                     Side
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
                              string SType = reader["Side"].ToString();
                              string connString = reader["Connection_String"].ToString();

                              if (SType == "Local")
                              {
                                  LocalConnectionString = connString;
                                  Console.WriteLine("primary " + connString);
                              }
                              else if (SType == "Remote")
                              {
                                  RemoteConnectionString = connString;
                                  Console.WriteLine("secondary " + connString);
                              }
                          }
                      }
                  }*/


                //Initilizing the Databases with syncAgent.
                Console.WriteLine("Initializing the SyncAgent");
                syncService.InitializeAsync(LocalConnectionString, RemoteConnectionString);

                //setting the registry keys values
                assignKeys();

                if (Deprovision.Equals("true"))
                {
                   
                    await syncService.DeprovisionAsync(scopeName);
                    return;
                }
                if (DeprovisionDCDR.Equals("true"))
                {

                    await syncService.DeprovisionAsync(scopeNameDCDR);
                    return;
                }
                if (SchemaChange.Equals("true"))
                {
                    var tables = new string[] { "Department", "Employee", "Task" };
                    await DotmimSyncService.Reconfigure2(scopeName, connectionString, tables);
                  //await syncService.Recongiure(scopeName, connectionString, tables);
                    Console.WriteLine("Please start the syncing.");
                    return;
                }
                string isSyncstr = ConfigurationManager.AppSettings["IsSync"];
                bool isSync;
                if (!bool.TryParse(isSyncstr, out isSync))
                {
                    Console.WriteLine($"Invalid IsSync Value.Unable to parse into boolean. vaue:- {isSyncstr}");
                }


                string IsDCDRSyncstr = ConfigurationManager.AppSettings["IsDCDRSync"];
                bool IsDCDRSync;
                if (!bool.TryParse(IsDCDRSyncstr, out IsDCDRSync))
                {
                    Console.WriteLine($"Invalid IsSync Value.Unable to parse into boolean. vaue:- {IsDCDRSyncstr}");
                }

                Task syncTask = Task.Run(async () =>
                {
                    while (true)
                    {

                        try
                        {
                            if (isSync)
                            {
                                Console.WriteLine("Syncing is enabled on the server.Starting Syncing process.");
                                await Sync();
                            }
                            else
                            {
                                Console.WriteLine("Syncing is disabled on the server");
                            }
                            if (IsDCDRSync)
                            {
                                Console.WriteLine("DC DR Syncing is enabled on the server.Starting Syncing process.");
                                await SyncDCDR();
                            }
                            else
                            {
                                Console.WriteLine("DC DR Syncing is disabled on the server");
                            }

                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Error during synchronization: " + ex.ToString());
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
            //  string valueName = "ServerId";
            //  string valueName2 = "SchemaChange";
            string valueDCDR = "DeprovisionDCDR";
            string valueName3 = "Deprovision";
            string registryKeyPath = @"DbSync";
            // Open the registry key
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(registryKeyPath))
            {
                if (key != null)
                {
                    // Retrieve the value
                   // object value = key.GetValue(valueName);
                    //object value2 = key.GetValue(valueName2);
                    object value3 = key.GetValue(valueName3);
                    object value4 = key.GetValue(valueDCDR);
                   /* if (value != null)
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
                    }*/
                    if (value3 != null)
                    {
                        Deprovision = value3.ToString();
                    }
                    else
                    {
                        Console.WriteLine("Deprovision Value not found.");
                    }
                    if (value4 != null)
                    {
                        DeprovisionDCDR = value4.ToString();
                    }
                    else
                    {
                        Console.WriteLine("DeprovisionDCDR Value not found.");
                    }
                }
                else
                {
                    Console.WriteLine("Registry key not found.");
                }
            }

        }


        //Sync Method 
        public static async Task Sync()
        {
            //checking if it is primary or secondary
            /*  assignKeys();
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
              }*/
            // checking which side it is currently
            /* if (_serverType != serverType)
             {
                 _serverType = serverType;
                 // changeMode();
             }*/
            /* if (_serverType == "secondary" && _serverType.Equals(serverType))
             {
                 //providing selected tables for setup
                 var tables = new string[] { "Department", "Employee", "Task" };
                 SyncResult res = await syncService.SyncDatabasesAsync(scopeName, tables);
                 if (res.TotalChangesFailedToApplyOnServer > 0)
                 {
                     syncService.SyncApplyRemoteFailed(res);
                 }
                 if (res.TotalChangesFailedToApplyOnClient > 0)
                 {
                     syncService.SyncApplyLocalFailed(res);
                 }

             }
             else if (_serverType == "primary" && _serverType.Equals(serverType))
             {
                 Console.WriteLine("This is primary server.");
                 return;
             }*/
          
            var tables = new string[] { "Products", "Customers", "Orders", "OrderDetails" };
            SyncResult res = await syncService.SyncDatabasesAsync(scopeName, tables);
            if (res.TotalChangesFailedToApplyOnServer > 0)
            {
                syncService.SyncApplyRemoteFailed(res);
            }
            if (res.TotalChangesFailedToApplyOnClient > 0)
            {
                syncService.SyncApplyLocalFailed(res);
            }

        }
        public static async Task SyncDCDR()
        {
            var tables = new string[] {"Products", "Customers", "Orders", "OrderDetails" };
            SyncResult res = await syncService.SyncDCDR(scopeNameDCDR, tables);
            if (res.TotalChangesFailedToApplyOnServer > 0)
            {
                syncService.SyncApplyRemoteFailed(res);
            }
            if (res.TotalChangesFailedToApplyOnClient > 0)
            {
                syncService.SyncApplyLocalFailed(res);
            }
        }
    }
}

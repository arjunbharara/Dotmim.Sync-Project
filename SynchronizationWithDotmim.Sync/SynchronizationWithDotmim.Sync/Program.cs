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

namespace SynchronizationWithDotmim.Sync
{
    public  class Program
    {
        //fetcching the connection string from the A[[.config file
        private static readonly string serverConnectionString = ConfigurationManager.ConnectionStrings["Conn1"].ConnectionString;
        private static readonly string clientConnectionString = ConfigurationManager.ConnectionStrings["Conn2"].ConnectionString;


        public async static Task Main()
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            try
            {
                Console.WriteLine(serverConnectionString);
                Console.WriteLine(clientConnectionString);
             

                IDotmimSyncService syncService = new DotmimSyncService();
                Console.WriteLine("Initializing...");
                syncService.InitializeAsync(clientConnectionString, serverConnectionString);

                //database exist logic



                string sqlScript = @"
                    USE AdventureWorks;
                    SELECT 
                         server_type
                    FROM 
                    databaseInfo;
                        ";

                string serverType = null;
                try
                {
                    using (SqlConnection connection = new SqlConnection(serverConnectionString))
                    {
                        SqlCommand command = new SqlCommand(sqlScript, connection);
                        connection.Open();
                        SqlDataReader reader = command.ExecuteReader();
                        if (reader.Read())
                        {
                            serverType = reader["server_type"] as string;
                        }
                    }
                }
                catch(Exception ex)
                {
                    Console.WriteLine("An error occurred: " + ex.Message);
                }
                // Output the server type
                Console.WriteLine($"Server Type: {serverType}");
                //checking the connection of local server
                try
                {
                    using (SqlConnection connection = new SqlConnection(clientConnectionString))
                    {
                        SqlCommand command = new SqlCommand(sqlScript, connection);
                        connection.Open();
                        SqlDataReader reader = command.ExecuteReader();
                        if (reader.Read())
                        {
                            serverType = reader["server_type"] as string;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("An error occurred: " + ex.Message);
                }

                // Output the server type
                Console.WriteLine($"Server Type: {serverType}");


                if (serverType == "primary server")
                {
                    Console.WriteLine("starting Provisioning");
                    await syncService.ProvisionAsync();

                }

                Task syncTask = Task.Run(async () =>
                {
                    while (!cts.Token.IsCancellationRequested)
                    {
                        try
                        {
                            await syncService.SyncDatabasesAsync();
                            Console.WriteLine("Synchronization completed successfully at {0}.", DateTime.Now);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Error during synchronization: " + ex.Message);
                        }
                        try
                        {
                            await Task.Delay(TimeSpan.FromMinutes(1), cts.Token);
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
    }
}

using SynchronizationWithDotmim.Sync.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SynchronizationWithDotmim.Sync
{
    public  class Program
    {
        private static string serverConnectionString = $" Data Source = (localdb)\\MSSQLLocalDB; Initial Catalog=AdventureWorks;Integrated Security=true;";
       
        private static string clientConnectionString = $" Data Source = (localdb)\\ProjectModels; Initial Catalog=AdventureWorks;Integrated Security=true;";
      
        public async static Task Main()
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            try
            {
                IDotmimSyncService syncService = new DotmimSyncService();

                Console.WriteLine("Initializing...");
                syncService.InitializeAsync(clientConnectionString, serverConnectionString);

                 Console.WriteLine("starting Provisioning");
               // await syncService.ProvisionAsync();

                //Migration with signle column
                await syncService.Recongiure();

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


                //Deprovisioning
               // await syncService.DeprovisionAsync();
                
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

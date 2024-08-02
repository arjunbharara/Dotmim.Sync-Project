using SynchronizationWithDotmim.Sync.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SynchronizationWithDotmim.Sync
{
    public  class Program
    {
        private static string serverConnectionString = $" Data Source = (localdb)\\MSSQLLocalDB; Initial Catalog=AdventureWorks;Integrated Security=true;";
       
        private static string clientConnectionString = $" Data Source = (localdb)\\MSSQLLocalDB; Initial Catalog=Client;Integrated Security=true;";

        public async static Task Main()
        {
            try
            {
                IDotmimSyncService syncService = new DotmimSyncService();

                Console.WriteLine("Initializing...");
                syncService.InitializeAsync(clientConnectionString, serverConnectionString);

                 Console.WriteLine("starting Provisioning");
                 await syncService.ProvisionAsync();

                await syncService.SyncDatabasesAsync();
               // await syncService.DeprovisionAsync();

                Console.WriteLine("Hello World!");
                Console.WriteLine("Synchronization completed successfully.");
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

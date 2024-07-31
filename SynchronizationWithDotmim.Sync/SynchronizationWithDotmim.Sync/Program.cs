using SynchronizationWithDotmim.Sync.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SynchronizationWithDotmim.Sync
{
    internal class Program
    {
        static void Main(string[] args)
        {
                string primaryConnectionString = "Server=LAPTOP-6326L1VA\\91745;Database=databaseA;Trusted_Connection=True;";
                string secondaryConnectionString = "Server=LAPTOP-6326L1VA\\91745;Database=databaseB;Trusted_Connection=True;";

                IDotmimSyncService syncService = new DotmimSyncService(primaryConnectionString, secondaryConnectionString);

                Console.WriteLine("Initializing...");
                 syncService.InitializeAsync();
        }
    }
}

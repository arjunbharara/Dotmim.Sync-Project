using Dotmim.Sync.SqlServer;
using Dotmim.Sync;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SynchronizationWithDotmim.Sync.Service
{
    public class DotmimSyncService : IDotmimSyncService
       {
        private readonly string _primaryConnectionString;
        private readonly string _secondaryConnectionString;
        private SyncSetup _syncSetup;
        private SqlSyncProvider _serverProvider;
        private SqlSyncProvider _clientProvider;
        private SyncAgent _syncAgent;

        public DotmimSyncService(string primaryConnectionString, string secondaryConnectionString)
            {
                _primaryConnectionString = primaryConnectionString;
                _secondaryConnectionString = secondaryConnectionString;
            }

        public async Task InitializeAsync()
        {
            // Define the sync setup with scope and tables
            _syncSetup = new SyncSetup("ExampleScope")
            {
                Tables = {"Employees"}
            };

            // Initialize providers with connection strings
            _serverProvider = new SqlSyncProvider(_primaryConnectionString);
            _clientProvider = new SqlSyncProvider(_secondaryConnectionString);

            // Initialize SyncAgent with providers and setup
            _syncAgent = new SyncAgent(_serverProvider, _clientProvider);

            Console.WriteLine("Initialization complete.");
        }

        Task IDotmimSyncService.DeprovisionAsync()
        {
            throw new NotImplementedException();
        }

        Task IDotmimSyncService.InitializeAsync()
        {
            throw new NotImplementedException();
        }

        Task IDotmimSyncService.ProvisionAsync()
        {
            throw new NotImplementedException();
        }

        Task IDotmimSyncService.SyncDatabasesAsync()
        {
            throw new NotImplementedException();
        }

        Task IDotmimSyncService.ValidateConfigurationAsync()
        {
            throw new NotImplementedException();
        }
    }
}

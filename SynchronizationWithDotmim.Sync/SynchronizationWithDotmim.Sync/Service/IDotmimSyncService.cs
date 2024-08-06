using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SynchronizationWithDotmim.Sync.Service
{
    internal interface IDotmimSyncService
    {
        void InitializeAsync(string sourceConnectionString, string destinationConnectionString);
        Task ProvisionAsync();
        void ValidateConfigurationAsync();
        Task SyncDatabasesAsync();
        Task DeprovisionAsync();
        Task Recongiure();
        void AddData();
    }
}

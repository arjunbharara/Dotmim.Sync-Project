using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SynchronizationWithDotmim.Sync.Service
{
    internal interface IDotmimSyncService
    {
        Task InitializeAsync();
        Task ProvisionAsync();
        Task ValidateConfigurationAsync();
        Task SyncDatabasesAsync();
        Task DeprovisionAsync();
    }
}

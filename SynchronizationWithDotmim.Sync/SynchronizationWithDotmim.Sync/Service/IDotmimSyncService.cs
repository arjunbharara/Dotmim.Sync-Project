using Dotmim.Sync;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SynchronizationWithDotmim.Sync.Service
{
    internal interface IDotmimSyncService
    {
        //Confuguring  the SyncAgent
        void InitializeAsync(string sourceConnectionString, string destinationConnectionString);
        //Provisioning the Database.
        Task Provision(string scopeName, string[] tables);
        //Syncing logic 
        Task<SyncResult> SyncDatabasesAsync(string scopeName, string[] tables);
        //Deprovisioning the databses.
        Task DeprovisionAsync(string scopeName);
        //Reconfiguring the databases.
        Task Reconfigure(string scopeName, string conn, string[] tables);
        //When Mode gets change reinitilizing the SyncAgent
        Task ReInitialize(string sourceConnectionString, string destinationConnectionString,string scopeNmae);
        //when changes failed to apply on server
        void SyncApplyRemoteFailed(SyncResult result);
        //When changes failed to apply on client
        void SyncApplyLocalFailed(SyncResult result);
    }
}
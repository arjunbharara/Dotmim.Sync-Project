using Dotmim.Sync.SqlServer;
using Dotmim.Sync;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dotmim.Sync.Enumerations;

namespace SynchronizationWithDotmim.Sync.Service
{
    public class DotmimSyncService : IDotmimSyncService
       {
        private  string _sourceConnectionString;
        private  string _destinationConnectionString;
        private SyncAgent _syncAgent;


        public void InitializeAsync(string sourceConnectionString, string destinationConnectionString)
        {
            _sourceConnectionString = sourceConnectionString;
            _destinationConnectionString = destinationConnectionString;

            var sourceProvider = new SqlSyncProvider(_sourceConnectionString);
            var destinationProvider = new SqlSyncProvider(_destinationConnectionString);

            _syncAgent = new SyncAgent(sourceProvider, destinationProvider);

          
        }

        public async Task  ProvisionAsync()
        {
            try {

                //provisioning the remote (server) side 
                var setup = new SyncSetup( "ProductModel", "Product",
                         "Address", "Customer", "CustomerAddress");

                // Provision everything needed by the setup
                await _syncAgent.RemoteOrchestrator.ProvisionAsync(setup);

                // Getting the server scope from server side
                var serverScope =await _syncAgent.RemoteOrchestrator.GetScopeInfoAsync();

                // Provision everything needed (sp, triggers, tracking tables, AND TABLES)
                  await  _syncAgent.LocalOrchestrator.ProvisionAsync(serverScope);

                Console.WriteLine("Provisioning Completed");
             }
             catch (Exception ex)
            {
                Console.WriteLine($"Error during provisioning: {ex.Message}");
            }
        }


        public async Task SyncDatabasesAsync()
        {
            try
            { 
                var result =await _syncAgent.SynchronizeAsync();

                Console.WriteLine(result);
                Console.WriteLine("Sync result:");
                Console.WriteLine($"Total changes applied on client: {result.ChangesAppliedOnClient}");
                Console.WriteLine($"Total changes applied on server: {result.ChangesAppliedOnServer}");
                Console.WriteLine($"Total resolved conflicts: {result.TotalResolvedConflicts}");
                Console.WriteLine($"Total comleted Time: {result.CompleteTime}");
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during synchronization: {ex.Message}");
            }
        }

         public async Task DeprovisionAsync()
        {
           //Deprovisioning server side
            // Deprovision everything
            var p = SyncProvision.ScopeInfo | SyncProvision.ScopeInfoClient |
                    SyncProvision.StoredProcedures | SyncProvision.TrackingTable |
                    SyncProvision.Triggers;

            // Deprovision everything
            await _syncAgent.RemoteOrchestrator.DeprovisionAsync(p);


            //deprovisioning client side
            // Deprovision everything
            await _syncAgent.LocalOrchestrator.DeprovisionAsync(p);
        }


        void IDotmimSyncService.ValidateConfigurationAsync()
        {
            throw new NotImplementedException();
        }


        //customizing the Scope table name
        public void CustomizeScopeInfo(string tableName)
        {
            var options = new SyncOptions();
            options.ScopeInfoTableName = tableName;
        }
    }
}

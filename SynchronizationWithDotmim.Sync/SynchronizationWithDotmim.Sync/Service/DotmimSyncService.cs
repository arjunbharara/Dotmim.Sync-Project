using Dotmim.Sync.SqlServer;
using Dotmim.Sync;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dotmim.Sync.Enumerations;
using System.Security.Cryptography.X509Certificates;

namespace SynchronizationWithDotmim.Sync.Service
{
    public class DotmimSyncService : IDotmimSyncService
       {
        private  string _sourceConnectionString;
        private  string _destinationConnectionString;
        private SyncAgent _syncAgent;
        private readonly string scopeName="AccopsScope";

        public void InitializeAsync(string sourceConnectionString, string destinationConnectionString)
        {
            _sourceConnectionString = sourceConnectionString;
            _destinationConnectionString = destinationConnectionString;

            var sourceProvider = new SqlSyncProvider(_sourceConnectionString);
            var destinationProvider = new SqlSyncProvider(_destinationConnectionString);
            var syncOptions = new SyncOptions
            {
                ScopeInfoTableName = scopeName
            };
            _syncAgent = new SyncAgent(sourceProvider, destinationProvider);

          
        }

        public async Task  ProvisionAsync()
        {
            try {

                //provisioning the remote (server) side 
                var tables = new string[] {"ProductModel",
                    "Product",
                    "Address", "Customer", "CustomerAddress",
                    "SalesOrderHeader" };

                var setup = new SyncSetup( tables);
                {
                  
                };
                // selecting which coloumns to add for synchronization.
                 setup.Tables["Product"].Columns.AddRange(new[] {"ProductId","Name","ProductNumber","Color", });
               
                // Provision everything needed by the setup
                await _syncAgent.RemoteOrchestrator.ProvisionAsync(scopeName, setup);

                // Getting the server scope from server side
                var serverScope =await _syncAgent.RemoteOrchestrator.GetScopeInfoAsync(scopeName);
                
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
                Console.WriteLine("Synchronization completed successfully.");
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
            //Deprovision everything
            var p = SyncProvision.ScopeInfo | SyncProvision.ScopeInfoClient |
                    SyncProvision.StoredProcedures | SyncProvision.TrackingTable |
                    SyncProvision.Triggers;

            // Deprovision everything
            await _syncAgent.RemoteOrchestrator.DeprovisionAsync(scopeName,p);
            Console.WriteLine($"DeProvisoned sucessfully server");

            //deprovisioning client side
            //Deprovision everything
            await _syncAgent.LocalOrchestrator.DeprovisionAsync(scopeName,p);
            Console.WriteLine($"DeProvisoned sucessfully clinet");
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

using Dotmim.Sync.SqlServer;
using Dotmim.Sync;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dotmim.Sync.Enumerations;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Build.Tasks;
using System.Data.SqlClient;

namespace SynchronizationWithDotmim.Sync.Service
{
    public class DotmimSyncService : IDotmimSyncService
       {
        private  string _sourceConnectionString;
        private  string _destinationConnectionString;
        private SyncAgent _syncAgent;
        private readonly string scopeName="AccopsScope";
        private  SyncSetup setup;


        //Initializing the basic setup
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

            //provisioning the remote (server) side 
            var tables = new string[] {"ProductModel",
                    "Product",
                    "Address", "Customer", "CustomerAddress",
                    "SalesOrderHeader" };

            setup = new SyncSetup(tables);
            {

            };

            // selecting which coloumns to add for synchronization.
            setup.Tables["Product"].Columns.AddRange(new[] { "ProductId", "Name", "ProductNumber", "Color", });
        }

        public async Task  ProvisionAsync()
        {
            try {

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
                var progress = new SynchronousProgress<ProgressArgs>(args => Console.WriteLine($"{args.ProgressPercentage:p}:\t{args.Message}"));
                var result =await _syncAgent.SynchronizeAsync(scopeName,progress);

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

        public async Task Recongiure()
        {
            Console.WriteLine("Inside the Reconfigure method");

            var progress = new SynchronousProgress<ProgressArgs>(args => Console.WriteLine($"{args.ProgressPercentage:p}:\t{args.Message}"));

            var result = await _syncAgent.RemoteOrchestrator.ProvisionAsync(scopeName, setup, overwrite: true, progress: progress);
            if (result != null)
            {
                Console.WriteLine("Server provisioning successful with the new column added to the Address table.");
            }
            Console.WriteLine("server is provisioned with new column added to the adderess table");

            //Provisioning the local side
            Console.WriteLine("provisioning the client side");
            var serverScopeInfo = await _syncAgent.RemoteOrchestrator.GetScopeInfoAsync(scopeName);
            var clinetScopeInfo = await _syncAgent.LocalOrchestrator.ProvisionAsync(serverScopeInfo, overwrite: true, progress: progress);
            Console.WriteLine("client side is also provisioned with new column added to the local database");
        }

        //Second Method for Reconfiguration
        public async Task Reconfigure2()
        {
                
        }

        //customizing the Scope table name
        public void CustomizeScopeInfo(string tableName)
        {
            var options = new SyncOptions();
            options.ScopeInfoTableName = tableName;
        }

        

        void IDotmimSyncService.ValidateConfigurationAsync()
        {
            throw new NotImplementedException();
        }

      
    }
}

﻿using Dotmim.Sync.SqlServer;
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
using System.Configuration;
using System.Data;
using Microsoft.IdentityModel.Protocols;
using System.Diagnostics;


namespace SynchronizationWithDotmim.Sync.Service
{
    public class DotmimSyncService : IDotmimSyncService
       {
        //syncing agent which handles all the syncing for both side (internally has both local and remote orchestrator)
        private static  SyncAgent _syncAgent;
    

        //Initializing the basic setup
        public void InitializeAsync(string SecondaryConnection, string PrimaryConnection)
        {

            var LocalProvider = new SqlSyncProvider(SecondaryConnection);
            var ServerProvider = new SqlSyncProvider(PrimaryConnection);
           
            //providing the customizable syncing options
            var syncOptions = new SyncOptions
            { 
                CleanMetadatas=false,
                ConflictResolutionPolicy=ConflictResolutionPolicy.ServerWins
               // ErrorResolutionPolicy=ErrorResolution.
            };

            _syncAgent = new SyncAgent(LocalProvider, ServerProvider,syncOptions);
           
        }

        //Provisioning Databases of both sides.
         public async Task Provision(string scopeName, string[] tables)
        {
            try
            {
                var setup = new SyncSetup(tables);
                //selecting which coloumns to add for synchronization for a particular table.
               /* setup.Tables["Orders"].Columns.AddRange(new[] { "OrderID", "OrderDate" });*/

                // Provision everything needed by the setup
                var p = SyncProvision.ScopeInfo | SyncProvision.ScopeInfoClient |
                SyncProvision.StoredProcedures | SyncProvision.TrackingTable |
                SyncProvision.Triggers;

                var progress = new SynchronousProgress<ProgressArgs>(args => Console.WriteLine($"{args.ProgressPercentage:p}:\t{args.Message}"));
                await _syncAgent.RemoteOrchestrator.ProvisionAsync(scopeName, setup, p, progress: progress);

                // Getting the server scope from server side for provisioning the local side
                var ScopeInfo = await _syncAgent.RemoteOrchestrator.GetScopeInfoAsync(scopeName);

                // Provision everything needed (sp, triggers, tracking tables)
                await _syncAgent.LocalOrchestrator.ProvisionAsync(ScopeInfo, p, progress: progress);

                Console.WriteLine("Provisioning Completed on both sides.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during provisioning: {ex.Message}");
            }
        }

        //Syncing method
        public async Task<SyncResult> SyncDatabasesAsync(string scopeName, string[] tables)
        {
            SyncResult res = null;
            try
            {
               
                var progress = new SynchronousProgress<ProgressArgs>(args => Console.WriteLine($"{args.ProgressPercentage:p}:\t{args.Message}"));
                bool sco =  await _syncAgent.RemoteOrchestrator.ExistScopeInfoTableAsync();
                if (sco) { 
                    var scopeInfo = await _syncAgent.RemoteOrchestrator.GetScopeInfoAsync(scopeName);
                    //if scope name is present or not 
                    if (scopeInfo == null)
                    {
                        Console.WriteLine("Scope is not present");
                        //applying scope 
                        await ApplyScope(scopeName, tables);
                        //await ApplyScopeWithFilter(scopeName);
                        var parameters = new SyncParameters(("ProductCategoryID", new Guid("5515F857-075B-4F9A-87B7-43B4997077B3")));
                        res = await _syncAgent.SynchronizeAsync(scopeName,progress:progress,parameters:parameters);
                        Console.WriteLine(res);
                        Console.WriteLine("Synchronization completed successfully at {0}.", DateTime.Now);
                    }
                    else
                    {
                         res = await _syncAgent.SynchronizeAsync(scopeName,progress);
                         Console.WriteLine(res);
                         Console.WriteLine("Synchronization completed successfully at {0}.", DateTime.Now);
                    }
                }
                //if scope info table is not prersent in the database
                else 
                {
                    Console.WriteLine("Scope is not present");
                    await ApplyScope(scopeName, tables);
                  //  await ApplyScopeWithFilter(scopeName);
                    Console.WriteLine("Provisioned both databases.");
                    res = await _syncAgent.SynchronizeAsync(scopeName, progress);
                    Console.WriteLine(res);
                    Console.WriteLine("Synchronization completed successfully at {0}.", DateTime.Now);
                }

                // cleaning metadatas from both side.
                if (res != null)
                {
                   /* // get all scope info clients
                    var sScopeInfoClients = await _syncAgent.RemoteOrchestrator.GetAllScopeInfoClientsAsync();


                    var oneMonthMaxScopeInfoClients = sScopeInfoClients.Where(
                        sic => sic.LastSync.HasValue && sic.LastSync.Value >= DateTime.UtcNow.AddMinutes(-5));

                    // Get the min timestamp
                    var minTimestamp = oneMonthMaxScopeInfoClients.Min(h => h.LastSyncTimestamp);

                    // Call the delete metadatas with this timestamp
                    await _syncAgent.RemoteOrchestrator.DeleteMetadatasAsync(minTimestamp.Value);*/

                    if (res.TotalChangesFailedToApplyOnServer == 0)
                    {
                        var scope = await _syncAgent.RemoteOrchestrator.GetAllScopeInfoClientsAsync();
                        var lastSyncTimestamp = scope.Min(sic => sic.LastServerSyncTimestamp);
                        if (lastSyncTimestamp.HasValue)
                        {
                            await _syncAgent.RemoteOrchestrator.DeleteMetadatasAsync(lastSyncTimestamp.Value);
                        }
                    }
                    if (res.TotalChangesFailedToApplyOnClient == 0)
                    {
                        var scope = await _syncAgent.LocalOrchestrator.GetAllScopeInfoClientsAsync();
                        var lastSyncTimestamp = scope.Min(sic => sic.LastSyncTimestamp);
                        if (lastSyncTimestamp.HasValue)
                        {
                            await _syncAgent.LocalOrchestrator.DeleteMetadatasAsync(lastSyncTimestamp.Value);
                        }
                    }
                }
                await Task.CompletedTask;
               return res;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during synchronization: {ex.Message}");
                Console.WriteLine(ex);
                return res;
            }
        }

        //Deprovisioning  both the databases.
         public async Task DeprovisionAsync(string scopeName)
         {
            try
            {  
                //Deprovision everything
                var p = SyncProvision.ScopeInfo | SyncProvision.ScopeInfoClient |
                        SyncProvision.StoredProcedures | SyncProvision.TrackingTable |
                        SyncProvision.Triggers;
                //Deprovisioning server side
                await _syncAgent.RemoteOrchestrator.DeprovisionAsync(scopeName, p);

                //deprovisioning client side
                await _syncAgent.LocalOrchestrator.DeprovisionAsync(scopeName, p);
                Console.WriteLine($"DeProvisoned sucessfully clinet");
            }catch(Exception ex)
            {
                Console.WriteLine("Error During Deprovisioning"+ex.Message);
            }
         }


        //reconfigure method (only runs from server side)
        public async Task Reconfigure(string scopeName,string connString, string[] tables)
        {
            Console.WriteLine("Inside the Reconfigure method");
            var progress = new SynchronousProgress<ProgressArgs>(args => Console.WriteLine($"{args.ProgressPercentage:p}:\t{args.Message}"));
            var setup1 = new SyncSetup(tables);

            var provider = new SqlSyncProvider(connString);
            var orchestrator = new RemoteOrchestrator(provider);

            var result = await orchestrator.ProvisionAsync(scopeName, setup1, overwrite: true, progress: progress);
            if (result != null)
            {
                Console.WriteLine("Server provisioning successful after change in dtabase.");
            }

            //client side
           var scopeInfo=await  _syncAgent.RemoteOrchestrator.GetScopeInfoAsync(scopeName);
            await _syncAgent.LocalOrchestrator.ProvisionAsync(scopeInfo, overwrite: true, progress: progress);
            Console.WriteLine("provisioned both  with new schema ");
            Console.WriteLine("Reconfiguration is done");
        }


        //Second Method for Reconfiguration
        public static  async Task Reconfigure2(string scopeName,string connectionString,string[] tabels)
        {
            try
            {
                //Reconfiguring the database schema which is deprovisioning eveything except scope_info and scope_info_cient
                var tempScope = scopeName + "_temp";

                //making connection
                var provider = new SqlSyncProvider(connectionString);
                var orchestrator = new RemoteOrchestrator(provider);
               
                //getting tables from scope
                var scopeInfo = await orchestrator.GetScopeInfoAsync(scopeName);
                var scopedTables = scopeInfo.Setup.Tables;

                //Deprovisioning everthing 
                var d = SyncProvision.StoredProcedures | SyncProvision.TrackingTable | SyncProvision.Triggers;

                var progress = new SynchronousProgress<ProgressArgs>(args => Console.WriteLine($"{args.ProgressPercentage:p}:\t{args.Message}"));
                orchestrator.DeprovisionAsync(scopeName, d, progress: progress).Wait();

                
                //now provisioning same with sp,tracking tables,triggers
                var setup1 = new SyncSetup(tabels);
                d = SyncProvision.ScopeInfo | SyncProvision.ScopeInfoClient;
                var result = await orchestrator.ProvisionAsync(tempScope, setup1, d, progress: progress);

                //Now swaping schema and setup info from new scopwe to original with  help of script
                string sqlScript =
                @"
                                                    
                DECLARE @setup NVARCHAR(MAX), @schema NVARCHAR(MAX);

                       SELECT @setup = si.[sync_scope_setup], @schema = si.[sync_scope_schema]
                       FROM [scope_info] si
                       WHERE si.sync_scope_name = @scope_name + '_temp';

                       UPDATE [scope_info]
                       SET [sync_scope_setup] = @setup, [sync_scope_schema] = @schema
                       WHERE sync_scope_name = @scope_name;

                       DELETE FROM [scope_info]  
                       WHERE sync_scope_name = @scope_name + '_temp';
                ";
                sqlScript = sqlScript.Replace("@scope_name", $"'{scopeName}'");
                try
                {
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        // Open the connection
                        connection.Open();

                        // Create a command to execute the SQL script
                        using (SqlCommand command = new SqlCommand(sqlScript, connection))
                        {
                            
                            command.ExecuteNonQuery();
                        }
                    }

                    d = SyncProvision.Triggers | SyncProvision.TrackingTable | SyncProvision.StoredProcedures;
                    orchestrator.ProvisionAsync(scopeName, d, progress: progress).Wait();
                    Console.WriteLine("Reconfiguration is done");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            
        }

        //appliying sccope(Provisioning)
        public static async Task ApplyScope(string scopeName, string[] tables)
        {
            try
            {
                var setup = new SyncSetup(tables);
               //selecting which coloumns to add for synchronization for a particular table.
                //setup.Tables["Orders"].Columns.AddRange(new[] { "OrderID", "OrderDate" });
              
               // Provision everything needed by the setup
                var p = SyncProvision.ScopeInfo | SyncProvision.ScopeInfoClient |
                SyncProvision.StoredProcedures | SyncProvision.TrackingTable |
                SyncProvision.Triggers;
                
                var progress = new SynchronousProgress<ProgressArgs>(args => Console.WriteLine($"{args.ProgressPercentage:p}:\t{args.Message}"));
                await _syncAgent.RemoteOrchestrator.ProvisionAsync(scopeName, setup, p, progress: progress);

                // Getting the server scope from server side for provisioning the local side
                var ScopeInfo = await _syncAgent.RemoteOrchestrator.GetScopeInfoAsync(scopeName);

                // Provision everything needed (sp, triggers, tracking tables)
                await _syncAgent.LocalOrchestrator.ProvisionAsync(ScopeInfo, p,progress:progress);

                Console.WriteLine("Provisioning Completed on both sides.");
                Console.WriteLine("Scope has applied to both the databases.;");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during provisioning: {ex.Message}");
            }
        }  

        public static async Task ApplyScopeWithFilter(string scopeName)
        {
            try
            {
                var setup = new SyncSetup("ProductCategory", "Product");

                // Shortcut to create a filter directly from your SyncSetup instance
                // We are filtering all the product categories, by the ID (a GUID)
                setup.Filters.Add("ProductCategory", "ProductCategoryID");

                // For the second table (Product) We can also create the filter manually.
                // The next 4 lines are equivalent to : setup.Filters.Add("Product", "ProductCategoryID");
                var productFilter = new SetupFilter("Product");

                // Add a column as parameter. This column will be automaticaly added in the tracking table
                productFilter.AddParameter("ProductCategoryID", "Product");

                // add the side where expression, mapping the parameter to the column
                productFilter.AddWhere("ProductCategoryID", "Product", "ProductCategoryID");

                // add this filter to setup
                setup.Filters.Add(productFilter);

                // Creating an agent that will handle all the process
                var parameters = new SyncParameters(("ProductCategoryID", new Guid("43521287-4B0B-438E-B80E-D82D9AD7C9F0")));
                do
                {
                    // Launch the sync process
                    var s1 = await _syncAgent.SynchronizeAsync(setup, parameters).ConfigureAwait(false);

                    // Write results
                    Console.WriteLine(s1);
                }
                while (Console.ReadKey().Key != ConsoleKey.Escape);

                Console.WriteLine("End");

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        
        //ReInitializing the SyncAgent when change mode gets called
        public  async Task ReInitialize(string sourceConnectionString, string destinationConnectionString,string scopeName)
        {
            //switching connection string as primary beacame secondary and vice versa
            InitializeAsync(sourceConnectionString, destinationConnectionString);
            /* string sqlScript =
                @"                                   
                 DECLARE @tempTimestamp BIGINT;

                 SELECT @tempTimestamp = [scope_last_sync_timestamp]
                 FROM [dbo].[scope_info_client]
                  WHERE sync_scope_name = @scope_name;

                 UPDATE [dbo].[scope_info_client]
                 SET [scope_last_sync_timestamp] = [scope_last_server_sync_timestamp],
                     [scope_last_server_sync_timestamp] = @tempTimestamp
                 WHERE sync_scope_name = @scope_name;
                 ";
             sqlScript = sqlScript.Replace("@scope_name", $"'{scopeName}'");
             try
             {
                 using (SqlConnection connection = new SqlConnection(destinationConnectionString))
                 {
                     // Open the connection
                     connection.Open();

                     // Create a command to execute the SQL script
                     using (SqlCommand command = new SqlCommand(sqlScript, connection))
                     {

                         command.ExecuteNonQuery();
                     }
                 }
                 using (SqlConnection connection = new SqlConnection(sourceConnectionString))
                 {
                     // Open the connection
                     connection.Open();

                     // Create a command to execute the SQL script
                     using (SqlCommand command = new SqlCommand(sqlScript, connection))
                     {

                         command.ExecuteNonQuery();
                     }
                 }
             }
             catch (Exception ex)
             {
                 Console.Write("Exception occured while excecuting the switching the timestamp");
                 Console.WriteLine(ex.ToString());
             }*/

            var scopeInfo = await _syncAgent.LocalOrchestrator.GetScopeInfoAsync(scopeName);
            await _syncAgent.LocalOrchestrator.DisableConstraintsAsync(scopeInfo, "Task");

            //event if after change it found that client is outdated.
            _syncAgent.LocalOrchestrator.OnOutdated(outdated =>
            {
                //setting action to reinitialize of syncing.
                outdated.Action = OutdatedAction.Reinitialize;
                Console.WriteLine(outdated.Message);
                Console.WriteLine(outdated.Action);
            });

        }

        //handling when change apply on server failed
        public void SyncApplyRemoteFailed(SyncResult result)
        {
            Console.WriteLine("Changes failed to apply on server :" + result.TotalChangesFailedToApplyOnServer);
           
        }

        //handling when change apply on client failed.
        public void SyncApplyLocalFailed(SyncResult result)
        {

            Console.WriteLine("Changes failed to apply on Client :" + result.TotalChangesFailedToApplyOnClient);
        }
    }
}

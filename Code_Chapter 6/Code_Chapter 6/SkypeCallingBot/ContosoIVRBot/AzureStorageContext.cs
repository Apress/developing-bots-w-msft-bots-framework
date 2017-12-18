using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace ContosoIVRBot
{
    public class AzureStorageContext
    {
        private readonly CloudStorageAccount storageAccount;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureStorageContext"/> class.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        public AzureStorageContext()
        {
            // Retrieve storage account from connection string.
            storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("AzureStorageConnectionString"));
        }

        /// <summary>
        /// Uploads the BLOB.
        /// </summary>
        /// <param name="uploadStream">The upload stream.</param>
        /// <param name="blobName">Name of the BLOB.</param>
        /// <returns></returns>
        public string UploadBlob(Stream uploadStream, string blobName)
        {
            
            // Create the blob client.
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            // Retrieve reference to a previously created container.
            CloudBlobContainer container = blobClient.GetContainerReference("contosouservoicecontainer");

            // create container of not exists
            container.CreateIfNotExists();

            // Retrieve reference to a blob named "myblob".
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(blobName + ".wav");

            blockBlob.Properties.ContentType = "audio/x-wav";

            // Create or overwrite the "myblob" blob with contents from a local file.
            using (var fileStream = uploadStream)
            {
                blockBlob.UploadFromStream(fileStream);
            }

            return blockBlob.StorageUri.PrimaryUri.ToString();
        }

        /// <summary>
        /// Saves the call state asynchronous.
        /// </summary>
        /// <param name="callState">State of the call.</param>
        /// <returns></returns>
        public async Task<TableResult> SaveCallStateAsync(CallState callState)
        {
            var blobUri = this.UploadBlob(callState.RecordedContent, callState.Id);

            // Create the table client.
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            // Retrieve a reference to the table.
            CloudTable table = tableClient.GetTableReference("contosouservoice");

            // Create the table if it doesn't exist.
            table.CreateIfNotExists();

            CallStateTableEntity callStateTableEntity = new CallStateTableEntity(callState.Participants.First().Identity);
            callStateTableEntity.Participant1Id = callState.Participants.First().Identity;
            callStateTableEntity.Participant2Id = callState.Participants.Last().Identity;
            callStateTableEntity.Option = callState.ChosenMenuOption;
            callStateTableEntity.VoiceUrl = blobUri;

            // Create the TableOperation object that inserts the customer entity.
            TableOperation insertOperation = TableOperation.InsertOrReplace(callStateTableEntity);

            // Execute the insert operation.
            return await table.ExecuteAsync(insertOperation);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="Microsoft.WindowsAzure.Storage.Table.TableEntity" />
    public class CallStateTableEntity : TableEntity
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CallStateTableEntity"/> class.
        /// </summary>
        public CallStateTableEntity() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="CallStateTableEntity"/> class.
        /// </summary>
        /// <param name="participantId">The participant identifier.</param>
        public CallStateTableEntity(string participantId)
        {
            this.PartitionKey = "ContosoUserVoicePartition";
            this.RowKey = participantId;
        }

        /// <summary>
        /// Gets or sets the participant1 identifier.
        /// </summary>
        /// <value>
        /// The participant1 identifier.
        /// </value>
        public string Participant1Id { get; set; }

        /// <summary>
        /// Gets or sets the participant2 identifier.
        /// </summary>
        /// <value>
        /// The participant2 identifier.
        /// </value>
        public string Participant2Id { get; set; }

        /// <summary>
        /// Gets or sets the option.
        /// </summary>
        /// <value>
        /// The option.
        /// </value>
        public string Option { get; set; }

        /// <summary>
        /// Gets or sets the voice URL.
        /// </summary>
        /// <value>
        /// The voice URL.
        /// </value>
        public string VoiceUrl { get; set; }
    }
}
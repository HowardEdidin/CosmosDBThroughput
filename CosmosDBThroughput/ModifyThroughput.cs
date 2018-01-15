#region Information

// Solution:  CosmosDBThroughput
// CosmosDBThroughput
// File:  ModifyThroughput.cs
// 
// Created: 01/13/2018 : 10:37 AM
// 
// Modified By: Howard Edidin
// Modified:  01/15/2018 : 2:27 PM

#endregion

#region Information

// Solution:  CosmosDBThroughput
// CosmosDBThroughput
// File:  ModifyThroughput.cs
// 
// Created: 01/13/2018 : 10:37 AM
// 
// Modified By: Howard Edidin
// Modified:  01/14/2018 : 1:54 PM

#endregion


namespace CosmosDBThroughput
{
	using System;
	using System.Linq;
	using System.Net;
	using System.Net.Http;
	using System.Threading.Tasks;
	using Microsoft.Azure.Documents;
	using Microsoft.Azure.Documents.Client;
	using Microsoft.Azure.WebJobs;
	using Microsoft.Azure.WebJobs.Extensions.Http;
	using Microsoft.Azure.WebJobs.Host;

	public static class ModifyThroughput
	{
		/// <summary>
		///     Modifies the Throughput (Request Units) for a Cosmos DB Collection
		/// </summary>
		/// <param name="req">{endpoint, authkey, database id}</param>
		/// <param name="collectionName">Collection Id</param>
		/// <param name="log"></param>
		/// <returns></returns>
		[FunctionName("ModifyThroughput")]
		public static async Task<HttpResponseMessage> Run(
			[HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "ModifyThroughput/name/{name}")]
			HttpRequestMessage req, string collectionName, TraceWriter log)
		{
			log.Info("C# HTTP trigger function processed a request.");


			// Get request body
			dynamic data = await req.Content.ReadAsAsync<object>();

			string endPoint = data.endpoint;
			string authKey = data.authkey;
			string requestUnits = data.requestunits;
			string databaseId = data.database;


			using (var client = new DocumentClient(
				new Uri(endPoint),
				authKey,
				new ConnectionPolicy
				{
					ConnectionMode = ConnectionMode.Direct,
					ConnectionProtocol = Protocol.Tcp
				}))


			{
				var collection = client.CreateDocumentCollectionQuery(UriFactory.CreateDatabaseUri(databaseId))
					.Where(c => c.Id == collectionName).ToArray().Single();
				var offer = client.CreateOfferQuery().Where(r => r.ResourceLink == collection.SelfLink)
					.AsEnumerable()
					.SingleOrDefault();


				var units = int.Parse(requestUnits);

				offer = new OfferV2(offer, units);

				await client.ReplaceOfferAsync(offer);
			}


			var res = new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent("Collection " + collectionName + " request units changed to " + requestUnits)
			};

			return res;
		}
	}
}
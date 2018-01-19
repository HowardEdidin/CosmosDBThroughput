#region Information

// Solution:  CosmosDBThroughput
// CosmosDBThroughput
// File:  ProcessThroughputEvent.cs
// 
// Created: 01/16/2018 : 12:53 PM
// 
// Modified By: Howard Edidin
// Modified:  01/19/2018 : 12:46 PM

#endregion

namespace CosmosDBThroughput
{
	using System;
	using System.Configuration;
	using System.Linq;
	using System.Net;
	using System.Net.Http;
	using System.Threading.Tasks;
	using Microsoft.Azure.Documents;
	using Microsoft.Azure.Documents.Client;
	using Microsoft.Azure.WebJobs;
	using Microsoft.Azure.WebJobs.Host;

	public static class ProcessThroughputEvent
	{
		private static readonly string Endpoint = ConfigurationManager.AppSettings["Endpoint"];
		private static readonly string AuthKey = ConfigurationManager.AppSettings["AuthKey"];
		private static readonly string Database = ConfigurationManager.AppSettings["databaseName"];
		private static readonly string Collection = ConfigurationManager.AppSettings["collectionName"];


		[FunctionName("ProcessThroughputEvent")]
		public static async Task<object> Run([HttpTrigger(WebHookType = "genericJson")] HttpRequestMessage req,
			TraceWriter log)

		{
			log.Info("Webhook was triggered!");

			


			var client = new DocumentClient(
				new Uri(Endpoint),
				AuthKey,
				new ConnectionPolicy
				{
					ConnectionMode = ConnectionMode.Direct,
					ConnectionProtocol = Protocol.Tcp
				});

			var collection = client.CreateDocumentCollectionQuery(UriFactory.CreateDatabaseUri(Database))
				.Where(c => c.Id == Collection).ToArray().Single();
			var offer = client.CreateOfferQuery().Where(r => r.ResourceLink == collection.SelfLink)
				.AsEnumerable()
				.SingleOrDefault();


			// ReSharper disable once PossibleNullReferenceException
			var result = ((OfferV2) offer).Content.OfferThroughput;

		

			var newThroughput = result + 100;

			offer = new OfferV2(offer, newThroughput);

			

			await client.ReplaceOfferAsync(offer);


			var res = new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent($"Collection {collection} request units changed to {newThroughput}")
			};

			return res;
		}
	}
}
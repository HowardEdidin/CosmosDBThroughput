

/*
* Copyright (c) 2018 Howard S. Edidin
*
* Permission is hereby granted, free of charge, to any person obtaining a copy
* of this software and associated documentation files (the "Software"), to deal
* in the Software without restriction, including without limitation the rights
* to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
* copies of the Software, and to permit persons to whom the Software is
* furnished to do so, subject to the following conditions:
*
* The above copyright notice and this permission notice shall be included in all
* copies or substantial portions of the Software.
*
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
* OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
* SOFTWARE.
*/

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
	using System.Configuration;
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

		private static readonly string Endpoint = ConfigurationManager.AppSettings["Endpoint"];
		private static readonly string AuthKey = ConfigurationManager.AppSettings["AuthKey"];
		private static readonly string Database = ConfigurationManager.AppSettings["databaseName"];

		/// <summary>
		///     Modifies the Throughput (Request Units) for a Cosmos DB Collection
		/// </summary>
		/// <param name="req">{endpoint, authkey, database id}</param>
		/// <param name="log"></param>
		/// <returns></returns>
		[FunctionName("ModifyThroughput")]
		public static async Task<HttpResponseMessage> Run(
			[HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]
			HttpRequestMessage req, TraceWriter log)
		{
			log.Info("C# HTTP trigger function processed a request.");


			// Get request body
			dynamic data = await req.Content.ReadAsAsync<object>();

		
			string requestUnits = data.requestunits;
			string collectionName = data.collection;


			using (var client = new DocumentClient(
				new Uri(Endpoint),
				AuthKey,
				new ConnectionPolicy
				{
					ConnectionMode = ConnectionMode.Direct,
					ConnectionProtocol = Protocol.Tcp
				}))


			{
				var collection = client.CreateDocumentCollectionQuery(UriFactory.CreateDatabaseUri(Database))
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

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
// File:  ProcessEvent.cs
// 
// Created: 01/16/2018 : 12:53 PM
// 
// Modified By: Howard Edidin
// Modified:  01/17/2018 : 1:09 PM

#endregion

namespace CosmosDBThroughput
{
	using System;
	using System.Configuration;
	using System.Linq;
	using System.Net.Http;
	using System.Threading.Tasks;
	using Microsoft.Azure.Documents;
	using Microsoft.Azure.Documents.Client;
	using Microsoft.Azure.WebJobs;
	using Microsoft.Azure.WebJobs.Host;
	using Newtonsoft.Json;

	public static class ProcessEvent
	{
		private static readonly string Endpoint = ConfigurationManager.AppSettings["Endpoint"];
		private static readonly string AuthKey = ConfigurationManager.AppSettings["AuthKey"];
		private static readonly string Database = ConfigurationManager.AppSettings["databaseName"];
		private static readonly string Collection = ConfigurationManager.AppSettings["collectionName"];


		[FunctionName("ProcessEvent")]
		public static async void Run([HttpTrigger(WebHookType = "genericJson")] HttpRequestMessage req, TraceWriter log)

		{
			log.Info("Webhook was triggered!");

			var jsonContent = await req.Content.ReadAsStringAsync();

			dynamic data = JsonConvert.DeserializeObject(jsonContent);


			if (data.count == null) return;


			var currentThroughput = GetCuurent();

			log.Info($"Current {currentThroughput}");

			var newThroughput = currentThroughput + 100;

			var result = ModifyThroughput(newThroughput);

			log.Info(result.ToString());
		}


		public static int GetCuurent()
		{
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
					.Where(c => c.Id == Collection).ToArray().Single();
				var offer = client.CreateOfferQuery().Where(r => r.ResourceLink == collection.SelfLink)
					.AsEnumerable()
					.SingleOrDefault();

				// ReSharper disable once PossibleNullReferenceException
				var result = ((OfferV2) offer).Content.OfferThroughput;


				return result;
			}
		}


		public static async Task<string> ModifyThroughput(int requestUnits)
		{
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
					.Where(c => c.Id == Collection).ToArray().Single();
				var offer = client.CreateOfferQuery().Where(r => r.ResourceLink == collection.SelfLink)
					.AsEnumerable()
					.SingleOrDefault();


				offer = new OfferV2(offer, requestUnits);

				await client.ReplaceOfferAsync(offer);
			}
			return "Collection " + Collection + " request units changed to " + requestUnits;
		}

		public static string GetEnvironmentVariable(string name)
		{
			return name + ": " +
			       Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
		}
	}
}
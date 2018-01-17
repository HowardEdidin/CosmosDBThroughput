# CosmosDBThroughput

## Azure Function App that changes the Throughput level for a Cosmos DB Collection.

### Inputs:
* Collection Name


## Azure Function App that gets the Throughput level for a Cosmos DB Collection.
### Inputs:
* Collection Name

## Azure Function App (Web hook) that receives an status code **429 Too Many Requests** Event from Cosmos DB. 
The Function App gets the current `offerThroughput` and increases it by 100 (standard). 

### Inputs:
* Collection Name



| To Do                                    |
|------------------------------------------|
| Add  `offerThroughput` for unlimited collections |








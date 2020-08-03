# Azure Cosmos DB Cassandra API - Event Sourcing with Change Feed

Change feed support in the Azure Cosmos DB API for Cassandra is available through the query predicates in the Cassandra Query Language (CQL). Using these predicate conditions, you can query the change feed API. Applications can get the changes made to a table using the primary key (also known as the partition key) as is required in CQL. You can then take further actions based on the results. Changes to the rows in the table are captured in the order of their modification time and the sort order is guaranteed per partition key.

The following example shows how to get a change feed on all the rows in a Cassandra API Keyspace table using .NET. The predicate `COSMOS_CHANGEFEED_START_TIME()` is used directly within CQL to query items in the change feed from a specified start time.

TODO.

> If this is your final lab, follow the steps in [Removing Lab Assets](07-cleaning_up.md) to remove all lab resources.

## Additional Resources

- https://docs.microsoft.com/en-us/azure/cosmos-db/cassandra-change-feed
- https://devblogs.microsoft.com/cosmosdb/announcing-change-feed-support-for-azure-cosmos-dbs-cassandra-api/
- https://github.com/Azure-Samples/azure-cosmos-db-cassandra-change-feed
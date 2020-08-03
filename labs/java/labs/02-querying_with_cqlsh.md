# Querying in Azure Cosmos DB

Azure Cosmos DB Cassandra API accounts provide support for querying items using the Cassandra Query Language (CQL). In this lab, you will explore how to use these rich query capabilities directly through the Azure Portal. No separate tools or client side code are required.

If this is your first lab and you have not already completed the setup for the lab content see the instructions for [Account Setup](00-account_setup.md) before starting this lab.

> NOTE: A PRIMARY KEY consists of a the partition key followed by the clustering columns. You can only insert values smaller than 64 kB into a clustering column.

## Query Overview

Querying tables with CQL allows Azure Cosmos DB to combine the advantages of Cosmos DB with Cassandra targeted applications.

## Running your first query

In this lab section, you will query your **foodcollection**.

### Simple queries

#CRUD

insert
update
delete
batch

### Exploring Indexing

In this lab, you will modify the indexing policy of an Azure Cosmos DB container. You will explore how you can optimize indexing policy for write or read heavy workloads as well as understand the indexing requirements for different SQL API query features.

1. In the **Azure Cosmos DB** blade, locate and select the **Data Explorer** link on the left side of the blade.
2. In the **Data Explorer** section, expand the **nutritiondatabase** keyspace node and then expand the **foodcollection** table node.
3. Within the **foodcollection** node, select the **Rows** link.
4. View the items within the container. Observe how these documents have many properties.

5. Select **Add new clause**
6. Select **foodgroup**
7. For the value, type **Snacks**
8. Select **Run Query**, you should get an error

9.  Open the `Lab03` folder in Visual Studio code
10. update the connection values
11. Run the project, it will execute the following CQL:

```sql
CREATE INDEX ON nutritiondatabase.foodcollection (foodgroup);

CREATE INDEX ON nutritiondatabase.foodcollection (foodid);
```

13. Re-run the CQL query, you should now get back results

### Exploring Paging

TODO 

```csharp
public static RowSet SelectDistinctPrimaryKeysFromTagReadings(byte[] pagingState)
{
    try
    {
        // will execute on continuing after failing in between. 
        if (pagingState != null)
        {
            PreparedStatement preparedStatement = BLL.currentSession.Prepare("SELECT DISTINCT \"Url\",\"Id\" FROM \"Readings\" ");
            BoundStatement boundStatement = preparedStatement.Bind();
            IStatement istatement = boundStatement.SetAutoPage(false).SetPageSize(1000).SetPagingState(pagingState);
            return BLL.currentSession.Execute(istatement);
        }
        else
        {
            PreparedStatement preparedStatement = BLL.currentSession.Prepare("SELECT DISTINCT \"Url\",\"Id\" FROM \"Readings\" ");
            BoundStatement boundStatement = preparedStatement.Bind();
            IStatement istatement = boundStatement.SetAutoPage(false).SetPageSize(1000);
            return BLL.currentSession.Execute(istatement);                    
        }
    }
    catch (Exception ex)
    {
        throw ex;
    }
}
```

4. Add the following lines of code to page through the results of this query using a while loop.

    ```csharp
    int pageCount = 0;
    while (queryB.HasMoreResults)
    {
        Console.Out.WriteLine($"---Page #{++pageCount:0000}---");
        foreach (var food in await queryB.ReadNextAsync())
        {
            Console.Out.WriteLine($"\t[{food.Id}]\t{food.Description,-20}\t{food.ManufacturerName,-40}");
        }
    }
    ```

### Exploring Query Costs

1. TODO

### Exploring Read Consistency

- https://docs.microsoft.com/en-us/azure/cosmos-db/consistency-levels-across-apis#cassandra-mapping
  
1. TODO
   
## More Resources

- https://docs.microsoft.com/en-us/azure/cosmos-db/consistency-levels-across-apis#cassandra-mapping

## More Resources

- https://docs.datastax.com/en/dse/6.0/cql/cql/cqlAbout.html
- https://docs.datastax.com/en/cql-oss/3.3/cql/cql_using/useMultIndexes.html
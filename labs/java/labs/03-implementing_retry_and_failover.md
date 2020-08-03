# Comos DB RetryPolicy

This sample illustrates how to handle rate limited requests. These are also known as 429 errors, and are returned when the consumed throughput exceeds the number of Request Units that have been provisioned for the service. In this code sample, we implement the Azure Cosmos DB extension for Cassandra Retry Policy.

The retry policy handles errors such as OverLoadedError (which may occur due to rate limiting), and parses the exception message to use RetryAfterMs field provided from the server as the back-off duration for retries. If RetryAfterMs is not available, it defaults to an exponential growing back-off scheme. In this case the time between retries is increased by a growing back off time (default: 1000 ms) on each retry, unless maxRetryCount is -1, in which case it backs off with a fixed duration. It is important to handle rate limiting in Azure Cosmos DB to prevent errors when provisioned throughput has been exhausted.

> If this is your first lab and you have not already completed the setup for the lab content see the instructions for [Account Setup](00-account_setup.md) before starting this lab.

## Create Retry Policies

TODO

> If this is your final lab, follow the steps in [Removing Lab Assets](11-cleaning_up.md) to remove all lab resources.

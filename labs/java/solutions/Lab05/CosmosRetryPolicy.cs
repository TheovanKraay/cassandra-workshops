using System;
using System.Threading;
using Cassandra;
using Cassandra.DataStax;

public class CosmosRetryPolicy : IRetryPolicy , IExtendedRetryPolicy
{
    public CosmosRetryPolicy(int maxRetryCount) : this(maxRetryCount, 5000, 1000)
    {
    }

    public CosmosRetryPolicy(int maxRetryCount, int fixedBackOffTimeMillis, int growingBackOffTimeMillis) {
        this.maxRetryCount = maxRetryCount;
        this.fixedBackOffTimeMillis = fixedBackOffTimeMillis;
        this.growingBackOffTimeMillis = growingBackOffTimeMillis;
    }

    public int getMaxRetryCount() {
        return maxRetryCount;
    }

    public void Close() {
    }

    
    public void Init(Cluster cluster) {
        Console.Write("Hello cluster");
    }

    public RetryDecision OnReadTimeout(
            IStatement statement,
            ConsistencyLevel consistencyLevel,
            int requiredResponses,
            int receivedResponses,
            bool dataRetrieved,
            int retryNumber) {
        return RetryManyTimesOrThrow(retryNumber);
    }

    public RetryDecision OnRequestError(IStatement statement, Configuration config, Exception driverException, int retryNumber)
    {
        RetryDecision retryDecision;

        try
        {
            if (driverException is NoHostAvailableException)
            {
                Thread.Sleep(10);

                return RetryDecision.Retry(null);
            }

            if (driverException is OperationTimedOutException)
            {
                Thread.Sleep(10);

                return RetryDecision.Retry(null);
            }

            if (driverException is OverloadedException || driverException is WriteFailureException)
            {
                if (this.maxRetryCount == -1 || retryNumber < this.maxRetryCount)
                {
                    int retryMillis = GetRetryAfterMs(driverException.ToString());
                    if (retryMillis == -1)
                    {
                        retryMillis = (this.maxRetryCount == -1)
                                ? this.fixedBackOffTimeMillis
                                : this.growingBackOffTimeMillis * retryNumber + random.Next(growingBackOffSaltMillis);
                    }

                    Thread.Sleep(retryMillis);

                    retryDecision = RetryDecision.Retry(null);
                }
                else
                {
                    retryDecision = RetryDecision.Rethrow();
                }
            }
            else
            {
                retryDecision = RetryDecision.Rethrow();
            }
        }
        catch (Exception exception)
        {
            retryDecision = RetryDecision.Rethrow();
        }

        return retryDecision;
    }

    public RetryDecision OnRequestError(
            IStatement statement,
            ConsistencyLevel consistencyLevel,
            DriverException driverException,
            int retryNumber) {

        RetryDecision retryDecision;

        try {
            Console.WriteLine(driverException.Message);

            if (driverException is OverloadedException || driverException is WriteFailureException)
            {
                if (this.maxRetryCount == -1 || retryNumber < this.maxRetryCount) {
                    int retryMillis = GetRetryAfterMs(driverException.ToString());
                    if (retryMillis == -1) {
                        retryMillis = (this.maxRetryCount == -1)
                                ? this.fixedBackOffTimeMillis
                                : this.growingBackOffTimeMillis * retryNumber + random.Next(growingBackOffSaltMillis);
                    }

                    Thread.Sleep(retryMillis);

                    retryDecision = RetryDecision.Retry(null);
                } else {
                    retryDecision = RetryDecision.Rethrow();
                }
            } else {
                retryDecision = RetryDecision.Rethrow();
            }
        } catch (Exception exception) {
            retryDecision = RetryDecision.Rethrow();
        }

        return retryDecision;
    }

    public RetryDecision OnUnavailable(
            IStatement statement,
            ConsistencyLevel consistencyLevel,
            int requiredReplica,
            int aliveReplica,
            int retryNumber) {
        return RetryManyTimesOrThrow(retryNumber);
    }

    public RetryDecision OnWriteTimeout(
            IStatement statement,
            ConsistencyLevel consistencyLevel,
            string writeType,
            int requiredAcks,
            int receivedAcks,
            int retryNumber) {
        return RetryManyTimesOrThrow(retryNumber);
    }

    private  static Random random = new Random();
    private  int growingBackOffSaltMillis = 2000;
    private  int fixedBackOffTimeMillis;
    private  int growingBackOffTimeMillis;
    private  int maxRetryCount;

    private RetryDecision RetryManyTimesOrThrow(int retryNumber) {
        return (this.maxRetryCount == -1 || retryNumber < this.maxRetryCount)
                ? RetryDecision.Retry(null)
                : RetryDecision.Rethrow();
    }

    private static int GetRetryAfterMs(String exceptionString){
        String[] tokens = exceptionString.ToString().Split(",");
        foreach (String token in tokens) {
            String[] kvp = token.Split("=");
            if (kvp.Length != 2) continue;
            if (kvp[0].Trim().Equals("RetryAfterMs")) {
                String value = kvp[1];
                return int.Parse(value);
            }
        }

        return -1;
    }
}
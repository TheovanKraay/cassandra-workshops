using System;
using System.Collections.Generic;
using System.Threading;
using Cassandra;
using Cassandra.DataStax;

public class CosmosLoadBalancingPolicy : ILoadBalancingPolicy
{
    ICollection<Host> hosts;

    public HostDistance Distance(Host host)
    {
        return HostDistance.Local;
    }

    public void Initialize(ICluster cluster)
    {
        hosts = cluster.AllHosts();
    }

    public IEnumerable<Host> NewQueryPlan(string keyspace, IStatement query)
    {
        return hosts;
    }
}
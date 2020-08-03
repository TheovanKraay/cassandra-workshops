using System;
using System.Threading;
using Cassandra;
using Cassandra.DataStax;

public class CosmosFailoverAwareRRPolicy : ILoadBalancingPolicy 
{
    private int index;
    private long lastDnsLookupTime = long.MinValue;
    private String globalContactPoint;
    private int dnsExpirationInSeconds;
    private Map.Entry<CopyOnWriteArrayList<Host>, CopyOnWriteArrayList<Host>> hosts;
    private InetAddress[] localAddresses = null;

    /**
     * Creates a new CosmosDB failover aware round robin policy given the global endpoint.
     * Optionally specify dnsExpirationInSeconds, which defaults to 60 seconds.
     * @param globalContactPoint is the contact point of the account (e.g, *.cassandra.cosmos.azure.com)
     */
    public CosmosFailoverAwareRRPolicy(String globalContactPoint)
    {
        this(globalContactPoint, 60);
    }

    /**
     * Creates a new CosmosDB failover aware round robin policy given the global endpoint.
     * @param globalContactPoint is the contact point of the account (e.g, *.cassandra.cosmos.azure.com)
     * @param dnsExpirationInSeconds specifies the dns refresh interval, which is 60 seconds by default.
     */
    public CosmosFailoverAwareRRPolicy(String globalContactPoint, int dnsExpirationInSeconds)
    {
        this.globalContactPoint = globalContactPoint;
        this.dnsExpirationInSeconds = dnsExpirationInSeconds;
    }

    public void init(Cluster cluster, Collection<Host> hosts)
    {
        CopyOnWriteArrayList<Host> localDcAddresses = new CopyOnWriteArrayList<Host>();
        CopyOnWriteArrayList<Host> remoteDcAddresses = new CopyOnWriteArrayList<Host>();
        
        List<InetAddress> localAddressesFromLookup = Arrays.asList(getLocalAddresses());
        
        foreach (Host host in hosts)
        {
            if (localAddressesFromLookup.contains(host.getAddress()))
            {
                localDcAddresses.add(host);
            }
            else
            {
                remoteDcAddresses.add(host);
            }
        }

        this.hosts = new AbstractMap.SimpleEntry<CopyOnWriteArrayList<Host>, CopyOnWriteArrayList<Host>>(localDcAddresses, remoteDcAddresses);
        this.index = new Random().Next(Math.Max(hosts.size(), 1));
    }

    /**
     * Return the HostDistance for the provided host.
     *
     * <p>This policy consider the nodes in the default write region as {@code LOCAL}.
     *
     * @param host the host of which to return the distance of.
     * @return the HostDistance to {@code host}.
     */
    
    public HostDistance distance(Host host)
    {
        if (Arrays.asList(getLocalAddresses()).contains(host.getAddress()))
        {
            return HostDistance.LOCAL;
        }

        return HostDistance.REMOTE;
    }

    /**
     * Returns the hosts to use for a new query.
     *
     * <p>The returned plan will always try each known host in the default write region first, and then,
     * if none of the host is reachable, it will try all other regions.
     * The order of the local node in the returned query plan will follow a
     * Round-robin algorithm.
     *
     * @param loggedKeyspace the keyspace currently logged in on for this query.
     * @param statement the query for which to build the plan.
     * @return a new query plan, i.e. an iterator indicating which host to try first for querying,
     *     which one to use as failover, etc...
     */
    public Iterator<Host> newQueryPlan(String loggedKeyspace, IStatement statement)
    {
        Map.Entry<CopyOnWriteArrayList<Host>, CopyOnWriteArrayList<Host>> allHosts = getHosts();
        List<Host> localHosts = cloneList(allHosts.getKey());
        List<Host> remoteHosts = cloneList(allHosts.getValue());
        int startIdx = index.getAndIncrement();

        // Overflow protection; not theoretically thread safe but should be good enough
        if (startIdx > int.MaxValue - 10000)
        {
            index.set(0);
        }

        return new AbstractIterator<Host>()
        {
            private int idx = startIdx;
    private int remainingLocal = localHosts.size();
    private int remainingRemote = remoteHosts.size();

    protected Host computeNext()
    {
        while (true)
        {
            if (remainingLocal > 0)
            {
                remainingLocal--;
                return localHosts.get(idx++ % localHosts.size());
            }

            if (remainingRemote > 0)
            {
                remainingRemote--;
                return remoteHosts.get(idx++ % remoteHosts.size());
            }

            return endOfData();
        }
    }
};
    }

    public void onUp(Host host)
{
    if (Arrays.asList(this.getLocalAddresses()).contains(host.getAddress()))
    {
        hosts.getKey().addIfAbsent(host);
        return;
    }

    hosts.getValue().addIfAbsent(host);
}

    public void onDown(Host host)
{
    if (Arrays.asList(this.getLocalAddresses()).contains(host.getAddress()))
    {
        hosts.getKey().remove(host);
        return;
    }

    hosts.getValue().remove(host);
}

    public void onAdd(Host host)
{
    onUp(host);
}

    public void onRemove(Host host)
{
    onDown(host);
}

public void close()
{
    // nothing to do
}

    private static CopyOnWriteArrayList<Host> cloneList(CopyOnWriteArrayList<Host> list)
{
    return (CopyOnWriteArrayList<Host>)list.clone();
}

private InetAddress[] getLocalAddresses()
{
    if (this.localAddresses == null || dnsExpired())
    {
        try
        {
            this.localAddresses = InetAddress.getAllByName(globalContactPoint);
            this.lastDnsLookupTime = System.currentTimeMillis() / 1000;
        }
        catch (UnknownHostException ex)
        {
            // dns entry may be temporarily unavailable
            if (this.localAddresses == null)
            {
                throw new IllegalArgumentException("The dns could not resolve the globalContactPoint the first time.");
            }
        }
    }

    return this.localAddresses;
}

private Map.Entry<CopyOnWriteArrayList<Host>, CopyOnWriteArrayList<Host>> getHosts()
{
    if (hosts != null && !dnsExpired())
    {
        return hosts;
    }

    CopyOnWriteArrayList<Host> oldLocalDcHosts = this.hosts.getKey();
    CopyOnWriteArrayList<Host> newLocalDcHosts = this.hosts.getValue();

    List<InetAddress> localAddresses = Arrays.asList(getLocalAddresses());
    CopyOnWriteArrayList<Host> localDcHosts = new CopyOnWriteArrayList<Host>();
    CopyOnWriteArrayList<Host> remoteDcHosts = new CopyOnWriteArrayList<Host>();

    for (Host host: oldLocalDcHosts)
    {
        if (localAddresses.contains(host.getAddress()))
        {
            localDcHosts.addIfAbsent(host);
        }
        else
        {
            remoteDcHosts.addIfAbsent(host);
        }
    }

    foreach (Host host in newLocalDcHosts)
    {
        if (localAddresses.contains(host.getAddress()))
        {
            localDcHosts.addIfAbsent(host);
        }
        else
        {
            remoteDcHosts.addIfAbsent(host);
        }
    }

    return hosts = new AbstractMap.SimpleEntry<CopyOnWriteArrayList<Host>, CopyOnWriteArrayList<Host>>(localDcHosts, remoteDcHosts);
}

private bool dnsExpired()
{
    return System.currentTimeMillis() / 1000 > lastDnsLookupTime + dnsExpirationInSeconds;
}
}
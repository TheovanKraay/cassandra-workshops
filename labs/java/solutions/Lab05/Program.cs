using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Threading.Tasks;
using Cassandra;
using Cassandra.Mapping;

public class Program
{
    private static readonly string _userName = "cosmos-cjg";
    private static readonly string _password = "SlIzVnoj0MPm26MIeSiCFD2Oezqo5U4Xnlj6C85LKoibJBVEGdRgh6pTtE6ozLGs9ozR78DyrePZo59f1TDjyA==";
    private static readonly string _contactPoint = "cosmos-cjg.cassandra.cosmos.azure.com";
    private static int CassandraPort = 10350;

    public static async Task Main(string[] args)
    {
        var options = new Cassandra.SSLOptions(SslProtocols.Tls12, true, ValidateServerCertificate);
        options.SetHostNameResolver((ipAddress) => _contactPoint);

        CosmosRetryPolicy retryPolicy = new CosmosRetryPolicy(3);

        CosmosLoadBalancingPolicy lbPolicy = new CosmosLoadBalancingPolicy();

        Cluster cluster = Cluster.Builder()
            .WithCredentials(_userName, _password)
            .WithPort(CassandraPort)
            .AddContactPoint(_contactPoint)
            .WithSSL(options)
            .WithRetryPolicy(retryPolicy)
            .WithLoadBalancingPolicy(lbPolicy)
            .Build();

        //Cluster cluster = Cluster.Builder().WithCredentials(_userName, _password).WithPort(CassandraPort).AddContactPoint(_contactPoint).WithSSL(options).Build();

        using (ISession session = cluster.Connect())
        {
            session.CreateKeyspaceIfNotExists("nutritiondatabase");
            session.Execute("create table IF NOT EXISTS nutritiondatabase.foodcollection (id text, description text, manufacturername text, foodgroup text, primary key (foodgroup, id))");
            session.ChangeKeyspace("nutritiondatabase");

            List<Food> foods = new Bogus.Faker<Food>()
            .RuleFor(p => p.Id, f => (-1 - f.IndexGlobal).ToString())
            .RuleFor(p => p.Description, f => f.Commerce.ProductName())
            .RuleFor(p => p.ManufacturerName, f => f.Company.CompanyName())
            .RuleFor(p => p.FoodGroup, f => "Energy Bars")
            .Generate(5000);

            List<Task> tasks = new List<Task>();

            foreach(Food f in foods)
            {
                Map<Food> m = new Map<Food>();
                m.TableName("foodcollection");
                MappingConfiguration config = new MappingConfiguration();
                config.Define(m);
                IMapper mapper = new Mapper(session, config);                

                Task t = mapper.InsertAsync<Food>(f);
                tasks.Add(t);
            }

            Task.WaitAll(tasks.ToArray());

            try
            {
                foreach (var task in tasks)
                {
                    await Console.Out.WriteLineAsync($"Item Created\t{task.Id}\r\n");
                }
            }
            catch (Exception ex)
            {
                Console.Write(ex.Message);
            }
        }
    }

    public static bool ValidateServerCertificate(
        object sender,
        X509Certificate certificate,
        X509Chain chain,
        SslPolicyErrors sslPolicyErrors)
    {
        if (sslPolicyErrors == SslPolicyErrors.None)
            return true;

            Console.WriteLine("Certificate error: {0}", sslPolicyErrors);
        // Do not allow this client to communicate with unauthenticated servers.
        return false;
    }
}

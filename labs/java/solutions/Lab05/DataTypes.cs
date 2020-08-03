using Newtonsoft.Json;
using Cassandra;
using Cassandra.Data.Linq;
using Cassandra.Mapping;
using Cassandra.Mapping.Attributes;

public class DeleteStatus
{
    public int Deleted { get; set; }
    public bool Continuation { get; set; }
}

[Cassandra.Mapping.Attributes.Table("foodcollection")]
public class Food
{
    [Cassandra.Mapping.Attributes.Column("id")]
    public string Id { get; set; }

    [Cassandra.Mapping.Attributes.Column("description")]
    public string Description { get; set; }

    [Cassandra.Mapping.Attributes.Column("manufacturerName")]
    public string ManufacturerName { get; set; }

    [Cassandra.Mapping.Attributes.Column("foodGroup")]
    public string FoodGroup { get; set; }
}

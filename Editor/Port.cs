
namespace TerrainGraph.Authoring
{
[System.Serializable]
public class Port
{
    public string   Name;
    public PortType Type;

    // Distributed connections (stored in the Input port)
    public string   SourceNodeGUID;
    public int      SourcePortIndex = -1;

    [System.NonSerialized]
    public GraphNode ConnectedNode; // Optimization: Resolved at bake-time for O(1) traversal


    public Port(string name, PortType type)
    {
        Name = name;
        Type = type;
    }
}

}

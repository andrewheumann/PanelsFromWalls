using Elements;
using System;
using System.Collections.Generic;
using Elements.Geometry;
namespace Elements.Spatial
{


    public class Network : List<Node>
    {
        public List<Edge> Edges { get; }
        public Network() : base()
        {
            Edges = new List<Edge>();
        }

        public Network(IEnumerable<Line> edges) : this()
        {
            foreach (var edge in edges)
            {
                AddEdge(edge);
            }
        }

        public void AddEdge(Line line)
        {
            var fromNode = AddNode(new Node(line.Start));
            var toNode = AddNode(new Node(line.End));
            AddEdge(new Edge(fromNode, toNode));
        }
        public void AddEdge(Edge edge)
        {
            this[edge.From].Edges.Add(edge);
            this[edge.To].Edges.Add(edge);
            Edges.Add(edge);
        }
        public int AddNode(Node node)
        {
            for (int i = 0; i < Count; i++)
            {
                Node otherNode = this[i];
                if (otherNode.Location.IsAlmostEqualTo(node.Location))
                {
                    return i;
                }
            }
            this.Add(node);
            return this.Count - 1;
        }

        public Line GetEdgeLine(Edge edge)
        {
            return new Line(
                this[edge.From].Location,
                this[edge.To].Location
            );
        }
    }

    public class Node
    {
        public Node()
        {
            Edges = new List<Edge>();
        }

        public Node(Vector3 location) : this()
        {
            Location = location;
        }
        public Vector3 Location { get; }
        public List<Edge> Edges { get; }

        public int Valence => Edges.Count;
    }

    public class Edge
    {
        public Edge(int fromNode, int toNode)
        {
            From = fromNode;
            To = toNode;
        }

        public int From { get; }
        public int To { get; }
    }

}
using System;
using Elements;
using Elements.Geometry;
using System.Collections.Generic;
using System.Linq;
using Elements.Spatial;

namespace PanelsFromWalls
{
    public static class PanelsFromWalls
    {
        /// <summary>
        /// The PanelsFromWalls function.
        /// </summary>
        /// <param name="model">The input model.</param>
        /// <param name="input">The arguments to the execution.</param>
        /// <returns>A PanelsFromWallsOutputs instance containing computed results and the model with any new elements.</returns>
        public static PanelsFromWallsOutputs Execute(Dictionary<string, Model> inputModels, PanelsFromWallsInputs input)
        {
            var allWalls = new List<Wall>();
            inputModels.TryGetValue("Walls", out Model wallModel);
            var getWalls = wallModel.AllElementsOfType<Wall>();
            if (wallModel == null || !getWalls.Any())
            {
                throw new ArgumentException("No Walls found.");
            }
            allWalls.AddRange(getWalls);
            var wallCenterlines = allWalls.Select(TryGetCenterlineFromWall).Where(s => s != null);
            var endPoints = wallCenterlines.SelectMany(l => new[] { l.Start, l.End });
            var network = new Network(wallCenterlines);
            Dictionary<Edge, Grid1d> edgeGrids = new Dictionary<Edge, Grid1d>();
            foreach (var edge in network.Edges)
            {
                var edgeLine = network.GetEdgeLine(edge);
                var grid = new Grid1d(edgeLine);
                edgeGrids.Add(edge, grid);

                var cornerAtStart = network[edge.From].Valence > 1;
                var cornerAtEnd = network[edge.To].Valence > 1;

                var cornerCount = (cornerAtStart ? 1 : 0) + (cornerAtEnd ? 1 : 0);

                var cornerLength = input.CornerLength;
                if (cornerLength * cornerCount > edgeLine.Length())
                {
                    cornerLength = edgeLine.Length() / cornerCount;
                }
                if (cornerAtStart)
                {
                    grid.SplitAtOffset(cornerLength);
                }

                if (cornerAtEnd)
                {
                    grid.SplitAtOffset(cornerLength, true);
                }
                Grid1d gridToSubdivide = null;
                switch (grid.Cells.Count)
                {
                    case 3:
                        gridToSubdivide = grid[1];
                        break;
                    case 2:
                        if (cornerCount == 1)
                        {
                            if (cornerAtStart) gridToSubdivide = grid[1];
                            if (cornerAtEnd) gridToSubdivide = grid[0];
                        }
                        break;
                    default:
                        gridToSubdivide = grid;
                        break;
                }
                if (gridToSubdivide != null)
                {
                    gridToSubdivide.DivideByFixedLength(input.PanelLength, FixedDivisionMode.RemainderAtEnd);
                }

            }

            var lines = edgeGrids.SelectMany(g => g.Value.GetCells()).Select(c => c.GetCellGeometry()).OfType<Line>();

            // Create walls from lines, and assign a random color material
            List<StandardWall> walls = new List<StandardWall>();
            var rand = new Random();
            Dictionary<int, Color> colorMap = new Dictionary<int, Color>();
            colorMap.Add(KeyFromLength(input.CornerLength), Colors.Red);
            if (input.CornerLength != input.PanelLength) colorMap.Add(KeyFromLength(input.PanelLength), Colors.Blue);
            foreach (var wallLine in lines)
            {
                var color = default(Color);
                var lengthKey = KeyFromLength(wallLine.Length());
                if (colorMap.ContainsKey(lengthKey))
                {
                    color = colorMap[lengthKey];
                }
                else
                {
                    color = new Color(rand.NextDouble(), rand.NextDouble(), rand.NextDouble(), 1.0);
                    colorMap.Add(lengthKey, color);
                }
                var mat = input.ColorCodeByLength ? new Material(color, 0, 0, Guid.NewGuid(), color.ToString()) : BuiltInMaterials.Concrete;
                walls.Add(new StandardWall(wallLine, 0.1, 3.0, mat));
            }

            var nonStandardCount = lines.Where(l => KeyFromLength(l.Length()) != KeyFromLength(input.PanelLength) && KeyFromLength(l.Length()) != KeyFromLength(input.CornerLength)).Count();

            var output = new PanelsFromWallsOutputs(walls.Count, nonStandardCount,colorMap.Count());
            output.model.AddElements(walls);
            return output;
        }

        private static int KeyFromLength(double length)
        {
            return (int)(Math.Round(length, 2) * 100);
        }

        public static Line TryGetCenterlineFromWall(Wall w)
        {
            if (w is StandardWall sw)
            {
                return sw.CenterLine;
            }
            var wallProfile = w.Profile;
            var wallXform = w.Transform;
            var dot = w.Transform.ZAxis.Dot(Vector3.ZAxis);
            if (dot > 0.99)
            { // profile is XY-Parallel
                var boundary = wallProfile.Perimeter;
                if (boundary.Segments().Length == 4)
                {
                    var segmentsOrdered = boundary.Segments().OrderByDescending(s => s.Length()).ToArray();
                    var longest = segmentsOrdered[0];
                    var secondLongest = segmentsOrdered[1];
                    //assume these are antiparallel
                    var A = (longest.Start + secondLongest.End) / 2.0;
                    var B = (longest.End + secondLongest.Start) / 2.0;
                    return new Line(A, B);
                }
                else
                {
                    // throw new NotSupportedException("Walls with complex profiles are not supported.");
                    return null;
                }
            }
            else if (Math.Abs(dot) < 0.1)
            { // profile is not XY-Parallel
                var boundary = wallXform.OfPolygon(wallProfile.Perimeter);
                //This is a bad way to do this! but hopefully this mostly happens with
                //StandardWalls so we're unlikely to run into this case. 
                return boundary.Segments().OrderBy(s => s.PointAt(0.5).Z).First();
            }
            return null;
        }
    }
}
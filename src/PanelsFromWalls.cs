using System;
using Elements;
using Elements.Geometry;
using System.Collections.Generic;
using System.Linq;
using Elements.Spatial;
using Newtonsoft.Json;
using Elements.Geometry.Solids;

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
            var outputModel = new Model();
            var allWalls = new List<Wall>();
            var allWallsByProfile = new List<WallByProfile>();
            var hasWallModel = inputModels.TryGetValue("Walls", out Model wallModel);
            var hasFacadeModel = inputModels.TryGetValue("Facade", out Model facadeModel);
            if (System.IO.File.Exists("/Users/andrewheumann/Hypar Dropbox/Andrew Heumann/Hypar/Sample Data/DemoExportFromRevitWithDynamo/WallsAndFloors-Residential.json"))
            {
                wallModel = Model.FromJson(System.IO.File.ReadAllText("/Users/andrewheumann/Hypar Dropbox/Andrew Heumann/Hypar/Sample Data/DemoExportFromRevitWithDynamo/WallsAndFloors-Residential.json"));
            }
            if (wallModel == null && facadeModel == null)
            {
                throw new ArgumentException("This function requires either Walls or a Facade that contains walls. Neither was found in your workflow.");
            }
            var nonNullModel = wallModel ?? facadeModel;
            if (!nonNullModel.AllElementsOfType<Wall>().Any() && !nonNullModel.AllElementsOfType<WallByProfile>().Any())
            {
                throw new ArgumentException("No Wall or WallByProfile elements were found in the Walls or Facade dependency.");
            }
            if (wallModel != null)
            {
                allWalls.AddRange(wallModel.AllElementsOfType<Wall>());
                allWallsByProfile.AddRange(wallModel.AllElementsOfType<WallByProfile>());
            }
            if (facadeModel != null)
            {
                allWalls.AddRange(facadeModel.AllElementsOfType<Wall>());
                allWallsByProfile.AddRange(facadeModel.AllElementsOfType<WallByProfile>());
            }
            List<Wall> wallsWithoutOpenings = new List<Wall>();
            foreach (var wall in allWalls)
            {
                // convert to wall by profile to handle openings
                if (wall.Openings != null && wall.Openings.Count > 0)
                {
                    var cl = TryGetCenterlineFromWall(wall, out var thickness);
                    if (cl == null)
                    {
                        wallsWithoutOpenings.Add(wall);
                        continue;
                    }
                    var wallRect = new Polygon(new[] {
                        cl.Start,
                        cl.End,
                        cl.End + new Vector3(0,0,wall.Height),
                        cl.Start + new Vector3(0,0,wall.Height)
                    });
                    var toWall = new Transform(cl.Start, cl.Direction(), Vector3.ZAxis, cl.Direction().Cross(Vector3.ZAxis).Negate());
                    var voids = wall.Openings.Select(o => o.Perimeter.TransformedPolygon(o.Transform)).ToList();
                    var profile = new Profile(wallRect.TransformedPolygon(wall.Transform), voids, Guid.NewGuid(), null);
                    // outputModel.AddElements(voids.Select(v => new ModelCurve(v)));
                    // outputModel.AddElement(new ModelCurve(wallRect.TransformedPolygon(wall.Transform)));
                    var wbp = new WallByProfile(profile, thickness, cl, new Transform());
                    allWallsByProfile.Add(wbp);
                }
                else
                {
                    wallsWithoutOpenings.Add(wall);
                }
            }

            var panels = ProcessWallsAndStandardWalls(input, wallsWithoutOpenings, out int totalCount, out int uniqueCount, out int nonStandardCount);
            var panels2d = ProcessWallsByProfile(input, allWallsByProfile, out int totalCountP, out int uniqueCountP, out int nonStandardCountP);
            var output = new PanelsFromWallsOutputs(totalCount + totalCountP, nonStandardCount + nonStandardCountP, uniqueCount + uniqueCountP);
            outputModel.AddElements(panels);
            outputModel.AddElements(panels2d);
            output.Model = outputModel;
            return output;
        }

        private static List<WallPanel> ProcessWallsByProfile(PanelsFromWallsInputs input, List<WallByProfile> allWallsByProfile, out int totalCount, out int uniqueCount, out int nonStandardCount)
        {
            var panelsOut = new List<WallPanel>();
            Dictionary<string, Color> colorMap = new Dictionary<string, Color>();
            uniqueCount = 0;
            nonStandardCount = 0;
            var rand = new Random(5);
            int uniqueIDCounter = 0;

            foreach (var wall in allWallsByProfile)
            {
                var centerline = wall.Centerline;
                var profile = wall.Profile;
                var clVec = centerline.Direction();
                var wallNormal = clVec.Cross(Vector3.ZAxis);
                var toWall = new Transform(centerline.Start, clVec, wallNormal);
                var fromWall = new Transform(toWall);

                fromWall.Invert();
                toWall.Concatenate(wall.Transform);


                var flatProfile = fromWall.OfProfile(profile);
                var polygons = new[] { flatProfile.Perimeter }.Union(flatProfile.Voids).Select(p => Make2d(p)).ToList();
                var grid = new Grid2d(polygons);
                grid.U.DivideByFixedLength(input.PanelLength, FixedDivisionMode.RemainderAtEnd);
                foreach (var cell in grid.GetCells())
                {
                    var cellGeometries = cell.GetTrimmedCellGeometry().OfType<Polygon>();
                    var isTrimmed = cell.IsTrimmed();
                    if (!isTrimmed)
                    {
                        var outerLoops = cellGeometries.Where(c => !c.IsClockWise());
                        var innerLoops = cellGeometries.Where(c => c.IsClockWise());
                        foreach (var cellGeometry in outerLoops)
                        {
                            var containedLoops = innerLoops.Where(i => cellGeometry.Covers(i)).Select(Make2d).ToList();
                            
                            var polygon = Make2d(cellGeometry);
                            if (polygon == null) continue;
                            var cellProfile = new Profile(polygon, containedLoops, Guid.NewGuid(), null);

                            var thicknessTransform = new Transform(0, 0, -wall.Thickness / 2.0);
                            cellProfile = thicknessTransform.OfProfile(cellProfile);
                            var extrude = new Extrude(cellProfile, wall.Thickness, Vector3.ZAxis, false);
                            var identifier = $"{Math.Round(cell.U.Domain.Length, 2)} x {Math.Round(cell.V.Domain.Length, 2)}";
                            Color color = default(Color);
                            if (colorMap.ContainsKey(identifier))
                            {
                                color = colorMap[identifier];
                            }
                            else
                            {
                                color = new Color(rand.NextDouble(), rand.NextDouble(), rand.NextDouble(), 1.0);
                                colorMap.Add(identifier, color);
                            }
                            var material = input.ColorCodeByLength ? new Material(color, 0, 0, false, null, true, Guid.NewGuid(), color.ToString()) : BuiltInMaterials.Concrete;
                            var geomRep = new Representation(new[] { extrude });
                            var panel = new WallPanel(identifier, cellProfile, true, wall.Thickness, toWall, material, geomRep, false, Guid.NewGuid(), "");
                            panelsOut.Add(panel);
                        }
                    }
                    else
                    {
                        foreach (var polygon in cellGeometries)
                        {
                            var cellProfile = new Profile(polygon);
                            var thicknessTransform = new Transform(0, 0, -wall.Thickness / 2.0);
                            cellProfile = thicknessTransform.OfProfile(cellProfile);
                            var extrude = new Extrude(cellProfile, wall.Thickness, Vector3.ZAxis, false);
                            var identifier = $"C-{uniqueIDCounter++:00}";
                            var color = new Color(rand.NextDouble(), rand.NextDouble(), rand.NextDouble(), 1.0);
                            colorMap.Add(identifier, color);
                            var material = input.ColorCodeByLength ? new Material(color, 0, 0, false, null, true, Guid.NewGuid(), color.ToString()) : BuiltInMaterials.Concrete;
                            var geomRep = new Representation(new[] { extrude });
                            var panel = new WallPanel(identifier, cellProfile, true, wall.Thickness, toWall, material, geomRep, false, Guid.NewGuid(), "");
                            panelsOut.Add(panel);
                        }
                    }

                }

            }

            totalCount = panelsOut.Count;

            return panelsOut;

        }

        private static Polygon Make2d(Polygon polygon)
        {
            return new Polygon(polygon.Vertices.Select(v => new Vector3(v.X, v.Y)).Distinct().ToList());
        }

        private static List<WallPanel> ProcessWallsAndStandardWalls(PanelsFromWallsInputs input, List<Wall> allWalls, out int totalCount, out int uniqueCount, out int nonStandardCount)
        {
            var wallCenterlines = allWalls.Select(TryGetCenterlineFromWall).Where(s => s != null);
            var endPoints = wallCenterlines.SelectMany(l => new[] { l.Start, l.End });
            var network = new Network(wallCenterlines);
            Dictionary<Elements.Spatial.Edge, Grid1d> edgeGrids = new Dictionary<Elements.Spatial.Edge, Grid1d>();
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
                if (!grid.IsSingleCell)
                {
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

            }
            List<Line> lines = new List<Line>();
            foreach (var edgeGrid in edgeGrids)
            {
                if (edgeGrid.Value == null) continue;
                var cells = edgeGrid.Value.IsSingleCell ? new List<Grid1d> { edgeGrid.Value } : edgeGrid.Value.GetCells();
                var cellGeometry = cells.Select(c => c.GetCellGeometry()).OfType<Line>();
                lines.AddRange(cellGeometry);
            }

            // Create walls from lines, and assign a random color material
            var walls = new List<WallPanel>();
            var rand = new Random(5);
            var colorMap = new Dictionary<int, Color>();
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
                var mat = input.ColorCodeByLength ? new Material(color, 0, 0, false, null, true, Guid.NewGuid(), color.ToString()) : BuiltInMaterials.Concrete;
                walls.Add(CreateSimpleWallPanel(wallLine, 0.1, 3.0, mat));
            }

            nonStandardCount = lines.Where(l => KeyFromLength(l.Length()) != KeyFromLength(input.PanelLength) && KeyFromLength(l.Length()) != KeyFromLength(input.CornerLength)).Count();
            totalCount = walls.Count;
            uniqueCount = Math.Min(totalCount, colorMap.Count());
            return walls;
        }

        private static WallPanel CreateSimpleWallPanel(Line wallLine, double thickness, double height, Material mat)
        {
            var wallProfile = new Profile(Polygon.Rectangle(Vector3.Origin, new Vector3(wallLine.Length(), height)));
            var d = wallLine.Direction();
            var z = d.Cross(Vector3.ZAxis);
            var wallTransform = new Transform(wallLine.Start, d, z);
            var extrude = new Extrude(wallProfile, thickness, Vector3.ZAxis, false);
            var geomRep = new Representation(new[] { extrude });

            var identifier = $"{Math.Round(wallLine.Length(), 2)} x {Math.Round(height, 2)}";
            var wallpanel = new WallPanel(identifier, wallProfile, true, thickness, wallTransform, mat, geomRep, false, Guid.NewGuid(), "");
            return wallpanel;
        }

        private static int KeyFromLength(double length)
        {
            return (int)(Math.Round(length, 2) * 100);
        }
        public static Line TryGetCenterlineFromWall(Wall w)
        {
            return TryGetCenterlineFromWall(w, out _);
        }
        public static Line TryGetCenterlineFromWall(Wall w, out double thickness)
        {
            if (w is StandardWall sw)
            {
                thickness = sw.Thickness;
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
                    thickness = longest.PointAt(0.5).DistanceTo(secondLongest);
                    return new Line(A, B);
                }
                else
                {
                    thickness = Double.NaN;
                    // throw new NotSupportedException("Walls with complex profiles are not supported.");
                    return null;
                }
            }
            else if (Math.Abs(dot) < 0.1)
            { // profile is not XY-Parallel
                var boundary = wallXform.OfPolygon(wallProfile.Perimeter);
                //This is a bad way to do this! but hopefully this mostly happens with
                //StandardWalls so we're unlikely to run into this case. 
                thickness = 0; // can't determine thickness from profile 
                return boundary.Segments().OrderBy(s => s.PointAt(0.5).Z).First();
            }
            thickness = Double.NaN;
            return null;
        }
    }
}
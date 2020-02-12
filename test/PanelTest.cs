
using Xunit;
using Hypar.Functions.Execution;
using System.Threading.Tasks;
using Elements;
using Elements.Geometry;
using Xunit.Abstractions;
using Hypar.Functions.Execution.Local;
using System.IO;
using System.Collections.Generic;

namespace PanelsFromWalls.Tests
{
    public class PanelTest
    {
        [Fact]
        public void LoadWalls()
        {
            var model = Model.FromJson(File.ReadAllText("../../../../Walls.json"));
            var inputs = new PanelsFromWallsInputs(6, 6, true, "", "", new Dictionary<string, string>(), "", "", "");
            var output = PanelsFromWalls.Execute(new Dictionary<string, Model> { { "Walls", model } }, inputs);

            var outModel = output.model.ToJson();
            File.WriteAllText("/Users/andrewheumann/Desktop/color-coded-walls.json", outModel);

        }

    }

}

using Xunit;
using Hypar.Functions.Execution;
using System.Threading.Tasks;
using Elements;
using Elements.Geometry;
using Xunit.Abstractions;
using Hypar.Functions.Execution.Local;
using System.IO;
using System.Collections.Generic;
using Elements.Serialization.glTF;

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

            var outModel = output.Model.ToJson();
            File.WriteAllText("/Users/andrewheumann/Desktop/color-coded-walls.json", outModel);

        }

        [Fact]
        public void LoadWallsByProfile()
        {
            var json = File.ReadAllText("../../../../WallsByProfile.json");
            var model = Model.FromJson(json);
            model.ToGlTF("/Users/andrewheumann/Desktop/inputModel.glb");
            var inputs = new PanelsFromWallsInputs(3, 2, true, "", "", new Dictionary<string, string>(), "", "", "");
            var output = PanelsFromWalls.Execute(new Dictionary<string, Model> { { "Walls", model } }, inputs);

            var outModel = output.Model.ToJson();
            File.WriteAllText("/Users/andrewheumann/Desktop/color-coded-walls.json", outModel);
            output.Model.ToGlTF("/Users/andrewheumann/Desktop/color-coded-walls.glb");
        }

    }

}
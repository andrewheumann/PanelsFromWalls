// This code was generated by Hypar.
// Edits to this code will be overwritten the next time you run 'hypar init'.
// DO NOT EDIT THIS FILE.

using Elements;
using Elements.GeoJSON;
using Elements.Geometry;
using Hypar.Functions;
using Hypar.Functions.Execution;
using Hypar.Functions.Execution.AWS;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;

namespace PanelsFromWalls
{
    public class PanelsFromWallsInputs: S3Args
    {
		/// <summary>
		/// The panel length.
		/// </summary>
		[JsonProperty("Panel Length")]
		public double PanelLength {get;}

		/// <summary>
		/// The length of any leg of a custom section where two or more walls meet.
		/// </summary>
		[JsonProperty("Corner Length")]
		public double CornerLength {get;}

		/// <summary>
		/// Set to true to visualize wall sections colored according to their length.
		/// </summary>
		[JsonProperty("Color-code by Length")]
		public bool ColorCodeByLength {get;}


        
        /// <summary>
        /// Construct a PanelsFromWallsInputs with default inputs.
        /// This should be used for testing only.
        /// </summary>
        public PanelsFromWallsInputs() : base()
        {
			this.PanelLength = 10;
			this.CornerLength = 10;
			this.ColorCodeByLength = false;

        }


        /// <summary>
        /// Construct a PanelsFromWallsInputs specifying all inputs.
        /// </summary>
        /// <returns></returns>
        [JsonConstructor]
        public PanelsFromWallsInputs(double panellength, double cornerlength, bool colorcodebylength, string bucketName, string uploadsBucket, Dictionary<string, string> modelInputKeys, string gltfKey, string elementsKey, string ifcKey): base(bucketName, uploadsBucket, modelInputKeys, gltfKey, elementsKey, ifcKey)
        {
			this.PanelLength = panellength;
			this.CornerLength = cornerlength;
			this.ColorCodeByLength = colorcodebylength;

		}

		public override string ToString()
		{
			var json = JsonConvert.SerializeObject(this);
			return json;
		}
	}
}
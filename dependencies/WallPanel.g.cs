//----------------------
// <auto-generated>
//     Generated using the NJsonSchema v10.1.21.0 (Newtonsoft.Json v12.0.0.0) (http://NJsonSchema.org)
// </auto-generated>
//----------------------
using Elements;
using Elements.GeoJSON;
using Elements.Geometry;
using Elements.Geometry.Solids;
using Elements.Properties;
using Elements.Validators;
using Elements.Serialization.JSON;
using System;
using System.Collections.Generic;
using System.Linq;
using Line = Elements.Geometry.Line;
using Polygon = Elements.Geometry.Polygon;

namespace Elements
{
    #pragma warning disable // Disable all warnings

    /// <summary>A subsection of a wall</summary>
    [Newtonsoft.Json.JsonConverter(typeof(Elements.Serialization.JSON.JsonInheritanceConverter), "discriminator")]
    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "10.1.21.0 (Newtonsoft.Json v12.0.0.0)")]
    [UserElement]
	public partial class WallPanel : GeometricElement
    {
        [Newtonsoft.Json.JsonConstructor]
        public WallPanel(string @identifier, Profile @profile, bool @isTrimmed, double @thickness, Transform @transform, Material @material, Representation @representation, bool @isElementDefinition, System.Guid @id, string @name)
            : base(transform, material, representation, isElementDefinition, id, name)
        {
            var validator = Validator.Instance.GetFirstValidatorForType<WallPanel>
            ();
            if(validator != null)
            {
                validator.PreConstruct(new object[]{ @identifier, @profile, @isTrimmed, @thickness, @transform, @material, @representation, @isElementDefinition, @id, @name});
            }
        
                this.Identifier = @identifier;
                this.Profile = @profile;
                this.IsTrimmed = @isTrimmed;
                this.Thickness = @thickness;
            
            if(validator != null)
            {
            validator.PostConstruct(this);
            }
            }
    
        /// <summary>The identifier of this section.</summary>
        [Newtonsoft.Json.JsonProperty("Identifier", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string Identifier { get; set; }
    
        /// <summary>The id of the profile to extrude.</summary>
        [Newtonsoft.Json.JsonProperty("Profile", Required = Newtonsoft.Json.Required.AllowNull)]
        public Profile Profile { get; set; }
    
        /// <summary>True if a panel is of irregular shape.</summary>
        [Newtonsoft.Json.JsonProperty("IsTrimmed", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public bool IsTrimmed { get; set; }
    
        /// <summary>The thickness of the Panel.</summary>
        [Newtonsoft.Json.JsonProperty("Thickness", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        [System.ComponentModel.DataAnnotations.Range(0D, double.MaxValue)]
        public double Thickness { get; set; }
    
    
    }
}
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using JetBrains.Annotations;
using Mesh = Xml2Unity.Editor.Mesh;
using Sprite = Xml2Unity.Editor.Sprite;
using Texture = Xml2Unity.Editor.Texture;

namespace Xml2Unity.Editor
{
    public class Library
    {
        private const string NAME = "XML2ULib";
        public string libraryName;
        public List<Prop> props;
        public string path;

        public Library(string path)
        {
            this.path = path;
            LoadFromPath(path);
        }

        public bool HasProp(string groupName, string propName)
        {
            return props.Count(prop => prop.groupName == groupName && prop.propName == propName) > 0;
        }

        [CanBeNull]
        public Prop GetProp(string groupName, string propName, string textureName = null)
        {
            if (!HasProp(groupName, propName)) return null;

            var prop = props.FirstOrDefault(prop => prop.groupName == groupName && prop.propName == propName);

            if (prop != null)
            {
                if (string.IsNullOrWhiteSpace(textureName))
                {
                    // set first texture in mesh list
                    //prop.textureName 
                }
                else
                {
                    //Debug.Log($"APPLIED TEXTURE {textureName} FOR {prop.propName}");
                    prop.textureName = textureName;
                }
            }
            
            return prop;
        }
        

        public void LoadFromPath(string path)
        {
            //Debug.Log($"[{NAME}:LoadFromPath] Loading library {path}");
            //var pathToImages = path + "/images.xml";
        /**
         * images.xml
<images>
	<image name='bush1.png' new-name='bush1.jpg' alpha='bush1.gif'/>
	<image name='bush2.png' new-name='bush2.jpg' alpha='bush2.gif'/>
	<image name='wood1.png' new-name='wood1.jpg' alpha='wood1.gif'/>
</images>
         */
            var pathToLibrary = $"{path}/library.xml";
        
            var xml = new XmlDocument();
            xml.Load(pathToLibrary);

            var xRoot = xml.DocumentElement;
            if (xRoot == null) return;
            
            // <library name="Land v1 Old">
            libraryName = xRoot.GetAttributeNode("name")?.Value;

            var list = new List<Prop>();

            // <prop-group name="Tiles">
            foreach (XmlElement group in xRoot)
            {
                var groupName = group.GetAttributeNode("name")?.Value;
                
                // <prop name="Tile 1x1">
                foreach (XmlElement prop in group)
                {
                    // Prop params
                    foreach (XmlElement element in prop) // InvalidCastException: Specified cast is not valid.
                    {
                        var propName = prop.GetAttribute("name");
                        var propObject = new Prop(path, libraryName, groupName, propName, "");
                        var elementType = element.Name;
                        
                        // one mesh, can be multiple or none textures inside it.

                        switch (elementType)
                        {
                            // <mesh file="tile_01.3DS">
                            case "mesh":
                                // mesh
                                var mesh = new Mesh()
                                {
                                    file = element.GetAttribute("file"),
                                    textures = new List<Texture>()
                                };
                                //Debug.Log($"{mesh.file} {libraryName}");
                                // textures
                                // <texture name="Tile Asp 1" diffuse-map="asp_1.jpg" />
                                foreach (XmlElement nElement in element) // InvalidCastException: Specified cast is not valid.
                                {
                                    if (nElement.Name != "texture") continue;
                                    var texture = new Texture()
                                    {
                                        diffuse_map = nElement.GetAttribute("diffuse-map"),
                                        name = nElement.GetAttribute("name")
                                    };
                                    mesh.textures.Add(texture);
                                }
                                propObject.meshes.Add(mesh);
                                break;
                            // </mesh>
                            // <sprite file="wood1.png" origin-y="0.99" scale="2.5"/>
                            case "sprite":
                                var sprite = new Sprite()
                                {
                                    file = element.GetAttribute("file"),
                                    origin_y = element.GetAttribute("origin-y"),
                                    scale = element.GetAttribute("scale")
                                };
                                propObject.sprites.Add(sprite);
                                break;
                            //default:
                                //Debug.LogWarning($"Unknown element type: {elementType}");
                                //break;
                        }
                        list.Add(propObject);
                    }
                }
            }
            props = list;
        }
    }
}

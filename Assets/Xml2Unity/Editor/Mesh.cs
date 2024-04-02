using System.Collections.Generic;

namespace Xml2Unity.Editor
{
    public class Mesh
    {
        // <mesh file="tile_01.3DS">

        public string file;
        public List<Texture> textures; // Can Be Empty
    }
}

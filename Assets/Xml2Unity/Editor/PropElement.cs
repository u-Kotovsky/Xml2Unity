namespace Xml2Unity.Editor
{ // Outdated
    public class PropElement
    {
        public string name; // (file)
        
        public string type; // mesh (like floor, walls), sprite (like bush)
        
        // Library Path (Assets/Resources/Library_v0/Landv1Old)
        public string path;
        
        // Mesh
        public string file; // mesh only, Model Filename (tile_01.3DS)

        // Texture
        public string texture; // (beton_tube.jpg)

        // Sprite
        public string origin_y; // "0.99"
        public string scale; // "2.5"
    }
}

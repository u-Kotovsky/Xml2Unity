using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Xml2Unity.Editor
{
    public class Prop
    {
        private const string NAME = "XML2UProp";
        private const string MAIN_PATH = "Assets/Xml2Unity/Resources/";
        public readonly string libraryName;
        public readonly string groupName; // (Tiles)
        public readonly string propName; // (Tile 1x1)
        public string textureName; // (beton_tube.jpg)

        //public readonly List<PropElement> elements;
        public List<Mesh> meshes;
        public List<Sprite> sprites;

        private string libraryPath;
        
        private static readonly int MainTex = Shader.PropertyToID("_MainTex");
        private static readonly int Color1 = Shader.PropertyToID("_Color");

        public Prop(string libraryPath, string libraryName, string groupName, string propName, string textureName)
        {
            //elements = new List<PropElement>();
            meshes = new List<Mesh>();
            sprites = new List<Sprite>();

            this.libraryPath = libraryPath;
            this.libraryName = libraryName;
            this.groupName = groupName;
            this.propName = propName;
            this.textureName = textureName;
        }

        public void Create(Vector3 position, Quaternion rotation, GameObject map)
        {
            foreach (var mesh in meshes)
            {
                var filename = mesh.file;
                var path = string.Join("/", libraryPath.Split('/').Skip(3)); // Remove assets path
                var modelPath = $"{path}/{string.Join(".", filename.Split('.').Take(1))}";
                //Debug.Log($"create mesh {modelPath} {path} {filename}");

                try
                {
                    var resource = Resources.Load<GameObject>(modelPath);
                    var propInstance = Object.Instantiate(resource, map.transform);
                    propInstance.transform.SetPositionAndRotation(position, rotation);
                    
                    var rotY = Mathf.RoundToInt(rotation.eulerAngles.y);
                    if (rotY == -180 || rotY == 180 || rotY == 0)
                        propInstance.transform.Rotate(0f, 180f, 0);
                    
                    foreach (Transform child in propInstance.transform)
                    {
                        ProcessPropChild(propInstance, child.gameObject);
                    }
                    
                    // Retrive texture
                    if (propInstance.TryGetComponent(out MeshRenderer meshRenderer))
                    {
                        UnityEngine.Texture texture = null;
                        if (textureName != "")
                        {
                            var _mesh = meshes.FirstOrDefault(mesh1 => mesh1.textures.Any(texture => texture.name == textureName));
                            var _tex = _mesh.textures.FirstOrDefault(texture => texture.name == textureName);
                            var texturePath = $"{path}/{string.Join(".", _tex.diffuse_map.Split('.').Take(1))}";
                            texture = Resources.Load<UnityEngine.Texture>(texturePath);
                        } else {
                            texture = meshRenderer.sharedMaterial.GetTexture(MainTex);
                        }
                        
                        // Apply material & texture
                        var material = new Material(meshRenderer.sharedMaterial);
                        material.SetColor(Color1, Color.white);
                        material.SetTexture(MainTex, texture);
                        meshRenderer.sharedMaterial = material;

                        meshRenderer.sharedMaterial.mainTexture.filterMode = FilterMode.Point;
                    }

                    if (Xml2Unity.debugText)
                        ApplyDebugText(propInstance, mesh);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Cannot find prop {modelPath}");
                    Debug.LogException(e);
                }
            }
        }

        private void ApplyDebugText(GameObject propInstance, Mesh mesh)
        {
            var info = new GameObject("Info");
            info.transform.SetParent(propInstance.transform);
            info.transform.SetLocalPositionAndRotation(new Vector3(0, 250, 0), new Quaternion());
            var text = info.AddComponent<TextMeshPro>();
            text.enableAutoSizing = true;
            text.horizontalAlignment = HorizontalAlignmentOptions.Left;
            text.verticalAlignment = VerticalAlignmentOptions.Middle;
            text.fontSizeMin = 10;
            info.transform.localScale = new Vector3(10f, 10f, 10f);
            text.text = $"Library: {libraryName}<br>Prop: {propName}<br>Group: {groupName}<br>Texture: {textureName}<br>File: {mesh.file}";
        }

        private static void ProcessPropChild(GameObject parent, GameObject child)
        {
            if (child.name.ToLower().Contains("box"))
            {
                child.AddComponent<BoxCollider>();
                child.GetComponent<Renderer>().enabled = false;
            }

            if (child.name.ToLower().Contains("plane"))
            {
                child.AddComponent<MeshCollider>();
                child.GetComponent<Renderer>().enabled = false;
                
            }

            if (child.name.ToLower().Contains("tri"))
            {
                if (!parent.TryGetComponent<MeshCollider>(out var collider)) 
                    parent.AddComponent<MeshCollider>();
                child.SetActive(false);
                //child.GetComponent<Renderer>().enabled = false;
            }

            if (child.name.ToLower().Contains("occl"))
            {
                child.SetActive(false);
            }
            
            Object.DestroyImmediate(child.gameObject);
        }
    }
}

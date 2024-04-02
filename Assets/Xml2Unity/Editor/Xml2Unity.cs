using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using UnityEditor;
using UnityEngine;

namespace Xml2Unity.Editor
{
    public class Xml2Unity : EditorWindow
    {
        private const string MainPath = "Assets/Xml2Unity/Resources/"; // Don't use hardocded paths
        private const string Name = "XML2Unity";
        private const float Scale = 0.005f;

        public static bool debugText;
        //public static bool colliders; // Could be accessed from other scripts when building map?
        //public static bool deleteUseless;

        private string _currentXml = "";
        private int _currentXmlIndex = 0;
        private readonly List<string> _maps = new List<string>()
        {
            "None"
        };

        private GameObject _replaceExample;
        private GameObject _replaceWith;

        private GameObject _latestBuild;
        private bool _overwriteLatestBuild;


        [MenuItem("Tools/Xml2Unity Window")]
        public static void OpenWindow()
        {
            GetWindow<Xml2Unity>();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Load xmls")) SearchXmls();
            if (GUILayout.Button("Clear xmls")) ClearXmls();
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.LabelField($"({_currentXmlIndex}/{_maps.Count}): {_currentXml}");
            RefreshXmlIndex();
            debugText = EditorGUILayout.Toggle("Debug:", debugText);

            EditorGUILayout.BeginHorizontal();
            _overwriteLatestBuild = EditorGUILayout.Toggle("Overwrite latest build?:", _overwriteLatestBuild);
            //deleteUseless = EditorGUILayout.Toggle("Delete useless", deleteUseless);
            //colliders = EditorGUILayout.Toggle("Add colliders", colliders);
            EditorGUILayout.EndHorizontal();

            if (_latestBuild != null)
            {
                EditorGUILayout.BeginHorizontal();
                BuildXmlButton();
                if (GUILayout.Button("Delete latest build"))
                {
                    DestroyImmediate(_latestBuild);
                    _latestBuild = null;
                }
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                BuildXmlButton();
            }
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Latest build");
            _latestBuild = EditorGUILayout.ObjectField(_latestBuild, typeof(GameObject), true) as GameObject;
            EditorGUILayout.EndHorizontal();

            if (_latestBuild != null)
            {
                ClearBullshitButton();
                ReplaceAllGroup();
            }
        }

        private void ReplaceAllGroup()
        {
            EditorGUILayout.LabelField("Replace example with other GameObject");
            EditorGUILayout.BeginHorizontal();
            _replaceExample = EditorGUILayout.ObjectField(_replaceExample, typeof(GameObject), true) as GameObject;
            _replaceWith = EditorGUILayout.ObjectField(_replaceWith, typeof(GameObject), true) as GameObject;
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Replace All"))
            {
                if (_replaceExample == null)
                {
                    Debug.LogError($"Replace example is null!");
                    return;
                }
                if (_replaceWith == null)
                {
                    Debug.LogError($"Replace with is null!");
                    return;
                }
                var name = _replaceExample.name;
                foreach (Transform child in _latestBuild.transform)
                {
                    if (child.gameObject.name == name)
                    {
                        var instance = Instantiate(_replaceWith, _latestBuild.transform);
                        instance.transform.SetPositionAndRotation(child.position, child.rotation);
                        DestroyImmediate(child);
                    }
                }
            }
        }

        private void ClearBullshitButton()
        {
            if (GUILayout.Button("Clear bullshit"))
            {
                foreach (Transform prop in _latestBuild.transform)
                {
                    foreach (Transform propChild in prop)
                    {
                        DestroyImmediate(propChild.gameObject);
                    }
                }
            }
        }

        private void RefreshXmlIndex()
        {
            _currentXmlIndex = EditorGUILayout.Popup(_currentXmlIndex, _maps.ToArray(), new GUILayoutOption[] { });
            _currentXml = _maps.Count - 1 > _currentXmlIndex ? _maps[_currentXmlIndex] : _maps[0];
        }

        private void BuildXmlButton()
        {
            if (GUILayout.Button("Build"))
            {
                if (_overwriteLatestBuild && _latestBuild != null)
                {
                    //Undo.RecordObject(_latestBuild, $"Destroy {_latestBuild.name}");
                    DestroyImmediate(_latestBuild);
                }
            
                var str1 = _currentXml.Split("/");
                if (str1.Length < 2)
                {
                    Debug.LogError($"[{Name}:BuildXmlButton] Failed to find selected xml");
                    return;
                }
            
                var str = _currentXml.Split("/")[3].Split("_v")[1];
                if (!int.TryParse(str, out var version))
                {
                    Debug.LogError($"[{Name}:BuildXmlButton] Failed to find version of the xml");
                    return;
                }
            
                Debug.Log($"[{Name}:BuildXmlButton] Xml version: {str}");
                _latestBuild = LoadXml(version, _currentXml);
                //Undo.RecordObject(_latestBuild, $"update {_latestBuild.name}");
            }
        }

        private void ClearXmls()
        {
            _maps.Clear();
            _maps.Add("None");
            Debug.Log($"[{Name}:ClearXmls] Xmls cleared!");
        }

        private void SearchXmls()
        {
            ClearXmls();
            Debug.Log($"{MainPath}Maps_v0/");
            SearchXmlInDirectory(MainPath + "Maps_v0/");
            SearchXmlInDirectory(MainPath + "Maps_v1/");
            Debug.Log($"[{Name}:SearchXmls] Xmls are loaded!");
            RefreshXmlIndex();
        }

        private void SearchXmlInDirectory(string str)
        {
            var dir = string.Join("/", str.Split(@"\"));
            //while (dir.Contains("\\"))
            //    dir = dir.Replace("\\", "/");

            // Check for .xml files
            var files = Directory.GetFiles(dir);
            var xmls = files.Where(file => file.ToLower().EndsWith(".xml"));
            _maps.AddRange(xmls);
            // Check for dirs
            var dirs = Directory.GetDirectories(dir);
            foreach (var s in dirs)
                SearchXmlInDirectory(s);
        }

        private static GameObject LoadXml(int version, string path)
        {
            //var pathArray = path.Split('/');
            //var mapName = string.Join("/", pathArray.Skip(pathArray.Length));
            var map = new GameObject(path.Split("/")[^1]);

            try
            {
                var libs = Directory
                    .GetDirectories($"{MainPath}Library_v{version}/")
                    .Select(directory => new Library(directory))
                    .ToList();
                ParseXml(map, path, libs);
            }
            catch (Exception e)
            { // Handle this exception so it will return empty GameObject in case it breaks and could be replaced.
                Debug.LogException(e);
                map.SetActive(false);
            }
        
            return map;
        }

        private static void ParseXml(GameObject map, string path, List<Library> libs)
        {
            var xml = new XmlDocument();
            xml.Load(path);

            // <map version="1.0">
            var xRoot = xml.DocumentElement;
            if (xRoot == null) return;
    
            // <static-geometry>
            var staticGeometry = xRoot["static-geometry"];
            if (staticGeometry != null)
            {
                Debug.Log($"[{Name}:ParseXml] Parsing static geometry..");
                foreach (XmlElement prop in staticGeometry)
                    ParseProp(map, prop, libs);
                
            }
    
            
            // <collision-geometry>
            var collisionGeometry = xRoot["collision-geometry"];
            if (collisionGeometry != null)
            {
                Debug.Log($"[{Name}:ParseXml] Parsing collision geometry..");
                
            }
            
            /**
    <collision-plane id="0">
      <width>500.000</width>
      <length>500.000</length>
      <position>
        <x>4250.000</x>
        <y>-250.000</y>
        <z>0.000</z>
      </position>
      <rotation>
        <x>0.000000</x>
        <y>0.000000</y>
        <z>-3.141593</z>
      </rotation>
    </collision-plane>
    <collision-box id="0">
      <size>
        <x>100.000</x>
        <y>500.000</y>
        <z>400.000</z>
      </size>
      <position>
        <x>-2250.000</x>
        <y>2250.000</y>
        <z>500.000</z>
      </position>
      <rotation>
        <x>0.000000</x>
        <y>0.000000</y>
        <z>0.000000</z>
      </rotation>
    </collision-box>
             */

            // <spawn-points>
            var spawnPoints = xRoot["spawn-points"];
            if (spawnPoints != null)
            {
                Debug.Log($"[{Name}:ParseXml] Parsing spawn points..");
                
            }
                
            // <bonus-region>
            var bonusRegions = xRoot["bonus-region"];
            if (bonusRegions != null)
            {
                Debug.Log($"[{Name}:ParseXml] Parsing bonus regions..");
                foreach (XmlElement region in bonusRegions)
                {
                    Vector3 position = GetVector3(region["position"]);
                    Quaternion rotation = GetQuaternion(region["rotation"]);
                    Vector3 min = GetVector3(region["min"]);
                    Vector3 max = GetVector3(region["max"]);
                    string type = region["bonus-type"]?.Value;
                    Debug.Log($"new bonus region {type}");
                }
            }
            
            /**
    <bonus-region name="bonus1" free="true">
      <position>
        <x>1750.000</x>
        <y>-2250.000</y>
        <z>1200.000</z>
      </position>
      <rotation>
        <z>0.000</z>
      </rotation>
      <min>
        <x>1500.000</x>
        <y>-2500.000</y>
        <z>1200.000</z>
      </min>
      <max>
        <x>2000.000</x>
        <y>-2000.000</y>
        <z>1500.000</z>
      </max>
      <bonus-type>medkit</bonus-type>
    </bonus-region>
             */
            
            // <ctf-flags>
            var ctfFlags = xRoot["ctf-flags"];
            if (ctfFlags != null)
            {
                Debug.Log($"[{Name}:ParseXml] Parsing ctf flags..");
                Vector3? flagRed = null;
                Vector3? flagBlue = null;

                // <flag-red>
                foreach (XmlElement ctfFlag in ctfFlags)
                {
                    switch (ctfFlag.Name)
                    {
                        case "flag-red":
                            flagRed = GetVector3(ctfFlag);
                            break;
                        case "flag-blue":
                            flagBlue = GetVector3(ctfFlag);
                            break;
                        default:
                            Debug.LogError($"[{Name}:ParseXml] Unknown ctf flag {ctfFlag.Name}");
                            break;
                    }
                }

                if (flagRed.HasValue)
                {
                    Debug.Log($"[{Name}:ParseXml] new position for red flag: {flagRed.Value.x} {flagRed.Value.y} {flagRed.Value.z}");
                }

                if (flagBlue.HasValue)
                {
                    Debug.Log($"[{Name}:ParseXml] new position for blue flag: {flagBlue.Value.x} {flagBlue.Value.y} {flagBlue.Value.z}");
                }
            }

            // Apply correct scaling
             map.transform.localScale = new Vector3(Scale, Scale, Scale); 
            Debug.Log($"[{Name}:ParseXml] Map parsed");
        }

        public static void ParseProp(GameObject map, XmlElement prop, IEnumerable<Library> libs)
        {
            // <prop library-name="Landscape Old" group-name="Concrete&amp;Pavement Tiles" name="Pave 1x1">
            var libraryName = prop.GetAttributeNode("library-name")?.Value;
            var groupName = prop.GetAttributeNode("group-name")?.Value;
            var name = prop.GetAttributeNode("name")?.Value;
            var textureName = string.Empty; // texture_name

            var position = new Vector3();
            var rotation = new Quaternion();

            foreach (XmlElement element in prop)
            {
                switch (element.Name)
                {
                    // <position> ...
                    case "position":
                        position = GetVector3(element);
                        break;
                    // <rotation> ...
                    case "rotation":
                        rotation = GetQuaternion(element);
                        break;
                    // <texture-name>PtG trans 2</texture-name>
                    case "texture-name":
                        // get text from element
                        foreach (XmlText text in element)
                        {
                            textureName = text.Value;
                        }
                        break;
                    default:
                        Debug.LogWarning($"[{Name}:ParseProp] Unknown element name: {element.Name} = {element.Value}");
                        break;
                }
            }
        
            //if (textureName == string.Empty) Debug.LogWarning("Texture name is null (empty)!");
        
            rotation.eulerAngles = new Vector3(0f, rotation.eulerAngles.y, 0f);

            // Place prop in scene
            var library = libs.FirstOrDefault(lib => lib.libraryName == libraryName);
            var propInfo = library?.GetProp(groupName, name, textureName);
            //propInfo?.textureName = textureName;
            propInfo?.Create(position, rotation, map);
        }

        private static Vector3 GetVector3(XmlElement value)
        {
            float x = 0;
            float y = 0;
            float z = 0;
            
            // <x>
            foreach (XmlElement param in value)
            {
                // get text value and parse it
                foreach (XmlText paramChild in param)
                {
                    switch (param.Name)
                    {
                        case "x":
                            x = float.Parse(paramChild.Value, CultureInfo.InvariantCulture);
                            break;
                        case "z":
                            y = float.Parse(paramChild.Value, CultureInfo.InvariantCulture);
                            break;
                        case "y":
                            z = float.Parse(paramChild.Value, CultureInfo.InvariantCulture);
                            break;
                        default:
                            Debug.LogError($"[{Name}:GetVector3] Unknown param name: {param.Name}");
                            break;
                    }
                }
            }
                    
            return new Vector3(x, y, z);
        }

        private static Quaternion GetQuaternion(XmlElement value)
        {
            float x = 0;
            float y = 0;
            float z = 0;

            // <x>
            foreach (XmlElement param in value)
            {
                // Retrieve text value and parse it
                foreach (XmlText paramChild in param)
                {
                    switch (param.Name)
                    {
                        case "x":
                            x = float.Parse(paramChild.Value, CultureInfo.InvariantCulture);
                            break;
                        case "z":
                            y = float.Parse(paramChild.Value, CultureInfo.InvariantCulture);
                            break;
                        case "y":
                            z = float.Parse(paramChild.Value, CultureInfo.InvariantCulture);
                            break;
                        default:
                            Debug.LogWarning($"[{Name}:GetQuaternion] Unknown param name: {param.Name}");
                            break;
                    }
                }
            }
    
            return Quaternion.Euler(
                GetDegree(x), 
                GetDegree(y), 
                GetDegree(z));
        }

        private static float GetDegree(double radian)
        {
            return (float)(radian * 180 / Math.PI);
        }
    }
}
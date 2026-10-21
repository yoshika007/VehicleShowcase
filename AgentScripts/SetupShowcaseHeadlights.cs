using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;

public static class SetupShowcaseHeadlights
{
    const string Folder = "Assets/Prefabs/Vehicles/Lighting";
    static bool IsHeadlamp(Mesh mesh, int[] triangles, int i)
    {
        var vertices = mesh.vertices; var uv = mesh.uv;
        int a = triangles[i], b = triangles[i+1], c = triangles[i+2];
        var center = (vertices[a]+vertices[b]+vertices[c])/3f;
        var sample = (uv[a]+uv[b]+uv[c])/3f;
        // The model pack's white lamp palette cell, on the front quarter only.
        return sample.x > .24f && sample.x < .32f && sample.y > .93f &&
            center.z > mesh.bounds.center.z + mesh.bounds.size.z * .25f &&
            center.y < mesh.bounds.min.y + mesh.bounds.size.y * .72f;
    }
    public static string Main()
    {
        if (EditorApplication.isPlaying) throw new Exception("Edit Mode required");
        var paths = AssetDatabase.FindAssets("t:Prefab", new[]{"Assets/Prefabs/Vehicles"})
            .Select(AssetDatabase.GUIDToAssetPath).Where(p=>p.EndsWith("_Showcase.prefab")).ToArray();
        // Validate the whole set before writing any prefab.
        foreach (var path in paths)
        {
            var root=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if(root.GetComponent<ShowcaseHeadlights>()!=null) throw new Exception("Already configured: "+path);
            var mesh=root.GetComponent<MeshFilter>().sharedMesh; var t=mesh.GetTriangles(1);
            var selected=Enumerable.Range(0,t.Length/3).Where(i=>IsHeadlamp(mesh,t,i*3)).SelectMany(i=>new[]{t[i*3],t[i*3+1],t[i*3+2]}).ToArray();
            if(!selected.Any(i=>mesh.vertices[i].x<-.1f)||!selected.Any(i=>mesh.vertices[i].x>.1f)) throw new Exception("Missing lamp pair: "+path);
        }
        if(!AssetDatabase.IsValidFolder(Folder))AssetDatabase.CreateFolder("Assets/Prefabs/Vehicles","Lighting");
        var material=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="Showcase Headlamp Lenses"};
        material.EnableKeyword("_EMISSION");material.SetColor("_BaseColor",new Color(.8f,.82f,.78f));material.SetColor("_EmissionColor",Color.white);material.globalIlluminationFlags=MaterialGlobalIlluminationFlags.RealtimeEmissive;material.SetFloat("_Smoothness",.7f);
        AssetDatabase.CreateAsset(material,Folder+"/Showcase Headlamp Lenses.mat");
        var report=new List<string>();
        foreach(var path in paths)
        {
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var filter=root.GetComponent<MeshFilter>();var original=filter.sharedMesh;var t=original.GetTriangles(1);var v=original.vertices;
                var chosen=Enumerable.Range(0,t.Length/3).Where(i=>IsHeadlamp(original,t,i*3)).ToArray();var set=new HashSet<int>(chosen);
                var body=Object.Instantiate(original);body.name=root.name+" Body";
                body.SetTriangles(Enumerable.Range(0,t.Length/3).Where(i=>!set.Contains(i)).SelectMany(i=>new[]{t[i*3],t[i*3+1],t[i*3+2]}).ToArray(),1);
                AssetDatabase.CreateAsset(body,Folder+"/"+root.name+" Body.asset");filter.sharedMesh=body;
                var indices=chosen.SelectMany(i=>new[]{t[i*3],t[i*3+1],t[i*3+2]}).ToArray();
                var mesh=new Mesh{name=root.name+" Headlamp Lenses"};mesh.vertices=indices.Select(i=>v[i]).ToArray();mesh.normals=indices.Select(i=>original.normals[i]).ToArray();mesh.triangles=Enumerable.Range(0,indices.Length).ToArray();mesh.RecalculateBounds();
                AssetDatabase.CreateAsset(mesh,Folder+"/"+root.name+" Headlamp Lenses.asset");
                var lenses=new GameObject("Showcase Headlamp Lenses",typeof(MeshFilter),typeof(MeshRenderer));lenses.transform.SetParent(root.transform,false);lenses.GetComponent<MeshFilter>().sharedMesh=mesh;
                var renderer=lenses.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
                var lights=new List<Light>();
                foreach(int side in new[]{-1,1})
                {
                    var points=indices.Select(i=>v[i]).Where(p=>p.x*side>0).ToArray();var bounds=new Bounds(points[0],Vector3.zero);foreach(var p in points)bounds.Encapsulate(p);
                    var go=new GameObject(side<0?"Left Headlight Beam":"Right Headlight Beam");go.transform.SetParent(root.transform,false);go.transform.localPosition=new Vector3(bounds.center.x,bounds.center.y,bounds.max.z+.04f);go.transform.localRotation=Quaternion.Euler(12,0,0);
                    var light=go.AddComponent<Light>();light.type=LightType.Spot;light.color=new Color(1,.94f,.78f);light.range=9;light.spotAngle=65;light.innerSpotAngle=35;light.intensity=0;light.enabled=false;light.shadows=LightShadows.None;lights.Add(light);
                }
                var component=root.AddComponent<ShowcaseHeadlights>();var so=new SerializedObject(component);so.FindProperty("lenses").objectReferenceValue=renderer;var beams=so.FindProperty("beams");beams.arraySize=lights.Count;for(int i=0;i<lights.Count;i++)beams.GetArrayElementAtIndex(i).objectReferenceValue=lights[i];so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root,path);report.Add(root.name+": "+chosen.Length+" lens triangles, 2 beams");
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        AssetDatabase.SaveAssets();return string.Join("\n",report);
    }
}

using System;using System.Linq;using System.Collections.Generic;using System.Reflection;using UnityEngine;using UnityEditor;using UnityEngine.SceneManagement;

// Rebuild the existing map asset from the main loop plus repaired branch visuals.
// Keep the map GUID, UI assignments, player marker, start line and dirty scene intact.
public static class RebuildLakeTownMinimap
{
 public static string Run(bool apply)
 {
  if(EditorApplication.isPlaying||SceneManager.GetActiveScene().path!="Assets/Scenes/LakeTownScene.unity")throw new Exception("Open Lake Town in Edit Mode");
  var map=UnityEngine.Object.FindAnyObjectByType<TestDriveMinimap>();var data=new SerializedObject(map);
  var target=(Mesh)data.FindProperty("roadMesh").objectReferenceValue;var frame=(Transform)data.FindProperty("roadTransform").objectReferenceValue;
  if(target==null||frame==null||AssetDatabase.GetAssetPath(target)!="Assets/Environments/LakeTownTrial/RoadNetwork.asset")throw new Exception("Unexpected map asset or transform");
  var town=UnityEngine.Object.FindAnyObjectByType<LakeTownNightLights>().transform;var filters=town.GetComponentsInChildren<MeshFilter>(true);
  var main=filters.Single(f=>f.name=="Lake Town Road");var names=new[]{"Market Street","Marina Avenue","Old Town Cross Street"};
  var branches=filters.Where(f=>names.Any(n=>f.name.StartsWith(n+" ("))&&f.GetComponent<Renderer>()!=null&&f.GetComponent<Renderer>().enabled).ToArray();
  if(names.Any(n=>!branches.Any(f=>f.name.StartsWith(n+" ("))))throw new Exception("Missing repaired branch render meshes");
  var vertices=new List<Vector3>();var indices=new List<int>();var lookup=new Dictionary<Vector2,int>();int removedDegenerate=0;
  foreach(var filter in new[]{main}.Concat(branches))
  {
   var mesh=filter.sharedMesh;var world=mesh.vertices.Select(filter.transform.TransformPoint).ToArray();var triangles=mesh.triangles;
   for(int i=0;i<triangles.Length;i+=3)
   {
    var a=world[triangles[i]];var b=world[triangles[i+1]];var c=world[triangles[i+2]];
    double area=(double)(b.x-a.x)*(c.z-a.z)-(double)(b.z-a.z)*(c.x-a.x);
    if(Math.Abs(area)<.00000001){removedDegenerate++;continue;}
    // Preserve the real road winding while discarding unused old endpoint vertices.
    for(int k=0;k<3;k++)
    {
     var point=world[triangles[i+k]];var key=new Vector2(point.x,point.z);
     if(!lookup.TryGetValue(key,out int index)){index=vertices.Count;lookup.Add(key,index);vertices.Add(frame.InverseTransformPoint(point));}
     indices.Add(index);
    }
   }
  }
  if(vertices.Count<600||vertices.Count>5000||indices.Count==0)throw new Exception("Unexpected map geometry budget");
  int beforeVertices=target.vertexCount,beforeTriangles=target.triangles.Length/3;
  if(apply)
  {
   Undo.RecordObject(target,"Match Lake Town minimap to repaired roads");target.Clear();target.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;
   target.SetVertices(vertices);target.SetTriangles(indices,0);target.RecalculateNormals();target.RecalculateBounds();EditorUtility.SetDirty(target);AssetDatabase.SaveAssets();
   // Same asset reference: refresh the transient Graphic cache without changing scene data.
   typeof(TestDriveMinimap).GetField("cachedMesh",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(map,null);map.SetVerticesDirty();
  }
  return (apply?"Applied":"Preview")+": map uses the main loop and "+branches.Length+" repaired street sections; "+beforeVertices+" -> "+vertices.Count+" vertices, "+beforeTriangles+" -> "+indices.Count/3+" triangles. "+removedDegenerate+" degenerate faces excluded. No UI or scene assignments changed.";
 }
}

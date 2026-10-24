using System;using System.Linq;using System.Collections.Generic;using UnityEngine;using UnityEditor;using UnityEngine.SceneManagement;

// Cut only the outer loop's white edge markings where actual inner-road asphalt overlaps.
// Existing road colliders, scene objects, and all unaffected line triangles stay intact.
public static class TrimLakeTownJunctionLines
{
 struct Vertex
 {
  public Vector3 p,n;public int index;
  public static Vertex Lerp(Vertex a,Vertex b,float t)
  {if(t<.000001f)return a;if(t> .999999f)return b;return new Vertex{p=Vector3.Lerp(a.p,b.p,t),n=Vector3.Lerp(a.n,b.n,t).normalized,index=-1};}
 }
 class Plan{public Mesh mesh;public Vector3[] positions,normals;public int[] triangles;public int cut;public double removedArea;}
 static float Coordinate(Vertex v,int axis)=>axis==0?v.p.x:v.p.z;
 static List<Vertex> Clip(List<Vertex> polygon,int axis,float boundary,bool greater)
 {
  var output=new List<Vertex>();if(polygon.Count==0)return output;var previous=polygon[polygon.Count-1];
  float old=Coordinate(previous,axis)-boundary;bool oldInside=greater?old>=0:old<=0;
  foreach(var current in polygon)
  {
   float distance=Coordinate(current,axis)-boundary;bool inside=greater?distance>=0:distance<=0;
   if(inside!=oldInside)output.Add(Vertex.Lerp(previous,current,old/(old-distance)));
   if(inside)output.Add(current);previous=current;old=distance;oldInside=inside;
  }
  return output;
 }
 static double Area(List<Vertex> polygon)
 {
  double sum=0;for(int i=0;i<polygon.Count;i++){var a=polygon[i].p;var b=polygon[(i+1)%polygon.Count].p;sum+=(double)a.x*b.z-(double)b.x*a.z;}return Math.Abs(sum)*.5;
 }
 static List<Vertex> Intersection(List<Vertex> polygon,Rect road)
 {
  polygon=Clip(polygon,0,road.xMin,true);polygon=Clip(polygon,0,road.xMax,false);polygon=Clip(polygon,1,road.yMin,true);return Clip(polygon,1,road.yMax,false);
 }
 static List<List<Vertex>> Subtract(List<Vertex> polygon,Rect road)
 {
  if(Area(Intersection(polygon,road))<.00000001)return new List<List<Vertex>>{polygon};
  var output=new List<List<Vertex>>();var inside=polygon;
  int[] axes={0,0,1,1};float[] boundaries={road.xMin,road.xMax,road.yMin,road.yMax};bool[] sides={true,false,true,false};
  for(int i=0;i<4&&inside.Count>0;i++)
  {
   var outside=Clip(inside,axes[i],boundaries[i],!sides[i]);if(Area(outside)>.00000001)output.Add(outside);
   inside=Clip(inside,axes[i],boundaries[i],sides[i]);
  }
  return output;
 }
 static Plan Prepare(MeshFilter filter,Rect[] roads)
 {
  var mesh=filter.sharedMesh;var positions=mesh.vertices;var normals=mesh.normals;var original=mesh.triangles;
  if(mesh.subMeshCount!=1||normals.Length!=positions.Length||mesh.GetVertexAttributes().Any(a=>a.attribute!=UnityEngine.Rendering.VertexAttribute.Position&&a.attribute!=UnityEngine.Rendering.VertexAttribute.Normal))throw new Exception("Unexpected line mesh attributes");
  var addedPositions=positions.ToList();var addedNormals=normals.ToList();var triangles=new List<int>();int cuts=0;double removed=0;
  for(int i=0;i<original.Length;i+=3)
  {
   var polygon=Enumerable.Range(0,3).Select(k=>new Vertex{p=filter.transform.TransformPoint(positions[original[i+k]]),n=normals[original[i+k]],index=original[i+k]}).ToList();
   var pieces=new List<List<Vertex>>{polygon};double before=Area(polygon);
   foreach(var road in roads)pieces=pieces.SelectMany(p=>Subtract(p,road)).ToList();
   double after=pieces.Sum(Area);
   if(before-after<.00000001){triangles.AddRange(new[]{original[i],original[i+1],original[i+2]});continue;}
   cuts++;removed+=before-after;
   foreach(var piece in pieces)
   {
    int[] indices=piece.Select(v=>
    {
     if(v.index>=0)return v.index;int next=addedPositions.Count;addedPositions.Add(filter.transform.InverseTransformPoint(v.p));addedNormals.Add(v.n);return next;
    }).ToArray();
    for(int k=1;k<piece.Count-1;k++)
     if(Area(new List<Vertex>{piece[0],piece[k],piece[k+1]})>.00000001)triangles.AddRange(new[]{indices[0],indices[k],indices[k+1]});
   }
  }
  // Every retained triangle must be completely clear of the inner-road asphalt.
  for(int i=0;i<triangles.Count;i+=3)
  {
   var triangle=Enumerable.Range(0,3).Select(k=>new Vertex{p=filter.transform.TransformPoint(addedPositions[triangles[i+k]])}).ToList();
   if(roads.Any(r=>Area(Intersection(triangle,r))>.00001))throw new Exception("A retained edge-line triangle still crosses an inner road");
  }
  return new Plan{mesh=mesh,positions=addedPositions.ToArray(),normals=addedNormals.ToArray(),triangles=triangles.ToArray(),cut=cuts,removedArea=removed};
 }
 public static string Run(bool apply)
 {
  if(EditorApplication.isPlaying||SceneManager.GetActiveScene().path!="Assets/Scenes/LakeTownScene.unity")throw new Exception("Open Lake Town in Edit Mode");
  var town=UnityEngine.Object.FindAnyObjectByType<LakeTownNightLights>().transform;
  var names=new[]{"Market Street","Marina Avenue","Old Town Cross Street"};
  var roads=names.Select(name=>
  {
   var filter=town.GetComponentsInChildren<MeshFilter>(true).Single(f=>f.name==name);if(filter.GetComponent<MeshCollider>()==null)throw new Exception("Original street collider missing");
   var world=filter.sharedMesh.vertices.Select(filter.transform.TransformPoint).ToArray();
   float xMin=world.Min(v=>v.x),xMax=world.Max(v=>v.x),zMin=world.Min(v=>v.z),zMax=world.Max(v=>v.z);
   if(!Mathf.Approximately(Mathf.Min(xMax-xMin,zMax-zMin),12f))throw new Exception("Unexpected inner-road width");
   return Rect.MinMaxRect(xMin,zMin,xMax,zMax);
  }).ToArray();
  var filters=new[]{"Road left edge","Road right edge"}.Select(name=>town.GetComponentsInChildren<MeshFilter>(true).Single(f=>f.name==name)).ToArray();
  foreach(var filter in filters)
   if(filter.GetComponent<Collider>()!=null||AssetDatabase.GetAssetPath(filter.sharedMesh)!="Assets/Environments/LakeTownTrial/"+filter.name+".asset")throw new Exception("Unexpected road-edge target");
  var plans=filters.Select(f=>Prepare(f,roads)).ToArray();
  if(plans.All(p=>p.cut==0))return "All edge lines already clear of inner-road asphalt";
  if(apply)
  {
   Undo.RecordObjects(plans.Select(p=>(UnityEngine.Object)p.mesh).ToArray(),"Clear Lake Town junction edge lines");
   foreach(var p in plans){p.mesh.SetVertices(p.positions);p.mesh.SetNormals(p.normals);p.mesh.SetTriangles(p.triangles,0);p.mesh.RecalculateBounds();EditorUtility.SetDirty(p.mesh);}
   AssetDatabase.SaveAssets();SceneView.RepaintAll();
  }
  return (apply?"Applied: ":"Preview: ")+string.Join("; ",plans.Select(p=>p.mesh.name+": "+p.cut+" affected source triangles, "+(p.removedArea/.17).ToString("F2")+"m marking removed, "+p.triangles.Length/3+" triangles retained"))+". Scene objects and road colliders untouched.";
 }
}

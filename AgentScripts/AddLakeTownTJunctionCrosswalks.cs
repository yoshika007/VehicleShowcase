using System;using System.Linq;using System.Collections.Generic;using UnityEngine;using UnityEditor;using UnityEngine.SceneManagement;

// Editor-baked paint in existing infield mesh batches; no scene save or runtime objects.
public static class AddLakeTownTJunctionCrosswalks
{
 const string MeshPath="Assets/Environments/LakeTownTrial/InfieldStreets/Infield Detail Meshes.asset";
 const float Height=.069f;
 class Crossing{public string label;public Vector3 center,inward,across;public Rect footprint,clearance;public MeshFilter batch;public List<Vector3> vertices=new List<Vector3>();}
 class Update{public Mesh mesh;public List<Vector3> vertices,normals;public List<int> triangles;}
 static Vector3 Flat(Vector3 p){p.y=0;return p;}
 static Rect Envelope(Vector3 center,Vector3 inward,float along,float across)
 {
  float x=Mathf.Abs(inward.x)*along+Mathf.Abs(inward.z)*across,z=Mathf.Abs(inward.z)*along+Mathf.Abs(inward.x)*across;
  return Rect.MinMaxRect(center.x-x,center.z-z,center.x+x,center.z+z);
 }
 static bool Inside(Vector3 point,Vector3[] loop)
 {
  bool inside=false;for(int i=0,j=loop.Length-1;i<loop.Length;j=i++)
   if((loop[i].z>point.z)!=(loop[j].z>point.z)&&point.x<(loop[j].x-loop[i].x)*(point.z-loop[i].z)/(loop[j].z-loop[i].z)+loop[i].x)inside=!inside;
  return inside;
 }
 static bool TriangleOverlaps(Vector3 a,Vector3 b,Vector3 c,Rect rect)
 {
  if(Mathf.Max(a.x,b.x,c.x)<=rect.xMin||Mathf.Min(a.x,b.x,c.x)>=rect.xMax||Mathf.Max(a.z,b.z,c.z)<=rect.yMin||Mathf.Min(a.z,b.z,c.z)>=rect.yMax)return false;
  // Triangle-vs-rectangle separating axes; X and Z were checked by the broad phase.
  var center=rect.center;var half=rect.size*.5f;var points=new[]{a,b,c};
  for(int i=0;i<3;i++)
  {
   var edge=points[(i+1)%3]-points[i];var axis=new Vector2(-edge.z,edge.x);
   float pa=a.x*axis.x+a.z*axis.y,pb=b.x*axis.x+b.z*axis.y,pc=c.x*axis.x+c.z*axis.y;
   float middle=Vector2.Dot(center,axis),radius=half.x*Mathf.Abs(axis.x)+half.y*Mathf.Abs(axis.y);
   if(Mathf.Max(pa,pb,pc)<=middle-radius||Mathf.Min(pa,pb,pc)>=middle+radius)return false;
  }
  return true;
 }
 static bool Clear(Rect footprint,Vector3[] loop,Vector3[] road,int[] triangles)
 {
  foreach(var corner in new[]{new Vector3(footprint.xMin,0,footprint.yMin),new Vector3(footprint.xMin,0,footprint.yMax),new Vector3(footprint.xMax,0,footprint.yMin),new Vector3(footprint.xMax,0,footprint.yMax)})if(!Inside(corner,loop))return false;
  var expanded=Rect.MinMaxRect(footprint.xMin-2,footprint.yMin-2,footprint.xMax+2,footprint.yMax+2);
  for(int i=0;i<triangles.Length;i+=3)if(TriangleOverlaps(road[triangles[i]],road[triangles[i+1]],road[triangles[i+2]],expanded))return false;
  return true;
 }
 static void CheckMesh(Mesh mesh)
 {
  if(AssetDatabase.GetAssetPath(mesh)!=MeshPath||mesh.subMeshCount!=1||mesh.normals.Length!=mesh.vertexCount||mesh.GetVertexAttributes().Any(a=>a.attribute!=UnityEngine.Rendering.VertexAttribute.Position&&a.attribute!=UnityEngine.Rendering.VertexAttribute.Normal))throw new Exception("Unexpected infield paint mesh format");
 }
 public static string Run(bool apply)
 {
  if(EditorApplication.isPlaying||SceneManager.GetActiveScene().path!="Assets/Scenes/LakeTownScene.unity")throw new Exception("Open Lake Town in Edit Mode");
  var town=UnityEngine.Object.FindAnyObjectByType<LakeTownNightLights>().transform;var details=town.Find("Infield street details");
  var main=town.GetComponentsInChildren<MeshCollider>(true).Single(c=>c.name=="Lake Town Road");
  var road=main.sharedMesh.vertices.Select(main.transform.TransformPoint).ToArray();var roadTriangles=main.sharedMesh.triangles;
  var loop=Enumerable.Range(0,road.Length/2-1).Select(i=>Flat((road[i*2]+road[i*2+1])*.5f)).ToArray();
  var white=details.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.GetComponent<Renderer>().sharedMaterial.name=="Road Ivory").ToArray();
  var yellow=details.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.GetComponent<Renderer>().sharedMaterial.name=="Warm Yellow").ToArray();
  foreach(var filter in white.Concat(yellow))CheckMesh(filter.sharedMesh);
  var plans=new List<Crossing>();
  foreach(var name in new[]{"Market Street","Marina Avenue","Old Town Cross Street"})
  {
   var street=town.GetComponentsInChildren<MeshCollider>(true).Single(c=>c.name==name);var v=street.sharedMesh.vertices;
   var start=Flat(street.transform.TransformPoint((v[0]+v[1])*.5f));var end=Flat(street.transform.TransformPoint((v[v.Length-2]+v[v.Length-1])*.5f));
   if(Mathf.Abs(Vector3.Distance(street.transform.TransformPoint(v[0]),street.transform.TransformPoint(v[1]))-12f)>.001f)throw new Exception("Unexpected side-road width");
   foreach(int side in new[]{0,1})
   {
    var endpoint=side==0?start:end;var inward=(side==0?end-start:start-end).normalized;
    var plan=new Crossing{label=name+(side==0?" start":" end"),inward=inward,across=Vector3.Cross(Vector3.up,inward)};
    bool found=false;
    for(int step=0;step<=600;step++)
    {
     var center=endpoint+inward*(step*.1f);var envelope=Envelope(center,inward,1.6f,5.09f);
     if(!Clear(envelope,loop,road,roadTriangles))continue;
     plan.center=center+Vector3.up*Height;plan.footprint=envelope;plan.clearance=Envelope(center,inward,2.1f,5.09f);found=true;break;
    }
    if(!found)throw new Exception("No clear stem crossing for "+plan.label);
    plan.batch=white.OrderBy(f=>(f.GetComponent<Renderer>().bounds.ClosestPoint(plan.center)-plan.center).sqrMagnitude).ThenBy(f=>(f.GetComponent<Renderer>().bounds.center-plan.center).sqrMagnitude).First();
    if(Vector3.Distance(plan.batch.GetComponent<Renderer>().bounds.ClosestPoint(plan.center),plan.center)>30f)throw new Exception("No nearby existing paint batch");
    for(int stripe=0;stripe<9;stripe++)
    {
     var center=plan.center+plan.across*(-4.8f+stripe*1.2f);var a=plan.across*.29f;var b=inward*1.6f;
     plan.vertices.AddRange(new[]{center-a-b,center-a+b,center+a+b,center+a-b});
    }
    plans.Add(plan);
   }
  }
  var existing=white.SelectMany(f=>f.sharedMesh.vertices.Select(f.transform.TransformPoint)).ToArray();
  int present=plans.Count(p=>p.vertices.All(v=>existing.Any(e=>(e-v).sqrMagnitude<.0000001f)));
  if(present==6)return "All six T-junction zebra crossings already present";
  if(present!=0)throw new Exception("Partial crossing set exists; inspect before adding paint");
  var edits=new Dictionary<Mesh,Update>();int removedDashes=0;
  foreach(var filter in yellow)
  {
   var mesh=filter.sharedMesh;var v=mesh.vertices;var t=mesh.triangles;
   if(v.Length%4!=0||t.Length!=v.Length/4*6)throw new Exception("Unexpected center-dash quad topology");
   var kept=new List<int>();
   for(int q=0;q<v.Length/4;q++)
   {
    int first=q*4;for(int k=0;k<6;k++)if(t[q*6+k]<first||t[q*6+k]>=first+4)throw new Exception("Dash indices cross quad boundary");
    var a=filter.transform.TransformPoint(v[first]);var b=filter.transform.TransformPoint(v[first+1]);var c=filter.transform.TransformPoint(v[first+2]);var d=filter.transform.TransformPoint(v[first+3]);
    bool overlap=plans.Any(p=>TriangleOverlaps(a,b,c,p.clearance)||TriangleOverlaps(a,c,d,p.clearance));
    if(overlap)removedDashes++;else kept.AddRange(t.Skip(q*6).Take(6));
   }
   if(kept.Count!=t.Length)edits.Add(mesh,new Update{mesh=mesh,vertices=v.ToList(),normals=mesh.normals.ToList(),triangles=kept});
  }
  foreach(var plan in plans)
  {
   var mesh=plan.batch.sharedMesh;if(!edits.TryGetValue(mesh,out var update))edits.Add(mesh,update=new Update{mesh=mesh,vertices=mesh.vertices.ToList(),normals=mesh.normals.ToList(),triangles=mesh.triangles.ToList()});
   for(int stripe=0;stripe<9;stripe++)
   {
    int first=update.vertices.Count;update.vertices.AddRange(plan.vertices.Skip(stripe*4).Take(4).Select(plan.batch.transform.InverseTransformPoint));
    update.normals.AddRange(Enumerable.Repeat(plan.batch.transform.InverseTransformDirection(Vector3.up),4));update.triangles.AddRange(new[]{first,first+1,first+2,first,first+2,first+3});
   }
  }
  if(apply)
  {
   Undo.RecordObjects(edits.Keys.Cast<UnityEngine.Object>().ToArray(),"Paint Lake Town T-junction crossings");
   foreach(var edit in edits.Values){edit.mesh.SetVertices(edit.vertices);edit.mesh.SetNormals(edit.normals);edit.mesh.SetTriangles(edit.triangles,0);edit.mesh.RecalculateBounds();EditorUtility.SetDirty(edit.mesh);}
   AssetDatabase.SaveAssets();SceneView.RepaintAll();
  }
  return (apply?"Applied":"Preview")+": six zebra crossings / 54 stripes / 108 added triangles; "+removedDashes+" overlapping center-dash patches cleared; "+edits.Count+" existing meshes updated, no new renderers. "+string.Join("; ",plans.Select(p=>p.label+" "+p.center.ToString("F1")));
 }
}

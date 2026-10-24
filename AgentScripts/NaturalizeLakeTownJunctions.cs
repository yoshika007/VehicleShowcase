using System;using System.Linq;using System.Collections.Generic;using UnityEngine;using UnityEditor;using UnityEngine.SceneManagement;

// Clip only branch-road and branch-sidewalk render meshes to the main-loop joins.
// The original colliders, minimap, main-loop geometry, crossings, and scene remain intact.
// One-time repair: the ownership guard refuses already-modified sidewalk batches.
public static class NaturalizeLakeTownJunctions
{
 struct V
 {
  public Vector3 p,n;public Vector2 uv;public int index;
  public static V Lerp(V a,V b,float t)
  {if(t<.000001f)return a;if(t>.999999f)return b;return new V{p=Vector3.Lerp(a.p,b.p,t),n=Vector3.Lerp(a.n,b.n,t).normalized,uv=Vector2.Lerp(a.uv,b.uv,t),index=-1};}
 }
 class Plan{public Mesh mesh;public List<Vector3> positions,normals;public List<Vector2> uv;public List<int>[] triangles;public int cut,empty,matched;public double removed;}
 static double Cross(Vector3 a,Vector3 b,Vector3 c)=>(double)(b.x-a.x)*(c.z-a.z)-(double)(b.z-a.z)*(c.x-a.x);
 static double Area(List<V> p){double value=0;for(int i=1;i<p.Count-1;i++)value+=Cross(p[0].p,p[i].p,p[i+1].p);return Math.Abs(value)*.5;}
 static bool InTriangle(Vector3 p,Vector3 a,Vector3 b,Vector3 c)=>Cross(a,b,p)>=-.0000001&&Cross(b,c,p)>=-.0000001&&Cross(c,a,p)>=-.0000001;
 static List<Vector3[]> Triangulate(Vector3[] polygon)
 {
  double signed=0;for(int i=0;i<polygon.Length;i++){var a=polygon[i];var b=polygon[(i+1)%polygon.Length];signed+=(double)a.x*b.z-(double)b.x*a.z;}
  var pending=Enumerable.Range(0,polygon.Length).ToList();if(signed<0)pending.Reverse();var output=new List<Vector3[]>();
  while(pending.Count>3)
  {
   bool found=false;
   for(int i=0;i<pending.Count;i++)
   {
    int previous=pending[(i+pending.Count-1)%pending.Count],current=pending[i],next=pending[(i+1)%pending.Count];var a=polygon[previous];var b=polygon[current];var c=polygon[next];
    if(Cross(a,b,c)<=.0000001)continue;
    if(pending.Any(k=>k!=previous&&k!=current&&k!=next&&InTriangle(polygon[k],a,b,c)))continue;
    output.Add(new[]{a,b,c});pending.RemoveAt(i);found=true;break;
   }
   if(!found)throw new Exception("Main-loop join contour could not be triangulated safely");
  }
  output.Add(pending.Select(k=>polygon[k]).ToArray());return output;
 }
 static Vector3[] InnerContour(Mesh mesh,Transform transform)
 {
  var vertices=mesh.vertices;if(vertices.Length%2!=0||vertices.Length<8)throw new Exception("Unexpected loop ribbon");
  int pairs=vertices.Length/2-1;
  if((vertices[0]-vertices[vertices.Length-2]).sqrMagnitude>.000001f||(vertices[1]-vertices[vertices.Length-1]).sqrMagnitude>.000001f)throw new Exception("Loop ribbon is not closed");
  var sides=Enumerable.Range(0,2).Select(side=>Enumerable.Range(0,pairs).Select(i=>transform.TransformPoint(vertices[i*2+side])).ToArray()).ToArray();
  return sides.OrderBy(p=>{double area=0;for(int i=0;i<p.Length;i++)area+=(double)p[i].x*p[(i+1)%p.Length].z-(double)p[(i+1)%p.Length].x*p[i].z;return Math.Abs(area);}).First();
 }
 static List<V> Clip(List<V> polygon,Vector3 a,Vector3 b)
 {
  var result=new List<V>();if(polygon.Count==0)return result;var old=polygon[polygon.Count-1];double oldDistance=Cross(a,b,old.p);bool oldInside=oldDistance>=0;
  foreach(var current in polygon)
  {
   double distance=Cross(a,b,current.p);bool inside=distance>=0;
   if(inside!=oldInside)result.Add(V.Lerp(old,current,(float)(oldDistance/(oldDistance-distance))));
   if(inside)result.Add(current);old=current;oldDistance=distance;oldInside=inside;
  }
  return result;
 }
 static List<V> Intersect(List<V> subject,Vector3[] mask)
 {
  var p=subject;for(int edge=0;edge<3&&p.Count>0;edge++)p=Clip(p,mask[edge],mask[(edge+1)%3]);return p;
 }
 static string Key(Vector3[] points)=>string.Join("/",points.Select(p=>Vector3Int.RoundToInt(p*10000f).ToString()).OrderBy(p=>p));
 static bool BoundsOverlap(List<V> subject,Vector3[] mask)
 {
  float minX=subject.Min(v=>v.p.x),maxX=subject.Max(v=>v.p.x),minZ=subject.Min(v=>v.p.z),maxZ=subject.Max(v=>v.p.z);
  return maxX>=mask.Min(v=>v.x)&&minX<=mask.Max(v=>v.x)&&maxZ>=mask.Min(v=>v.z)&&minZ<=mask.Max(v=>v.z);
 }
 static Plan Prepare(MeshFilter filter,List<Vector3[]> mask,HashSet<string> owned=null)
 {
  var mesh=filter.sharedMesh;var p=mesh.vertices;var n=mesh.normals;var uv=mesh.uv;
  if(filter.GetComponent<Collider>()!=null||n.Length!=p.Length||(uv.Length!=0&&uv.Length!=p.Length)||mesh.GetVertexAttributes().Any(a=>a.attribute!=UnityEngine.Rendering.VertexAttribute.Position&&a.attribute!=UnityEngine.Rendering.VertexAttribute.Normal&&a.attribute!=UnityEngine.Rendering.VertexAttribute.TexCoord0))throw new Exception("Unexpected branch visual attributes");
  var plan=new Plan{mesh=mesh,positions=p.ToList(),normals=n.ToList(),uv=uv.ToList(),triangles=new List<int>[mesh.subMeshCount]};
  for(int sub=0;sub<mesh.subMeshCount;sub++)
  {
   var source=mesh.GetTriangles(sub);var retained=plan.triangles[sub]=new List<int>();
   for(int i=0;i<source.Length;i+=3)
   {
    var original=Enumerable.Range(0,3).Select(k=>new V{p=filter.transform.TransformPoint(p[source[i+k]]),n=n[source[i+k]],uv=uv.Length>0?uv[source[i+k]]:Vector2.zero,index=source[i+k]}).ToList();
    if(owned!=null&&!owned.Contains(Key(original.Select(v=>v.p).ToArray()))){retained.AddRange(new[]{source[i],source[i+1],source[i+2]});continue;}
    plan.matched++;
    double before=Area(original);var pieces=mask.Where(t=>BoundsOverlap(original,t)).Select(t=>Intersect(original,t)).Where(poly=>Area(poly)>.00000001).ToArray();double after=pieces.Sum(Area);
    if(Math.Abs(before-after)<Math.Max(.00001,before*.00002)){retained.AddRange(new[]{source[i],source[i+1],source[i+2]});continue;}
    if(after>before+.001)throw new Exception("Clipping contour produces overlapping road fragments");
    plan.cut++;plan.removed+=before-after;
    foreach(var polygon in pieces)
    {
     var indices=polygon.Select(v=>
     {
      if(v.index>=0)return v.index;int next=plan.positions.Count;plan.positions.Add(filter.transform.InverseTransformPoint(v.p));plan.normals.Add(v.n);if(uv.Length>0)plan.uv.Add(v.uv);return next;
     }).ToArray();
     for(int k=1;k<polygon.Count-1;k++)if(Math.Abs(Cross(polygon[0].p,polygon[k].p,polygon[k+1].p))>.00000002)retained.AddRange(new[]{indices[0],indices[k],indices[k+1]});
    }
   }
  }
  plan.empty=plan.triangles.All(t=>t.Count==0)?1:0;return plan;
 }
 static Plan RestoreOuterLine(Transform town,int pairs)
 {
  var filter=town.GetComponentsInChildren<MeshFilter>(true).Single(f=>f.name=="Road left edge");var mesh=filter.sharedMesh;int count=(pairs+1)*2;
  if(mesh.vertexCount<count||mesh.subMeshCount!=1||mesh.normals.Length!=mesh.vertexCount||filter.GetComponent<Collider>()!=null)throw new Exception("Unexpected outer line mesh");
  var triangles=new List<int>();for(int i=1;i<=pairs;i++){int k=i*2;triangles.AddRange(new[]{k-2,k,k-1,k-1,k,k+1});}
  return new Plan{mesh=mesh,positions=mesh.vertices.Take(count).ToList(),normals=mesh.normals.Take(count).ToList(),uv=new List<Vector2>(),triangles=new[]{triangles}};
 }
 public static string Run(bool apply)
 {
  if(EditorApplication.isPlaying||SceneManager.GetActiveScene().path!="Assets/Scenes/LakeTownScene.unity")throw new Exception("Open Lake Town in Edit Mode");
  var town=UnityEngine.Object.FindAnyObjectByType<LakeTownNightLights>().transform;var all=town.GetComponentsInChildren<MeshFilter>(true);
  var road=all.Single(f=>f.name=="Lake Town Road");var pavement=town.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Town pavement");
  var pavementMesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Environments/LakeTownTrial/Town pavement.asset");
  var roadMask=Triangulate(InnerContour(road.sharedMesh,road.transform));var walkMask=Triangulate(InnerContour(pavementMesh,pavement));
  var names=new[]{"Market Street","Marina Avenue","Old Town Cross Street"};var plans=new List<Plan>();int roadSections=0,walkSections=0,expectedWalks=0,matchedWalks=0;
  var walkKeys=new HashSet<string>();
  foreach(var name in names)foreach(var side in new[]{" west walk"," east walk"})
  {
   var marker=town.GetComponentsInChildren<Transform>(true).Single(t=>t.name==name+side);var source=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Environments/LakeTownTrial/"+name+side+".asset");
   var vertices=source.vertices;var indices=source.triangles;expectedWalks+=indices.Length/3;
   for(int i=0;i<indices.Length;i+=3)walkKeys.Add(Key(Enumerable.Range(0,3).Select(k=>marker.TransformPoint(vertices[indices[i+k]])).ToArray()));
  }
  foreach(var filter in all)
  {
   var renderer=filter.GetComponent<Renderer>();if(renderer==null||!renderer.enabled)continue;
   bool asphalt=names.Any(name=>filter.name.StartsWith(name+" ("));bool walk=renderer.sharedMaterial.name=="Promenade Stone"&&AssetDatabase.GetAssetPath(filter.sharedMesh)=="Assets/Environments/LakeTownTrial/Local Road Lighting Meshes.asset";
   if(!asphalt&&!walk)continue;
   var plan=Prepare(filter,asphalt?roadMask:walkMask,asphalt?null:walkKeys);if(walk)matchedWalks+=plan.matched;if(plan.cut==0)continue;
   plans.Add(plan);if(asphalt)roadSections++;else walkSections++;
  }
  if(matchedWalks!=expectedWalks)throw new Exception("Cannot isolate every original branch sidewalk triangle: "+matchedWalks+" / "+expectedWalks);
  var outer=RestoreOuterLine(town,road.sharedMesh.vertexCount/2-1);bool restore=!outer.mesh.triangles.SequenceEqual(outer.triangles[0]);if(restore)plans.Add(outer);
  if(plans.Count==0)return "All branch joins already follow the main-loop boundaries";
  if(plans.Select(p=>p.mesh).Distinct().Count()!=plans.Count)throw new Exception("Shared visual mesh targeted twice");
  if(apply)
  {
   Undo.RecordObjects(plans.Select(p=>(UnityEngine.Object)p.mesh).ToArray(),"Naturalize Lake Town road joins");
   foreach(var plan in plans)
   {
    // Clear old indices before shrinking an asset such as the restored outer line.
    plan.mesh.Clear();plan.mesh.SetVertices(plan.positions);plan.mesh.SetNormals(plan.normals);if(plan.uv.Count>0)plan.mesh.SetUVs(0,plan.uv);
    plan.mesh.subMeshCount=plan.triangles.Length;
    for(int sub=0;sub<plan.triangles.Length;sub++)plan.mesh.SetTriangles(plan.triangles[sub],sub);
    plan.mesh.RecalculateBounds();EditorUtility.SetDirty(plan.mesh);
   }
   AssetDatabase.SaveAssets();SceneView.RepaintAll();
  }
  return (apply?"Applied":"Preview")+": "+roadSections+" asphalt render sections, "+walkSections+" sidewalk render sections trimmed; "+plans.Sum(p=>p.removed).ToString("F2")+" m2 of overlap/excess removed; outer white line "+(restore?"restored":"already continuous")+". Main road, colliders, minimap, crossings and scene objects unchanged.";
 }
}

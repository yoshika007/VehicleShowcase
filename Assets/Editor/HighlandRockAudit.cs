using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class HighlandRockAudit
{
    struct Rock { public Vector3 center; public Vector3 extents; public int index; }
    static float Radius(Rock a) => Mathf.Max(a.extents.x,a.extents.z);
    static float Dist(Vector3 a,Vector3 b) => new Vector2(a.x-b.x,a.z-b.z).magnitude;
    static float Ground(MeshCollider terrain, Vector3 p) {return terrain.Raycast(new Ray(new Vector3(p.x,300,p.z),Vector3.down),out var h,600)?h.point.y:float.NaN;}
    static Rock BoundsFor(Vector3[] v,int start,int count,int index) {var b=new Bounds(v[start],Vector3.zero);for(int i=1;i<count;i++)b.Encapsulate(v[start+i]);return new Rock{center=b.center,extents=b.extents,index=index};}
    public static string Run(bool apply)
    {
        var scene=SceneManager.GetActiveScene();if(scene.path!="Assets/Scenes/HighlandClimbScene.unity"||scene.isDirty||EditorApplication.isPlaying)throw new Exception("Open saved Highland Climb in Edit Mode");
        var root=scene.GetRootGameObjects().First(g=>g.name=="Highland Climb Trail").transform;var terrain=root.Find("Desert terrain").GetComponent<MeshCollider>();
        var rocksMesh=root.Find("Dry rock outcrops").GetComponent<MeshFilter>().sharedMesh;var verts=rocksMesh.vertices;int size=Resources.GetBuiltinResource<Mesh>("Sphere.fbx").vertexCount;if(verts.Length%size!=0)throw new Exception("Rock mesh format changed");
        var stones=new List<Rock>();for(int i=0;i<verts.Length/size;i++)stones.Add(BoundsFor(verts,i*size,size,i));
        var bushVerts=root.Find("Dry scrub").GetComponent<MeshFilter>().sharedMesh.vertices;int group=Resources.GetBuiltinResource<Mesh>("Cube.fbx").vertexCount*3;if(bushVerts.Length%group!=0)throw new Exception("Scrub mesh format changed");var shrubs=new List<Rock>();for(int i=0;i<bushVerts.Length/group;i++)shrubs.Add(BoundsFor(bushVerts,i*group,group,i));
        var hill=root.Find("Distant desert hills");var hillCollider=hill.gameObject.AddComponent<MeshCollider>();hillCollider.sharedMesh=hill.GetComponent<MeshFilter>().sharedMesh;
        var map=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Environments/HighlandClimb/Trail map 2.asset").vertices;
        bool overlap(Rock r,List<Rock> accepted) {foreach(var x in accepted)if(Dist(r.center,x.center)<(Radius(r)+Radius(x))*.90f)return true;foreach(var x in shrubs)if(Dist(r.center,x.center)<Radius(r)+Radius(x)+.35f)return true;return false;}
        bool buried(Rock r) {float y=Ground(terrain,r.center);if(float.IsNaN(y)||r.center.y+r.extents.y<y+.1f||r.center.y-r.extents.y>y+.5f)return true;var ray=new Ray(new Vector3(r.center.x,300,r.center.z),Vector3.down);return hillCollider.Raycast(ray,out var h,600)&&h.point.y>r.center.y+.1f;}
        var bad=new List<Rock>();var accepted=new List<Rock>();int overlaps=0,terrainIssues=0;foreach(var r in stones){bool o=overlap(r,accepted),b=buried(r);if(o)overlaps++;if(b)terrainIssues++;if(o||b)bad.Add(r);else accepted.Add(r);}
        int moved=0,failed=0;var rng=new System.Random(4257);
        if(apply)foreach(var original in bad){bool found=false;for(int attempt=0;attempt<400;attempt++){float angle=(float)rng.NextDouble()*Mathf.PI*2;float distance=8f+(float)rng.NextDouble()*Mathf.Min(160,12+attempt*.8f);var target=attempt<100 ? original.center+new Vector3(Mathf.Cos(angle)*distance,0,Mathf.Sin(angle)*distance) : new Vector3(Mathf.Cos(angle)*(280f+(float)rng.NextDouble()*120f),0,Mathf.Sin(angle)*(280f+(float)rng.NextDouble()*120f));if(new Vector2(target.x,target.z).magnitude>405f)continue;bool near=false;for(int j=0;j<map.Length;j+=2){var p=(map[j]+map[j+1])*.5f;if(Dist(target,p)<30f+Radius(original)){near=true;break;}}if(near)continue;float ground=Ground(terrain,target);if(float.IsNaN(ground))continue;var r=original;r.center=new Vector3(target.x,ground+original.extents.y*.8f,target.z);if(overlap(r,accepted)||buried(r))continue;var delta=r.center-original.center;for(int k=0;k<size;k++)verts[original.index*size+k]+=delta;accepted.Add(r);moved++;found=true;break;}if(!found){accepted.Add(original);failed++;}}
        UnityEngine.Object.DestroyImmediate(hillCollider);if(apply&&moved>0){rocksMesh.vertices=verts;rocksMesh.RecalculateBounds();EditorUtility.SetDirty(rocksMesh);AssetDatabase.SaveAssets();}
        return "rocks="+stones.Count+" overlapping="+overlaps+" terrain_or_hill="+terrainIssues+" flagged="+bad.Count+" moved="+moved+" unresolved="+failed;
    }
}

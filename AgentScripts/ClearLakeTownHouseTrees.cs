using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

public static class ClearLakeTownHouseTrees
{
    static Bounds Crown(Transform tree, Vector3 offset)
        => new Bounds(tree.position+offset+Vector3.up*tree.localScale.y*.5f,new Vector3(tree.localScale.x*2,tree.localScale.y,tree.localScale.z*2));
    static Bounds House(BoxCollider house)
    {
        var bounds=house.bounds;
        bounds.center+=Vector3.up*1.5f;
        bounds.Expand(new Vector3(1,3,1)); // Include gabled roof overhang.
        return bounds;
    }
    static int[] FindVertices(MeshFilter batch, Mesh source, Transform part)
    {
        var expected=source.vertices.Select(v=>batch.transform.InverseTransformPoint(part.TransformPoint(v))).ToArray();
        var vertices=batch.sharedMesh.vertices;
        var indices=Enumerable.Range(0,vertices.Length).Where(i=>expected.Any(v=>(v-vertices[i]).sqrMagnitude<.0000001f)).ToArray();
        if(indices.Length!=source.vertexCount)throw new Exception("Could not isolate complete "+part.name+" geometry: "+indices.Length+" expected "+source.vertexCount);
        return indices;
    }
    public static string Run()
    {
        if(EditorApplication.isPlaying||SceneManager.GetActiveScene().isDirty)throw new Exception("Clean Edit Mode required");
        var scene=SceneManager.GetActiveScene();if(scene.path!="Assets/Scenes/LakeTownScene.unity")throw new Exception("Open Lake Town first");
        var town=Object.FindAnyObjectByType<LakeTownNightLights>().transform;
        var trees=town.GetComponentsInChildren<Transform>().Where(t=>t.name=="Pine canopy").ToArray();
        var houses=town.GetComponentsInChildren<BoxCollider>().Where(c=>c.name.StartsWith("Town house ")).Select(House).ToArray();
        var roads=town.GetComponentsInChildren<MeshCollider>().Where(c=>c.GetComponent<TestDriveAudioSurface>()!=null).ToArray();
        var ground=town.GetComponentsInChildren<MeshCollider>().Single(c=>c.name=="Town ground");
        var batches=town.GetComponentsInChildren<MeshFilter>().Where(f=>f.name.StartsWith("Scenery batch ")).ToArray();
        var crownBatch=batches.Single(f=>f.GetComponent<Renderer>().sharedMaterial.name=="Pine Green");
        var trunkBatch=batches.Single(f=>f.GetComponent<Renderer>().sharedMaterial.name=="Timber");
        var cone=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Environments/LakeTownTrial/Pine Cone.asset");
        var cylinder=GameObject.CreatePrimitive(PrimitiveType.Cylinder);var trunkMesh=cylinder.GetComponent<MeshFilter>().sharedMesh;Object.DestroyImmediate(cylinder);
        var moves=new List<Tuple<Transform,Transform,Vector3,int[],int[]>>();
        var trunks=town.GetComponentsInChildren<Transform>().Where(t=>t.name=="Pine trunk").ToArray();
        foreach(var tree in trees.Where(t=>houses.Any(h=>h.Intersects(Crown(t,Vector3.zero)))))
        {
            Vector3 offset=Vector3.zero;bool found=false;
            for(float radius=2;radius<=60&&!found;radius+=2)for(int angle=0;angle<32&&!found;angle++)
            {
                float a=angle*Mathf.PI/16;var candidate=new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius);
                var bounds=Crown(tree,candidate);bounds.Expand(new Vector3(2.5f,0,2.5f));
                if(houses.Any(h=>h.Intersects(bounds)))continue;
                var point=tree.position+candidate;point.y=20;
                if(!ground.Raycast(new Ray(point,Vector3.down),out var hit,30))continue;
                bool onRoad=false;
                for(int sample=0;sample<9;sample++)
                {
                    var test=point;
                    if(sample>0){float a2=(sample-1)*Mathf.PI/4;test+=new Vector3(Mathf.Cos(a2),0,Mathf.Sin(a2))*(tree.localScale.x+1);}
                    if(roads.Any(r=>r.Raycast(new Ray(test,Vector3.down),out var roadHit,30))){onRoad=true;break;}
                }
                if(onRoad)continue;
                if(trees.Any(other=>other!=tree&&new Vector2(other.position.x-point.x,other.position.z-point.z).magnitude<2.5f))continue;
                if(moves.Any(m=>Vector3.Distance(m.Item1.position+m.Item3,tree.position+candidate)<tree.localScale.x+m.Item1.localScale.x))continue;
                offset=candidate;found=true;
            }
            if(!found)throw new Exception("No safe placement for tree "+tree.position);
            var trunk=trunks.Single(t=>Mathf.Abs(t.position.x-tree.position.x)<.001f&&Mathf.Abs(t.position.z-tree.position.z)<.001f);
            moves.Add(Tuple.Create(tree,trunk,offset,FindVertices(crownBatch,cone,tree),FindVertices(trunkBatch,trunkMesh,trunk)));
        }
        var crownVertices=crownBatch.sharedMesh.vertices;var trunkVertices=trunkBatch.sharedMesh.vertices;
        foreach(var move in moves)
        {
            foreach(int i in move.Item4)crownVertices[i]+=crownBatch.transform.InverseTransformVector(move.Item3);
            foreach(int i in move.Item5)trunkVertices[i]+=trunkBatch.transform.InverseTransformVector(move.Item3);
            move.Item1.position+=move.Item3;move.Item2.position+=move.Item3;
        }
        crownBatch.sharedMesh.vertices=crownVertices;crownBatch.sharedMesh.RecalculateBounds();EditorUtility.SetDirty(crownBatch.sharedMesh);
        trunkBatch.sharedMesh.vertices=trunkVertices;trunkBatch.sharedMesh.RecalculateBounds();EditorUtility.SetDirty(trunkBatch.sharedMesh);
        if(trees.Any(t=>houses.Any(h=>h.Intersects(Crown(t,Vector3.zero)))))throw new Exception("A house/tree overlap remains");
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        return "Moved "+moves.Count+" complete trees; checked "+trees.Length+" trees against "+houses.Length+" houses including roofs. Zero overlaps remain.\n"+string.Join("\n",moves.Select(m=>m.Item1.position+" moved by "+m.Item3));
    }
}

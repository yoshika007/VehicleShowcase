using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
public static class AddRoadsideLights {
 const string Folder="Assets/Environments/CircuitFacilities/RoadsideLighting";
 static Vector3 Point(float t,float r){float arc=Mathf.PI*25,d=Mathf.Repeat(t,1)*(140+2*arc);if(d<70)return new Vector3(-35+d,0,-r);d-=70;if(d<arc){float a=-Mathf.PI/2+d/25;return new Vector3(35+Mathf.Cos(a)*r,0,Mathf.Sin(a)*r);}d-=arc;if(d<70)return new Vector3(35-d,0,r);d-=70;float b=Mathf.PI/2+d/25;return new Vector3(-35+Mathf.Cos(b)*r,0,Mathf.Sin(b)*r);}
 static void Box(List<CombineInstance> list,Mesh cube,Vector3 p,Vector3 size,Quaternion rotation){list.Add(new CombineInstance{mesh=cube,transform=Matrix4x4.TRS(p,rotation,size)});}
 static Mesh MeshAsset(string name,List<CombineInstance> pieces){var m=new Mesh{name=name};m.CombineMeshes(pieces.ToArray(),true,true);AssetDatabase.CreateAsset(m,Folder+"/"+name+".asset");return m;}
 static Renderer MeshObject(string name,Transform root,Mesh mesh,Material material){var g=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));g.transform.SetParent(root,false);g.GetComponent<MeshFilter>().sharedMesh=mesh;var r=g.GetComponent<MeshRenderer>();r.sharedMaterial=material;return r;}
 public static string Main(){
 if(EditorApplication.isPlaying)throw new Exception("Edit Mode required");var original=SceneManager.GetActiveScene();var track=EditorSceneManager.OpenScene("Assets/Scenes/TestTrackScene.unity",OpenSceneMode.Additive);
 try{SceneManager.SetActiveScene(track);var objects=track.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();if(objects.Any(t=>t.name=="Night Roadside Lighting"))throw new Exception("Already configured");var road=objects.Single(t=>t.name=="Asphalt — 12m wide");var markerStates=objects.Where(t=>t.name.Contains("Trail edge marker")).ToDictionary(t=>t.gameObject,t=>t.gameObject.activeSelf);
 AssetDatabase.CreateFolder("Assets/Environments/CircuitFacilities","RoadsideLighting");
 var metal=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="Roadside Lamp Metal"};metal.SetColor("_BaseColor",new Color(.10f,.14f,.17f));metal.SetFloat("_Metallic",.35f);metal.SetFloat("_Smoothness",.4f);AssetDatabase.CreateAsset(metal,Folder+"/Lamp Metal.mat");
 var glass=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="Roadside Lamp Lens"};glass.SetColor("_BaseColor",new Color(.8f,.78f,.65f));glass.SetColor("_EmissionColor",Color.white);glass.EnableKeyword("_EMISSION");glass.globalIlluminationFlags=MaterialGlobalIlluminationFlags.RealtimeEmissive;AssetDatabase.CreateAsset(glass,Folder+"/Lamp Lens.mat");
 var root=new GameObject("Night Roadside Lighting").transform;var primitive=GameObject.CreatePrimitive(PrimitiveType.Cube);var cube=primitive.GetComponent<MeshFilter>().sharedMesh;var poles=new List<CombineInstance>();var lenses=new List<CombineInstance>();var lights=new List<Light>();
 for(int i=0;i<28;i++){
 float t=(i+.25f)/28;var p=road.TransformPoint(Point(t,36.8f));p.y=0;
 // Keep the east access opening clear.
 if(p.x>75&&Mathf.Abs(p.z)<11)continue;
 var center=road.TransformPoint(Point(t,25));center.y=.03f;var direction=center-p;direction.y=0;direction.Normalize();var q=Quaternion.LookRotation(direction);
 var head=p+direction*3.5f+Vector3.up*8.1f;
 Box(poles,cube,p+Vector3.up*.1f,new Vector3(.55f,.2f,.55f),q);Box(poles,cube,p+Vector3.up*4,new Vector3(.18f,8,.18f),q);Box(poles,cube,p+direction*1.75f+Vector3.up*8,new Vector3(.18f,.18f,3.7f),q);Box(poles,cube,head,new Vector3(.65f,.22f,1.5f),q);Box(lenses,cube,head-Vector3.up*.13f,new Vector3(.53f,.04f,1.25f),q);
 var go=new GameObject("Road Lamp "+lights.Count.ToString("D2"),typeof(Light));go.transform.SetParent(root,false);go.transform.position=head-Vector3.up*.18f;go.transform.rotation=Quaternion.LookRotation(center-go.transform.position);var l=go.GetComponent<Light>();l.type=LightType.Spot;l.color=new Color(1f,.88f,.66f);l.intensity=250;l.range=26;l.spotAngle=90;l.innerSpotAngle=58;l.shadows=LightShadows.None;l.enabled=false;lights.Add(l);
 }
 Object.DestroyImmediate(primitive);MeshObject("Lamp Poles",root,MeshAsset("Circuit Lamp Poles",poles),metal);var lens=MeshObject("Lamp Lenses",root,MeshAsset("Circuit Lamp Lenses",lenses),glass);lens.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
 var component=root.gameObject.AddComponent<NightRoadsideLights>();var so=new SerializedObject(component);var a=so.FindProperty("lamps");a.arraySize=lights.Count;for(int i=0;i<lights.Count;i++)a.GetArrayElementAtIndex(i).objectReferenceValue=lights[i];var b=so.FindProperty("lenses");b.arraySize=1;b.GetArrayElementAtIndex(0).objectReferenceValue=lens;so.ApplyModifiedPropertiesWithoutUndo();
 if(objects.Where(t=>markerStates.ContainsKey(t.gameObject)).Any(t=>markerStates[t.gameObject]!=t.gameObject.activeSelf))throw new Exception("Marker state changed");EditorSceneManager.MarkSceneDirty(track);EditorSceneManager.SaveScene(track);AssetDatabase.SaveAssets();return lights.Count+" lamps; no added colliders; existing marker states retained.";
 }finally{EditorSceneManager.CloseScene(track,true);SceneManager.SetActiveScene(original);}
 }
}

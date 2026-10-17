using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
public static class BuildTestTrack {
 static Transform root;
 const string Dir="Assets/Environments/TestTrack";
 static Material Mat(string name, Color color) { var m=new Material(Shader.Find("Universal Render Pipeline/Lit")); m.name=name; m.color=color; m.SetFloat("_Smoothness",0.12f); AssetDatabase.CreateAsset(m,Dir+"/"+name+".mat"); return m; }
 static GameObject Box(string name, Vector3 p, Vector3 s, Material m, Transform parent=null) {var g=GameObject.CreatePrimitive(PrimitiveType.Cube); g.name=name; g.transform.SetParent(parent ? parent : root); g.transform.position=p; g.transform.localScale=s; g.GetComponent<Renderer>().sharedMaterial=m; return g; }
 static Vector3 Point(float t,float radius) { // Two 70m straights, joined by semicircles.
 float arc=Mathf.PI*25, length=140+2*arc, d=Mathf.Repeat(t,1)*length;
 if(d<70) return new Vector3(-35+d,0,-radius);
 d-=70; if(d<arc) {float a=-Mathf.PI/2+d/25; return new Vector3(35+Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius);}
 d-=arc; if(d<70) return new Vector3(35-d,0,radius);
 d-=70; float b=Mathf.PI/2+d/25; return new Vector3(-35+Mathf.Cos(b)*radius,0,Mathf.Sin(b)*radius);
 }
 static void Ribbon(string name,float inner,float outer,float height,Material mat,bool collider) {int n=256;var v=new Vector3[(n+1)*2];var uv=new Vector2[v.Length];var tris=new int[n*6];for(int i=0;i<=n;i++){float t=i/(float)n;v[2*i]=Point(t,inner)+Vector3.up*height;v[2*i+1]=Point(t,outer)+Vector3.up*height;uv[2*i]=new Vector2(0,t*100);uv[2*i+1]=new Vector2(1,t*100);if(i<n){int k=i*6,j=i*2;tris[k]=j;tris[k+1]=j+2;tris[k+2]=j+1;tris[k+3]=j+1;tris[k+4]=j+2;tris[k+5]=j+3;}}
 var mesh=new Mesh{name=name};mesh.vertices=v;mesh.uv=uv;mesh.triangles=tris;mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,Dir+"/"+name+".asset");var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root);go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=mat;if(collider)go.AddComponent<MeshCollider>().sharedMesh=mesh;
 }
 public static string Main() {
 if(AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/TestTrackScene.unity"))throw new System.Exception("Scene already exists; refusing overwrite.");
 if(!AssetDatabase.IsValidFolder(Dir))AssetDatabase.CreateFolder("Assets/Environments","TestTrack");
 var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);root=new GameObject("Test Track — Oval Blockout").transform;
 var asphalt=Mat("Track_Asphalt",new Color(.105f,.12f,.14f));var grass=Mat("Track_Grass",new Color(.26f,.37f,.23f));var white=Mat("Track_White",new Color(.9f,.91f,.86f));var red=Mat("Track_Red",new Color(.72f,.13f,.09f));var dark=Mat("Track_Dark",new Color(.045f,.055f,.065f));var concrete=Mat("Track_Concrete",new Color(.48f,.51f,.5f));var sand=Mat("Track_Runoff",new Color(.55f,.48f,.33f));
 Box("Ground",new Vector3(0,-.3f,0),new Vector3(180,.5f,120),grass);
 Ribbon("Runoff",17,33,0,sand,false);Ribbon("Asphalt — 12m wide",19,31,.02f,asphalt,true);
 Ribbon("Inside edge line",19.35f,19.52f,.035f,white,false);Ribbon("Outside edge line",30.48f,30.65f,.035f,white,false);
 var curbs=new GameObject("Red and white curbs").transform;curbs.SetParent(root);
 for(int i=0;i<160;i++){float t=i/160f;foreach(float r in new[]{18.6f,31.4f}){var p=Point(t,r);var tangent=Point(t+.0002f,r)-p;var g=Box("Curb "+i,p+Vector3.up*.07f,new Vector3(.8f,.14f,1.94f),i%2==0?red:white,curbs);g.transform.rotation=Quaternion.LookRotation(tangent);}}
 var barriers=new GameObject("Outer safety barriers").transform;barriers.SetParent(root);
 for(int i=0;i<112;i++){float t=i/112f;var p=Point(t,35);var g=Box("Barrier "+i,p+Vector3.up*.65f,new Vector3(.5f,1.3f,2.8f),i%4==0?red:concrete,barriers);g.transform.rotation=Quaternion.LookRotation(Point(t+.0002f,35)-p);}
 var start=new GameObject("Start finish markings").transform;start.SetParent(root);
 for(int x=0;x<2;x++)for(int z=0;z<12;z++)Box("Checker",new Vector3(-18+x*.7f,.045f,-30.5f+z),new Vector3(.7f,.025f,1),((x+z)%2==0)?white:dark,start);
 for(int i=0;i<5;i++) {Box("Grid slot "+(i+1),new Vector3(-24-i*2,.045f,-27+(i%2)*4),new Vector3(1,.025f,.12f),white,start);}
 var spawn=new GameObject("VehicleSpawn");spawn.transform.SetParent(root);spawn.transform.position=new Vector3(-25,.15f,-25);spawn.transform.rotation=Quaternion.Euler(0,90,0);
 var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Vehicles/SportsCar_Showcase.prefab");var car=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);car.name="Sports Car — scale reference (parked)";car.transform.SetParent(root);car.transform.SetPositionAndRotation(spawn.transform.position,spawn.transform.rotation);foreach(var probe in car.GetComponentsInChildren<ReflectionProbe>())probe.enabled=false;
 var light=new GameObject("Sun",typeof(Light));light.transform.SetParent(root);light.transform.rotation=Quaternion.Euler(48,-35,0);light.GetComponent<Light>().type=LightType.Directional;light.GetComponent<Light>().intensity=1.8f;light.GetComponent<Light>().shadows=LightShadows.Soft;
 RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.58f,.65f,.72f);RenderSettings.fog=false;
 var camera=new GameObject("Track Overview Camera",typeof(Camera),typeof(AudioListener));camera.tag="MainCamera";camera.transform.SetParent(root);camera.transform.position=new Vector3(-100,105,-125);camera.transform.LookAt(Vector3.zero);var cam=camera.GetComponent<Camera>();cam.fieldOfView=48;cam.farClipPlane=500;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.55f,.7f,.79f);
 EditorSceneManager.SaveScene(scene,"Assets/Scenes/TestTrackScene.unity");AssetDatabase.SaveAssets();
 // Keep the unsaved showroom loaded but hidden; the track can be previewed alone.
 foreach(var other in new[]{"Assets/Scenes/ShowcaseScene.unity","Assets/Scenes/TitleScene.unity"}) {var s=SceneManager.GetSceneByPath(other);if(s.IsValid()&&s.isLoaded)foreach(var g in s.GetRootGameObjects())SceneVisibilityManager.instance.Hide(g,true);}
 Selection.activeGameObject=root.gameObject; if(SceneView.lastActiveSceneView)SceneView.lastActiveSceneView.LookAt(Vector3.zero,Quaternion.Euler(55,0,0),100);
 return "Saved TestTrackScene: 12m wide oval, 70m straights, 25m centerline bend radius; original scenes remain loaded.";
 }
}

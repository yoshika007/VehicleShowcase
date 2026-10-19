using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using TMPro;
using Object = UnityEngine.Object;

// Run once on a clean TestTrackScene. Uses Editor APIs for all authored scene/assets.
public static class AddMudWaterTrial
{
    const string Dir = "Assets/Environments/OffroadCourse/WetTrial";
    const int Samples = 200;
    static Transform root;
    static Material dirt, mud, water, bank, timber, sign, gold;
    static readonly List<MeshFilter> roads = new List<MeshFilter>();
    public static Vector3 Point(float u)
    {
        return new Vector3(155 + 80 * u, .12f, -30 - 40 * Mathf.Pow(Mathf.Sin(Mathf.PI * u), 2));
    }
    static float ExistingGround(Vector3 p)
    {
        float y = -.05f;
        foreach (var hit in Physics.RaycastAll(p + Vector3.up * 35, Vector3.down, 70))
        {
            if (root != null && hit.transform.IsChildOf(root)) continue;
            if (hit.collider.GetComponent<TestDriveSurface>() != null || hit.collider.name == "Offroad ground")
                y = Mathf.Max(y, hit.point.y);
        }
        return y;
    }
    static Material Material(string name, Color tint, float smoothness = .05f)
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
        m.SetColor("_BaseColor", tint); m.SetFloat("_Smoothness", smoothness);
        AssetDatabase.CreateAsset(m, Dir + "/" + name + ".mat"); return m;
    }
    static void AddMudTexture(Material material)
    {
        const int size=256;
        var texture=new Texture2D(size,size,TextureFormat.RGB24,true){name="Wet mud tire ruts",wrapMode=TextureWrapMode.Repeat,anisoLevel=2};
        var pixels=new Color[size*size];
        for(int y=0;y<size;y++)for(int x=0;x<size;x++)
        {
            float u=x/(float)size,v=y/(float)size;
            float grain=Mathf.PerlinNoise(u*39+7,v*32+11);
            float patch=Mathf.PerlinNoise(u*8+3,v*6+2);
            Color color=new Color(.21f,.135f,.075f)*(.76f+patch*.32f+grain*.18f);
            float rut=Mathf.Max(Mathf.Exp(-Mathf.Pow((u-.425f)/.025f,4)),Mathf.Exp(-Mathf.Pow((u-.575f)/.025f,4)));
            float tread=.7f+.3f*Mathf.Pow(Mathf.Sin(v*48*Mathf.PI+u*13),2);
            pixels[y*size+x]=color*(1-.48f*rut*tread);
        }
        texture.SetPixels(pixels);texture.Apply();AssetDatabase.CreateAsset(texture,Dir+"/Wet mud tire ruts.asset");
        material.SetColor("_BaseColor",Color.white);material.SetTexture("_BaseMap",texture);material.SetTextureScale("_BaseMap",new Vector2(1f/3,.3f));EditorUtility.SetDirty(material);
    }
    static MeshFilter Ribbon(string name, float from, float to, float width, Material material,
        TestDriveSurface.Kind kind, bool collider, float waterLevel = -1)
    {
        int count = Mathf.Max(2, Mathf.RoundToInt((to - from) * Samples));
        var vertices = new Vector3[(count + 1) * 2]; var uv = new Vector2[vertices.Length];
        var triangles = new int[count * 6]; float distance = 0;
        for (int i = 0; i <= count; i++)
        {
            float u = Mathf.Lerp(from, to, i / (float)count); var p = Point(u);
            var tangent = Point(Mathf.Min(1, u + .001f)) - Point(Mathf.Max(0, u - .001f));
            var side = Vector3.Cross(Vector3.up, tangent).normalized;
            if (i > 0) distance += Vector3.Distance(p, Point(Mathf.Lerp(from, to, (i - 1f) / count)));
            for (int j = 0; j < 2; j++)
            {
                var v = p + side * width * (j == 0 ? -.5f : .5f);
                if (waterLevel >= 0) v.y = waterLevel;
                else if (kind == TestDriveSurface.Kind.Dry)
                {
                    float lift = .04f + .08f * Mathf.SmoothStep(0,1,Mathf.Min(u/.06f,(1-u)/.06f));
                    v.y = Mathf.Max(lift, ExistingGround(v) + .005f);
                }
                vertices[i * 2 + j] = v; uv[i * 2 + j] = new Vector2(j * width / 4, distance / 4);
            }
            if (i == count) continue;
            int k = i * 6, n = i * 2;
            // With left/right vertex order these triangles face upward.
            triangles[k] = n; triangles[k + 1] = n + 2; triangles[k + 2] = n + 1;
            triangles[k + 3] = n + 1; triangles[k + 4] = n + 2; triangles[k + 5] = n + 3;
        }
        var mesh = new Mesh { name = name }; mesh.vertices = vertices; mesh.uv = uv;
        mesh.triangles = triangles; mesh.RecalculateNormals(); mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh, Dir + "/" + name + ".asset");
        var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        go.transform.SetParent(root, false); go.GetComponent<MeshFilter>().sharedMesh = mesh;
        go.GetComponent<MeshRenderer>().sharedMaterial = material;
        if (collider)
        {
            go.AddComponent<MeshCollider>().sharedMesh = mesh;
            var surface = go.AddComponent<TestDriveSurface>(); var so = new SerializedObject(surface);
            so.FindProperty("kind").enumValueIndex = (int)kind;
            so.FindProperty("gripMultiplier").floatValue = kind == TestDriveSurface.Kind.Mud ? .58f : kind == TestDriveSurface.Kind.Water ? .78f : .82f;
            so.FindProperty("resistanceAcceleration").floatValue = kind == TestDriveSurface.Kind.Mud ? .55f : kind == TestDriveSurface.Kind.Water ? .12f : 0;
            so.FindProperty("velocityResistance").floatValue = kind == TestDriveSurface.Kind.Mud ? .5f : kind == TestDriveSurface.Kind.Water ? .22f : 0;
            so.FindProperty("dustColor").colorValue = kind == TestDriveSurface.Kind.Water ? new Color(.68f,.88f,.9f,.8f) : kind == TestDriveSurface.Kind.Mud ? new Color(.25f,.14f,.065f,.85f) : new Color(.64f,.48f,.31f,.28f);
            so.ApplyModifiedPropertiesWithoutUndo();
            // Reuse the existing subdued dirt recording; no new licensed audio required.
            var audio = go.AddComponent<TestDriveAudioSurface>(); var audioSo = new SerializedObject(audio);
            audioSo.FindProperty("kind").enumValueIndex = (int)TestDriveAudioSurface.Kind.Dirt; audioSo.ApplyModifiedPropertiesWithoutUndo();
            roads.Add(go.GetComponent<MeshFilter>());
        }
        return go.GetComponent<MeshFilter>();
    }
    static GameObject Box(string name, Vector3 position, Vector3 size, Material material)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name;
        go.transform.SetParent(root, false); go.transform.position = position; go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = material; Object.DestroyImmediate(go.GetComponent<Collider>()); return go;
    }
    static void Board(string name, string title, string subtitle, Vector3 position, float yaw)
    {
        float y = ExistingGround(position); position.y = y;
        var holder = new GameObject(name).transform; holder.SetParent(root, false); holder.SetPositionAndRotation(position, Quaternion.Euler(0,yaw,0));
        for (int i = -1; i <= 1; i += 2)
        {
            var post = Box(name + " post", Vector3.zero, new Vector3(.15f,2.3f,.15f),timber);
            post.transform.SetParent(holder,false); post.transform.localPosition = new Vector3(i*1.8f,1.1f,0);
        }
        var panel = Box(name + " board", Vector3.zero, new Vector3(4.5f,1.45f,.12f),sign);
        panel.transform.SetParent(holder,false); panel.transform.localPosition = new Vector3(0,2.0f,0);
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Nunito/Nunito-Bold SDF.asset");
        for (int side = -1; side <= 1; side += 2)
        {
            var label = new GameObject(name + " label",typeof(TextMeshPro)); label.transform.SetParent(holder,false);
            label.transform.localPosition = new Vector3(0,2f,side*.071f); label.transform.localRotation = Quaternion.Euler(0,side==1?180:0,0);
            var text = label.GetComponent<TextMeshPro>(); text.font=font; text.text=title+"\n<size=55%>"+subtitle+"</size>";
            text.fontSize=4.5f; text.alignment=TextAlignmentOptions.Center; text.color=new Color(.98f,.78f,.3f);
            text.rectTransform.sizeDelta=new Vector2(4.2f,1.25f); text.textWrappingMode=TextWrappingModes.NoWrap;
        }
    }
    static void CreateBanks()
    {
        foreach (float direction in new[] { -1f, 1f })
        {
            const int rows = 72, across = 4;
            var v = new Vector3[(rows + 1) * across]; var uv = new Vector2[v.Length];
            var t = new List<int>();
            for (int i=0; i<=rows; i++)
            {
                float u=Mathf.Lerp(.20f,.80f,i/(float)rows);var p=Point(u);
                var tangent=(Point(u+.001f)-Point(u-.001f)).normalized;
                var side=Vector3.Cross(Vector3.up,tangent);
                float fade=Mathf.SmoothStep(0,1,Mathf.Min((u-.20f)/.06f,(.80f-u)/.06f));
                for(int j=0;j<across;j++)
                {
                    float offset=new[]{6f,7.3f,9f,11f}[j];var q=p+side*direction*offset;
                    float rise=new[]{0f,.55f,.23f,0f}[j];q.y=Mathf.Max(.02f,ExistingGround(q)+.015f)+rise*fade;
                    v[i*across+j]=q;uv[i*across+j]=new Vector2(q.x/4,q.z/4);
                    if(i<rows&&j<across-1){int k=i*across+j;t.AddRange(new[]{k,k+across,k+1,k+1,k+across,k+across+1});}
                }
            }
            // Orient both shoulders upward independently of which side they occupy.
            var normals=Vector3.Cross(v[t[1]]-v[t[0]],v[t[2]]-v[t[0]]);
            if(normals.y<0)for(int i=0;i<t.Count;i+=3){int swap=t[i+1];t[i+1]=t[i+2];t[i+2]=swap;}
            string name=direction<0?"Outer wet trial bank":"Inner wet trial bank";
            var mesh=new Mesh{name=name};mesh.vertices=v;mesh.uv=uv;mesh.triangles=t.ToArray();mesh.RecalculateNormals();mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh,Dir+"/"+name+".asset");
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root,false);
            go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=bank;
        }
    }
    public static string Main()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play mode first");
        if(Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty))throw new Exception("Save pending scene edits first");
        var scene=SceneManager.GetActiveScene();if(scene.path!="Assets/Scenes/TestTrackScene.unity")scene=EditorSceneManager.OpenScene("Assets/Scenes/TestTrackScene.unity");
        var parent=GameObject.Find("Offroad Course").transform;if(parent.Find("Mud and Water Trial")!=null)throw new Exception("Trial already exists");
        root=new GameObject("Mud and Water Trial").transform;root.SetParent(parent,false);roads.Clear();
        dirt=AssetDatabase.LoadAssetAtPath<Material>("Assets/Environments/OffroadCourse/SurfaceDetail/Dirt Hill 1 Surface Detail.mat");
        mud=Material("Wet Rutted Mud",new Color(.19f,.115f,.055f),.65f);AddMudTexture(mud);
        bank=Material("Wet Bank Soil",new Color(.33f,.23f,.12f),.12f);
        timber=AssetDatabase.LoadAssetAtPath<Material>("Assets/Environments/OffroadCourse/Timber.mat");
        sign=Material("Wet Trial Sign",new Color(.035f,.065f,.07f));gold=Material("Ford Depth Marker",new Color(.95f,.73f,.22f));
        water=new Material(Shader.Find("Vehicle Showcase/Shallow Trial Water")){name="Shallow Ford Water"};AssetDatabase.CreateAsset(water,Dir+"/Shallow Ford Water.mat");
        // Three dry sections and two wet beds share exact end vertices.
        Ribbon("Trial approach west",0,.29f,12,dirt,TestDriveSurface.Kind.Dry,true);
        Ribbon("Deep mud bed",.29f,.44f,12,mud,TestDriveSurface.Kind.Mud,true);
        Ribbon("Trial dry island",.44f,.56f,12,dirt,TestDriveSurface.Kind.Dry,true);
        Ribbon("Water crossing bed",.56f,.71f,12,bank,TestDriveSurface.Kind.Water,true);
        Ribbon("Trial approach east",.71f,1,12,dirt,TestDriveSurface.Kind.Dry,true);
        Ribbon("Mud slurry surface",.29f,.44f,11.7f,mud,TestDriveSurface.Kind.Mud,false,.25f);
        Ribbon("Ford water surface",.56f,.71f,11.7f,water,TestDriveSurface.Kind.Water,false,.37f);
        CreateBanks();
        for(int i=0;i<6;i++)
        {
            float u=Mathf.Lerp(.57f,.70f,i/5f);var p=Point(u);var f=(Point(u+.001f)-Point(u-.001f)).normalized;var side=Vector3.Cross(Vector3.up,f);
            foreach(float direction in new[]{-1f,1f})
            {
                var q=p+side*direction*6.6f;q.y=.65f;Box("Ford edge depth post",q,new Vector3(.12f,1.3f,.12f),timber);
                q.y=.4f;Box("25 cm depth band",q,new Vector3(.14f,.12f,.14f),gold);
            }
        }
        var island=parent.Find("Offroad scenery and facilities/South straight scrub island");if(island!=null)island.position+=new Vector3(-12,0,-8);
        Board("Wet trial entrance west","MUD + WATER >","OPTIONAL TRUCK TRIAL",new Vector3(151,0,-43),30);
        Board("Wet trial entrance east","< MUD + WATER","DRY LOOP STRAIGHT AHEAD",new Vector3(246,0,-40),-30);
        Board("Mud warning","DEEP MUD","LOW SPEED / KEEP MOVING",Point(.29f)+new Vector3(-6,0,-9),-65);
        Board("Ford warning","SHALLOW FORD","25 cm / SLOW CROSSING",Point(.56f)+new Vector3(3,0,-10),-90);
        // Keep the existing minimap reference while including all new driving ribbons.
        var names=new[]{"Dirt Hill 1","Dirt Hill 2","Gravel Corner 1","Gravel Corner 2","Dirt access road","Truck ridge branch"};
        roads.AddRange(names.Select(n=>parent.Find(n).GetComponent<MeshFilter>()));roads.Add(GameObject.Find("Asphalt — 12m wide").GetComponent<MeshFilter>());
        var map=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Environments/OffroadCourse/CourseMap.asset");map.Clear();map.CombineMeshes(roads.Select(f=>new CombineInstance{mesh=f.sharedMesh,transform=f.transform.localToWorldMatrix}).ToArray());EditorUtility.SetDirty(map);
        var minimap=Object.FindAnyObjectByType<TestDriveMinimap>();minimap.SetVerticesDirty();
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject=root.gameObject;
        if(SceneView.lastActiveSceneView!=null)SceneView.lastActiveSceneView.LookAt(new Vector3(195,0,-57),Quaternion.Euler(50,0,0),95);
        return "Added southern optional 12m-wide mud and 25cm ford route, wet surfaces with resistance, signs, depth posts and updated minimap. Main dry loop retained.";
    }
}

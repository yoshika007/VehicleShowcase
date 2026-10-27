using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using Object=UnityEngine.Object;

// A modest technical section: three alternating, low rocky obstacles with an open lane.
public static class AddRidgeChallenge
{
    static Vector3[] route;
    static Vector3 Side(int i){var d=route[(i+1)%route.Length]-route[(i+route.Length-1)%route.Length];d.y=0;return Vector3.Cross(Vector3.up,d.normalized);}
    static void Marker(Transform root,Vector3 p,Vector3 direction,Material wood,Material gold)
    {
        var post=GameObject.CreatePrimitive(PrimitiveType.Cube);post.name="Rock garden marker post";post.transform.SetParent(root,false);post.transform.position=p+Vector3.up*.65f;post.transform.localScale=new Vector3(.16f,1.3f,.16f);post.GetComponent<Renderer>().sharedMaterial=wood;Object.DestroyImmediate(post.GetComponent<Collider>());
        var marker=GameObject.CreatePrimitive(PrimitiveType.Cube);marker.name="Rock garden marker";marker.transform.SetParent(root,false);marker.transform.SetPositionAndRotation(p+Vector3.up*1.25f,Quaternion.LookRotation(direction));marker.transform.localScale=new Vector3(.65f,.42f,.1f);marker.GetComponent<Renderer>().sharedMaterial=gold;Object.DestroyImmediate(marker.GetComponent<Collider>());
    }
    public static string Run()
    {
        var scene=SceneManager.GetActiveScene();if(scene.path!="Assets/Scenes/PineRidgeScene.unity"||EditorApplication.isPlaying)throw new Exception("Open Pine Ridge in Edit Mode");
        if(scene.GetRootGameObjects().Any(g=>g.name=="Rock Garden Challenge"))throw new Exception("Rock garden already exists");
        var map=AssetDatabase.FindAssets("Trail map t:Mesh",new[]{"Assets/Environments/PineRidge"}).Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<Mesh>).First();var v=map.vertices;route=new Vector3[v.Length/2-1];for(int i=0;i<route.Length;i++)route[i]=(v[i*2]+v[i*2+1])*.5f;
        var stone=AssetDatabase.LoadAssetAtPath<Material>("Assets/Environments/PineRidge/Ridge stone.mat");var wood=AssetDatabase.LoadAssetAtPath<Material>("Assets/Environments/PineRidge/Details/Trail timber.mat");var gold=AssetDatabase.LoadAssetAtPath<Material>("Assets/Environments/PineRidge/Details/Trail marker gold.mat");
        if(stone==null||wood==null||gold==null)throw new Exception("Missing existing trail materials");
        var root=new GameObject("Rock Garden Challenge").transform;SceneManager.MoveGameObjectToScene(root.gameObject,scene);
        int[] indices={425,435,445}; float[] sides={-1f,1f,-1f};
        for(int i=0;i<indices.Length;i++)
        {
            int k=indices[i];var forward=route[k+1]-route[k];forward.y=0;forward.Normalize();var side=Side(k);var center=route[k]+side*(sides[i]*4.35f);
            for(int j=-1;j<=1;j++)
            {
                var p=center+side*(j*1.05f);var rock=GameObject.CreatePrimitive(PrimitiveType.Sphere);rock.name="Rock garden obstacle";rock.transform.SetParent(root,false);
                rock.transform.position=p+Vector3.up*.25f;rock.transform.rotation=Quaternion.LookRotation(forward)*Quaternion.Euler(0,j*8,0);
                rock.transform.localScale=new Vector3(1.85f,.95f,2.4f);rock.GetComponent<Renderer>().sharedMaterial=stone;rock.isStatic=true;
            }
            Marker(root,route[k]+side*(sides[i]*6.8f),forward,wood,gold);
        }
        var dir=route[408]-route[407];dir.y=0;dir.Normalize();var signPosition=route[407]+Side(407)*-10f;
        var board=GameObject.CreatePrimitive(PrimitiveType.Cube);board.name="Rock garden warning board";board.transform.SetParent(root,false);board.transform.SetPositionAndRotation(signPosition+Vector3.up*2.3f,Quaternion.LookRotation(dir));board.transform.localScale=new Vector3(5f,1.25f,.2f);board.GetComponent<Renderer>().sharedMaterial=wood;Object.DestroyImmediate(board.GetComponent<Collider>());
        var post2=GameObject.CreatePrimitive(PrimitiveType.Cube);post2.name="Rock garden warning post";post2.transform.SetParent(root,false);post2.transform.position=signPosition+Vector3.up*1.1f;post2.transform.localScale=new Vector3(.22f,2.2f,.22f);post2.GetComponent<Renderer>().sharedMaterial=wood;Object.DestroyImmediate(post2.GetComponent<Collider>());
        var label=new GameObject("Lettering",typeof(TextMeshPro));label.transform.SetParent(root,false);label.transform.SetPositionAndRotation(board.transform.position-dir*.12f,board.transform.rotation);var text=label.GetComponent<TextMeshPro>();text.text="ROCK GARDEN\n<size=65%>SLOW / CHOOSE A LINE</size>";text.fontSize=4.5f;text.alignment=TextAlignmentOptions.Center;text.color=new Color(.98f,.8f,.35f);text.rectTransform.sizeDelta=new Vector2(4.8f,1.15f);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        return "Added three alternating rock clusters with marked bypasses and an advance warning.";
    }
}

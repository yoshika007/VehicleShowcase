using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;

// Render actual scene geometry into small UI photographs without reloading or
// saving the user's current scene, including when that scene has unsaved edits.
public static class CaptureCoursePreviews
{
    const string Dir="Assets/Resources/CoursePreviews";
    static readonly string[] Scenes={"TestTrackScene","LakeTownScene","MountainPassScene","HarborStreetScene"};
    static readonly string[] Names={"TestCircuit","LakeTown","MountainPass","HarborStreet"};
    static readonly Vector3[] Positions={new Vector3(100,85,-115),new Vector3(-110,60,210),new Vector3(-290,175,20),new Vector3(-180,66,-283)};
    static readonly Vector3[] Focus={new Vector3(0,0,10),new Vector3(0,0,90),new Vector3(-40,65,170),new Vector3(-20,12,-143)};
    public static string PreviewPanel()
    {
        var scene=SceneManager.GetActiveScene();
        if(EditorApplication.isPlaying||scene.name!="ShowcaseScene")throw new Exception("Preview in ShowcaseScene outside Play Mode.");
        var menu=Object.FindObjectsByType<FreeRideCourseMenu>(FindObjectsInactive.Include).Single();
        typeof(FreeRideCourseMenu).GetMethod("AddCoursePhotos",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(menu,null);
        EditorSceneManager.MarkSceneDirty(scene);
        return "Course photos visible in Editor; existing scene edits remain unsaved.";
    }
    public static string Run(int only=-1)
    {
        if(EditorApplication.isPlaying)throw new Exception("Capture outside Play Mode.");
        var original=SceneManager.GetActiveScene();
        var roots=Enumerable.Range(0,SceneManager.sceneCount).SelectMany(i=>SceneManager.GetSceneAt(i).GetRootGameObjects()).ToArray();
        var states=roots.Select(g=>g.activeSelf).ToArray();
        Directory.CreateDirectory(Dir);
        try
        {
            foreach(var root in roots)root.SetActive(false);
            for(int i=0;i<Scenes.Length;i++)
            {
                if(only>=0 && only!=i)continue;
                Scene preview=default;RenderTexture target=null;Texture2D pixels=null;
                var previousTarget=RenderTexture.active;
                try
                {
                    preview=EditorSceneManager.OpenScene("Assets/Scenes/"+Scenes[i]+".unity",OpenSceneMode.Additive);
                    SceneManager.SetActiveScene(preview);
                    foreach(var canvas in preview.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Canvas>(true)))canvas.gameObject.SetActive(false);
                    var go=new GameObject("Temporary course photo camera",typeof(Camera));
                    var camera=go.GetComponent<Camera>();camera.enabled=false;camera.fieldOfView=i==3?56:52;camera.nearClipPlane=.2f;camera.farClipPlane=1800;
                    camera.transform.position=Positions[i];camera.transform.LookAt(Focus[i]);camera.aspect=600f/408f;
                    target=new RenderTexture(600,408,24,RenderTextureFormat.ARGB32){antiAliasing=4};target.Create();camera.targetTexture=target;
                    camera.Render();RenderTexture.active=target;
                    pixels=new Texture2D(600,408,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,600,408),0,0);pixels.Apply();
                    File.WriteAllBytes(Dir+"/"+Names[i]+".png",pixels.EncodeToPNG());
                }
                finally
                {
                    RenderTexture.active=previousTarget;
                    if(pixels)Object.DestroyImmediate(pixels);if(target){target.Release();Object.DestroyImmediate(target);}
                    if(preview.IsValid()&&preview.isLoaded)EditorSceneManager.CloseScene(preview,true);
                    SceneManager.SetActiveScene(original);
                }
            }
        }
        finally
        {
            for(int i=0;i<roots.Length;i++)if(roots[i])roots[i].SetActive(states[i]);
            SceneManager.SetActiveScene(original);
        }
        AssetDatabase.Refresh();
        foreach(string name in Names)
        {
            string path=Dir+"/"+name+".png";if(!File.Exists(path))continue;
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;
            importer.spriteImportMode=SpriteImportMode.Single;importer.mipmapEnabled=false;importer.isReadable=false;
            importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;importer.maxTextureSize=1024;
            importer.textureCompression=TextureImporterCompression.Compressed;importer.SaveAndReimport();
        }
        return "Captured "+(only>=0?Names[only]:"all four course photos")+"; original scene="+original.name+", dirty="+original.isDirty;
    }
}

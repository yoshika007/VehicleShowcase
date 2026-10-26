using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

public static class CheckHarborWindows
{
    static async Task Frames(int count){for(int i=0;i<count;i++){EditorApplication.Step();await Task.Delay(25);}}
    public static string Geometry()
    {
        const string dir="Assets/Environments/HarborStreet/";
        var source=AssetDatabase.LoadAssetAtPath<Mesh>(dir+"Blue windows scenery.asset");
        var parts=AssetDatabase.LoadAllAssetsAtPath(dir+"City window pattern.asset").OfType<Mesh>().ToArray();
        if(parts.Length!=2||parts.Sum(m=>m.vertexCount)!=source.vertexCount||parts.Sum(m=>m.triangles.Length)!=source.triangles.Length)
            throw new Exception("Window geometry was lost or duplicated.");
        foreach(var mesh in parts)
            if(mesh.triangles.Any(i=>i<0||i>=mesh.vertexCount)||mesh.normals.Length!=mesh.vertexCount)
                throw new Exception("Invalid window mesh "+mesh.name);
        return "All "+source.vertexCount/24+" windows preserved; valid indices/normals; "+string.Join("; ",parts.Select(m=>m.name+"="+m.vertexCount/24));
    }
    public static async Task<string> Load(bool night)
    {
        TestDriveSelection.Select(0,lighting:night?TestDriveSelection.TimeOfDay.Night:TestDriveSelection.TimeOfDay.Day,course:TestDriveSelection.CourseKind.HarborStreet);
        SceneManager.LoadScene("HarborStreetScene");await Frames(3);Object.FindAnyObjectByType<TestDriveIntro>()?.Skip();await Frames(2);
        var lit=GameObject.Find("Lit city windows").GetComponent<Renderer>();
        var dark=GameObject.Find("Batched Blue windows").GetComponent<Renderer>();
        string expected=night?"Evening windows":"Blue windows";
        if(lit.sharedMaterial.name!=expected||dark.sharedMaterial.name!="Blue windows")throw new Exception("Incorrect day/night window materials.");
        var controller=GameObject.Find("Harbor Street Course").GetComponent<LakeTownNightLights>();
        var data=new SerializedObject(controller);var array=data.FindProperty("windows");
        if(array.arraySize!=1||array.GetArrayElementAtIndex(0).objectReferenceValue!=lit)throw new Exception("Dark windows registered for lighting.");
        return (night?"Night":"Day")+": lit subset="+lit.sharedMaterial.name+", dark subset="+dark.sharedMaterial.name+", only lit subset changes material; frame="+Time.frameCount;
    }
}

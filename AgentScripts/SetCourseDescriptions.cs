using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;

public static class SetCourseDescriptions
{
    public static string RemoveMaps()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(EditorApplication.isPlaying||scene.name!="ShowcaseScene"||scene.isDirty)throw new Exception("Use the saved ShowcaseScene outside Play Mode.");
        var menu=Object.FindObjectsByType<FreeRideCourseMenu>(FindObjectsInactive.Include).Single();
        foreach(string name in new[]{"TestCircuit","LakeTown","MountainPass","HarborStreet"})
        {
            var map=menu.transform.Find("Dialog/"+name+"Course/CourseLayout");
            if(map)Object.DestroyImmediate(map.gameObject);
        }
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        AssetDatabase.DeleteAsset("Assets/Resources/CourseLayouts");
        return "Removed the four layout objects and unused generated map data.";
    }
    public static string Apply()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(EditorApplication.isPlaying||scene.name!="ShowcaseScene")throw new Exception("Use ShowcaseScene outside Play Mode.");
        bool wasDirty=scene.isDirty;
        var menu=Object.FindObjectsByType<FreeRideCourseMenu>(FindObjectsInactive.Include).Single();
        foreach(string method in new[]{"ArrangeWithoutSubtitle","ArrangeCourses","MatchButtonSizes","AddCoursePhotos","UpdateCourseDescriptions"})
            typeof(FreeRideCourseMenu).GetMethod(method,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(menu,null);
        EditorSceneManager.MarkSceneDirty(scene);if(!wasDirty)EditorSceneManager.SaveScene(scene);
        return "Descriptions displayed beneath all four photos; "+(wasDirty?"existing unsaved edits preserved":"scene saved");
    }
}

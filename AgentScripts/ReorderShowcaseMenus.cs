using System;using System.Reflection;using UnityEngine;using UnityEditor;using UnityEditor.Events;using UnityEditor.SceneManagement;using TMPro;
public static class ReorderShowcaseMenus{
static void Wire(UnityEngine.UI.Button button,UnityEngine.Events.UnityAction action){Undo.RecordObject(button,"Reorder showcase menu navigation");for(int i=button.onClick.GetPersistentEventCount()-1;i>=0;i--)UnityEventTools.RemovePersistentListener(button.onClick,i);UnityEventTools.AddPersistentListener(button.onClick,action);EditorUtility.SetDirty(button);}
public static string Run(){var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.name!="ShowcaseScene"||EditorApplication.isPlaying)throw new Exception("Use ShowcaseScene outside Play Mode");var manager=UnityEngine.Object.FindAnyObjectByType<VehicleShowcaseManager>();var data=new SerializedObject(manager);var courses=(GameObject)data.FindProperty("courseSelectionPanel").objectReferenceValue;var modes=(GameObject)data.FindProperty("driveModePanel").objectReferenceValue;
Undo.RecordObject(courses,"Rename course selection panel");courses.name="CourseSelectionPanel";
Wire(courses.transform.Find("Dialog/Back").GetComponent<UnityEngine.UI.Button>(),manager.CloseDriveMenu);
Wire(courses.transform.Find("Dialog/Go").GetComponent<UnityEngine.UI.Button>(),manager.ConfirmCourse);
Wire(modes.transform.Find("Dialog/BackButton").GetComponent<UnityEngine.UI.Button>(),manager.BackToCourses);
var heading=courses.transform.Find("Dialog/Heading").GetComponent<TMP_Text>();Undo.RecordObject(heading,"Course selection heading");heading.text="CHOOSE YOUR COURSE";
var menu=courses.GetComponent<FreeRideCourseMenu>();foreach(string name in new[]{"UpdateActionLabels","AddToonButtonIcons"})typeof(FreeRideCourseMenu).GetMethod(name,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(menu,null);
var description=modes.transform.Find("Dialog/FreeRunButton/Description").GetComponent<TMP_Text>();description.text="Explore your selected course.";
EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);return "CourseSelectionPanel renamed; NEXT -> ConfirmCourse; course BACK -> showroom; mode BACK -> course selection. Existing challenge BACK -> mode selection retained.";}}

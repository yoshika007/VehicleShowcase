using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using TMPro;
using Object = UnityEngine.Object;

public static class SetupApexPaintChoices
{
    const string Dir = "Assets/Materials/APEXGT";
    static void Set(Object target, string property, Object value)
    {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(property).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
    static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max, Vector2 pivot, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = (RectTransform)go.transform;rect.SetParent(parent, false);
        rect.anchorMin=min;rect.anchorMax=max;rect.pivot=pivot;rect.anchoredPosition=position;rect.sizeDelta=size;
        return rect;
    }
    public static string Main()
    {
        var scene=SceneManager.GetActiveScene();
        if(EditorApplication.isPlaying||scene.isDirty||scene.path!="Assets/Scenes/ShowcaseScene.unity")
            throw new Exception("Clean ShowcaseScene in Edit mode required");
        if(AssetDatabase.LoadAssetAtPath<VehiclePaintPalette>(Dir+"/APEX_PaintPalette.asset")!=null)
            throw new Exception("APEX palette already configured");
        var manager=Object.FindAnyObjectByType<VehicleShowcaseManager>();
        var panel=GameObject.Find("VehicleInfoPanel").GetComponent<RectTransform>();
        var drive=GameObject.Find("TestDriveButton").GetComponent<RectTransform>();
        var originalMaterial=AssetDatabase.LoadAssetAtPath<Material>(Dir+"/APEX_PaintAndGlass.mat");
        string sourcePath="Assets/Awbmecreations/Mobile Optimize-Free Low Poly Cars/Texture/Color.png";
        var source=new Texture2D(2,2);source.LoadImage(System.IO.File.ReadAllBytes(sourcePath));
        var mask=new Texture2D(2,2);mask.LoadImage(System.IO.File.ReadAllBytes(Dir+"/APEX_FinishMask.png"));
        var pixels=source.GetPixels32();var finish=mask.GetPixels32();
        string[] names={"SKY BLUE","RACING RED","PEARL WHITE","SOLAR YELLOW"};
        string[] files={"Blue","RacingRed","PearlWhite","SolarYellow"};
        Color32[] colors={new Color32(54,159,255,255),new Color32(255,66,72,255),new Color32(243,246,252,255),new Color32(255,212,71,255)};
        var materials=new Material[4];materials[0]=originalMaterial;
        for(int option=1;option<4;option++)
        {
            var changed=(Color32[])pixels.Clone();int count=0;
            for(int i=0;i<changed.Length;i++)
            {
                var m=finish[(i/source.width*mask.height/source.height)*mask.width+i%source.width*mask.width/source.width];var c=changed[i];
                if(Mathf.Abs(m.r-46)>1||Mathf.Abs(m.a-230)>1||c.b>=30||c.r<=c.g||c.g<30)continue;
                float shade=c.r/248f;var target=colors[option];
                changed[i]=new Color32((byte)Mathf.RoundToInt(target.r*shade),(byte)Mathf.RoundToInt(target.g*shade),(byte)Mathf.RoundToInt(target.b*shade),c.a);count++;
            }
            if(count!=2560)throw new Exception("Unexpected paint region: "+count);
            var texture=new Texture2D(source.width,source.height,TextureFormat.RGBA32,false);texture.SetPixels32(changed);texture.Apply();
            string path=Dir+"/APEX_"+files[option]+"Palette.png";System.IO.File.WriteAllBytes(path,texture.EncodeToPNG());Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            var settings=new TextureImporterSettings();((TextureImporter)AssetImporter.GetAtPath(sourcePath)).ReadTextureSettings(settings);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.SetTextureSettings(settings);importer.SaveAndReimport();
            var material=new Material(originalMaterial){name="APEX_"+files[option]};var palette=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            material.SetTexture("_BaseMap",palette);material.SetTexture("_MainTex",palette);
            AssetDatabase.CreateAsset(material,Dir+"/APEX_"+files[option]+".mat");materials[option]=material;
        }
        Object.DestroyImmediate(source);Object.DestroyImmediate(mask);
        var choices=ScriptableObject.CreateInstance<VehiclePaintPalette>();
        var data=new SerializedObject(choices);data.FindProperty("paletteId").stringValue="apex-gt";var options=data.FindProperty("options");options.arraySize=4;
        for(int i=0;i<4;i++){var item=options.GetArrayElementAtIndex(i);item.FindPropertyRelative("displayName").stringValue=names[i];item.FindPropertyRelative("swatch").colorValue=colors[i];item.FindPropertyRelative("material").objectReferenceValue=materials[i];}
        data.ApplyModifiedPropertiesWithoutUndo();AssetDatabase.CreateAsset(choices,Dir+"/APEX_PaintPalette.asset");
        foreach(var path in new[]{"Assets/Prefabs/Vehicles/SportsCar_Showcase.prefab","Assets/Prefabs/TestDrive/SportsCar_TestDrive.prefab"})
        {
            var contents=PrefabUtility.LoadPrefabContents(path);
            try{var paint=contents.GetComponent<VehiclePaint>();if(paint==null)paint=contents.AddComponent<VehiclePaint>();Set(paint,"palette",choices);Set(paint,"body",contents.GetComponent<Renderer>());PrefabUtility.SaveAsPrefabAsset(contents,path);}
            finally{PrefabUtility.UnloadPrefabContents(contents);}
        }
        var section=Rect("ExteriorColorSection",panel,Vector2.zero,Vector2.right,new Vector2(.5f,0),new Vector2(0,20),new Vector2(-56,88));
        var header=Rect("SelectedColorName",section,new Vector2(0,1),Vector2.one,new Vector2(.5f,1),Vector2.zero,new Vector2(0,24));
        var text=header.gameObject.AddComponent<TextMeshProUGUI>();text.font=GameObject.Find("CategoryText").GetComponent<TMP_Text>().font;text.fontSize=22;text.color=new Color(.73f,.81f,.82f);text.text="EXTERIOR  /  SKY BLUE";text.raycastTarget=false;text.alignment=TextAlignmentOptions.MidlineLeft;
        var row=Rect("ColorSwatches",section,Vector2.zero,Vector2.right,new Vector2(.5f,0),Vector2.zero,new Vector2(0,60));
        var layout=row.gameObject.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();layout.spacing=12;layout.childControlWidth=true;layout.childForceExpandWidth=true;layout.childControlHeight=true;layout.childForceExpandHeight=true;
        var template=Rect("SwatchTemplate",row,Vector2.zero,Vector2.zero,new Vector2(.5f,.5f),Vector2.zero,new Vector2(80,60));
        var face=template.gameObject.AddComponent<UnityEngine.UI.Image>();face.color=new Color(.07f,.09f,.11f);
        var button=template.gameObject.AddComponent<UnityEngine.UI.Button>();button.targetGraphic=face;
        var tint=button.colors;tint.normalColor=tint.highlightedColor=tint.selectedColor=Color.white;tint.pressedColor=new Color(.65f,.65f,.65f);tint.fadeDuration=.08f;button.colors=tint;
        var outline=template.gameObject.AddComponent<UnityEngine.UI.Outline>();outline.effectColor=new Color(.98f,.77f,.25f);outline.effectDistance=new Vector2(2,-2);outline.enabled=false;
        var chip=Rect("Swatch",template,Vector2.zero,Vector2.one,new Vector2(.5f,.5f),new Vector2(0,2),new Vector2(-16,-20));var image=chip.gameObject.AddComponent<UnityEngine.UI.Image>();image.color=colors[0];image.raycastTarget=false;
        var indicator=Rect("Selected",template,new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(0,3),new Vector2(24,3));var mark=indicator.gameObject.AddComponent<UnityEngine.UI.Image>();mark.color=new Color(.98f,.77f,.25f);mark.raycastTarget=false;indicator.gameObject.SetActive(false);
        var selector=manager.gameObject.AddComponent<VehiclePaintSelector>();Set(selector,"panel",panel);Set(selector,"testDriveButton",drive);Set(selector,"section",section.gameObject);Set(selector,"swatchRow",row);Set(selector,"swatchTemplate",button);Set(selector,"selectedName",text);Set(manager,"paintSelector",selector);
        template.gameObject.SetActive(false);section.gameObject.SetActive(false);
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        return "APEX GT has blue plus red, pearl white and yellow; separate per-vehicle palette data, showroom selector and test-drive carryover configured.";
    }
}

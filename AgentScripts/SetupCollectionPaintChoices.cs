using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;

// Extends the APEX trial using independent palettes, retaining each car's authored finish.
public static class SetupCollectionPaintChoices
{
    const string Dir = "Assets/Materials/CollectionFinishes";
    class Recipe
    {
        public string id, key;
        public int metallic, smoothness, peak;
        public string[] names;
        public Color32[] colors;
        public Recipe(string id, string key, int metallic, int smoothness, int peak, string[] names, string[] colors)
        {
            this.id=id;this.key=key;this.metallic=metallic;this.smoothness=smoothness;this.peak=peak;this.names=names;
            this.colors=colors.Select(hex=>{ColorUtility.TryParseHtmlString("#"+hex,out var color);return (Color32)color;}).ToArray();
        }
        public bool IsPaint(Color32 c)
        {
            switch(id)
            {
                case "ClassicCar": return c.b>c.g && c.g>c.r && c.b>100;
                case "Hatchback": return c.r>c.g && c.g>30 && c.b<30;
                case "MilitaryVehicle": return c.r==c.g && c.g==c.b && c.r<80;
                case "MonsterTruck": return c.r>c.b && c.b>c.g;
                case "MuscleCar": return c.g>c.r && c.g>c.b;
                case "PickupTruck": return c.r>c.g && c.b<30;
                case "PoliceCar": return c.b>c.g && c.g>c.r+30;
                case "Van": return c.r>c.b && c.b>c.g;
                default: return false;
            }
        }
    }
    static Recipe[] Recipes() => new[]
    {
        new Recipe("ClassicCar","regent-s",46,194,147,new[]{"HERITAGE BLUE","BURGUNDY","BRITISH GREEN","CHAMPAGNE"},new[]{"055493","862E43","285849","C4AE87"}),
        new Recipe("Hatchback","cityline",36,189,248,new[]{"SUNNY YELLOW","CORAL","MINT","SKY BLUE"},new[]{"F8BD00","FF7466","83CFB8","64BAE3"}),
        new Recipe("MilitaryVehicle","sentinel",10,143,41,new[]{"TACTICAL CHARCOAL","OLIVE DRAB","DESERT SAND","WINTER GREY"},new[]{"292929","62704D","B8A27C","A6ADAE"}),
        new Recipe("MonsterTruck","trailmaster",56,224,255,new[]{"CANDY PINK","ELECTRIC LIME","BLAZE ORANGE","COBALT BLUE"},new[]{"FF8FC3","A5E536","F58332","356CDA"}),
        new Recipe("MuscleCar","vortex-v8",51,199,161,new[]{"VINTAGE GREEN","CHARGER ORANGE","CHERRY RED","STEEL GREY"},new[]{"6EA171","E6742E","AF303D","66717C"}),
        new Recipe("PickupTruck","workhorse",31,173,176,new[]{"HARVEST GOLD","FOREST GREEN","UTILITY WHITE","WORKSHOP RED"},new[]{"B07900","466149","E9E4D7","A84338"}),
        new Recipe("PoliceCar","interceptor",31,189,204,new[]{"PATROL CYAN","MIDNIGHT NAVY","PATROL BLACK","HIGHWAY SILVER"},new[]{"009DCC","244568","343D48","A1ADB8"}),
        new Recipe("Van","wayfarer",26,173,187,new[]{"DUSTY ROSE","CREAM","SEAFOAM","SLATE BLUE"},new[]{"BB8790","E6D8B6","79B1A0","69869F"})
    };
    static void Set(Object target,string property,Object value)
    {
        var serialized=new SerializedObject(target);serialized.FindProperty(property).objectReferenceValue=value;serialized.ApplyModifiedPropertiesWithoutUndo();
    }
    public static string Main()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required");
        foreach(var recipe in Recipes())
        {
            if(AssetDatabase.LoadAssetAtPath<VehiclePaintPalette>(Dir+"/"+recipe.id+"_PaintPalette.asset")!=null)
                throw new Exception("Palette already configured: "+recipe.id);
            if(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Vehicles/"+recipe.id+"_Showcase.prefab")==null)
                throw new Exception("Missing prefab "+recipe.id);
        }
        var report=new List<string>();
        foreach(var recipe in Recipes())
        {
            var originalMaterial=AssetDatabase.LoadAssetAtPath<Material>(Dir+"/"+recipe.id+"_PaintAndGlass.mat");
            string sourcePath=AssetDatabase.GetAssetPath(originalMaterial.GetTexture("_BaseMap"));
            var source=new Texture2D(2,2);source.LoadImage(System.IO.File.ReadAllBytes(sourcePath));
            var mask=new Texture2D(2,2);mask.LoadImage(System.IO.File.ReadAllBytes(AssetDatabase.GetAssetPath(originalMaterial.GetTexture("_MetallicGlossMap"))));
            var pixels=source.GetPixels32();var finish=mask.GetPixels32();
            var selected=Enumerable.Range(0,pixels.Length).Where(i=>
            {
                var m=finish[(i/source.width*mask.height/source.height)*mask.width+i%source.width*mask.width/source.width];
                return Mathf.Abs(m.r-recipe.metallic)<=1&&Mathf.Abs(m.a-recipe.smoothness)<=1&&recipe.IsPaint(pixels[i]);
            }).ToArray();
            if(selected.Length<100)throw new Exception("Unexpected paint region: "+recipe.id+" "+selected.Length);
            var materials=new Material[4];materials[0]=originalMaterial;
            for(int option=1;option<4;option++)
            {
                var changed=(Color32[])pixels.Clone();var target=recipe.colors[option];
                foreach(int i in selected)
                {
                    var c=pixels[i];float shade=Mathf.Max(c.r,c.g,c.b)/(float)recipe.peak;
                    changed[i]=new Color32((byte)Mathf.RoundToInt(Mathf.Min(255,target.r*shade)),(byte)Mathf.RoundToInt(Mathf.Min(255,target.g*shade)),(byte)Mathf.RoundToInt(Mathf.Min(255,target.b*shade)),c.a);
                }
                var texture=new Texture2D(source.width,source.height,TextureFormat.RGBA32,false);texture.SetPixels32(changed);texture.Apply();
                string prefix=Dir+"/"+recipe.id+"_Paint"+option;string path=prefix+"Palette.png";
                System.IO.File.WriteAllBytes(path,texture.EncodeToPNG());Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(path);
                var settings=new TextureImporterSettings();((TextureImporter)AssetImporter.GetAtPath(sourcePath)).ReadTextureSettings(settings);
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.SetTextureSettings(settings);importer.SaveAndReimport();
                var material=new Material(originalMaterial){name=recipe.id+"_"+recipe.names[option].Replace(' ','_')};var paletteTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                material.SetTexture("_BaseMap",paletteTexture);material.SetTexture("_MainTex",paletteTexture);AssetDatabase.CreateAsset(material,prefix+".mat");materials[option]=material;
            }
            Object.DestroyImmediate(source);Object.DestroyImmediate(mask);
            var palette=ScriptableObject.CreateInstance<VehiclePaintPalette>();var serialized=new SerializedObject(palette);
            serialized.FindProperty("paletteId").stringValue=recipe.key;var options=serialized.FindProperty("options");options.arraySize=4;
            for(int i=0;i<4;i++){var item=options.GetArrayElementAtIndex(i);item.FindPropertyRelative("displayName").stringValue=recipe.names[i];item.FindPropertyRelative("swatch").colorValue=recipe.colors[i];item.FindPropertyRelative("material").objectReferenceValue=materials[i];}
            serialized.ApplyModifiedPropertiesWithoutUndo();AssetDatabase.CreateAsset(palette,Dir+"/"+recipe.id+"_PaintPalette.asset");
            foreach(string path in new[]{"Assets/Prefabs/Vehicles/"+recipe.id+"_Showcase.prefab","Assets/Prefabs/TestDrive/"+recipe.id+"_TestDrive.prefab"})
            {
                var contents=PrefabUtility.LoadPrefabContents(path);
                try{var paint=contents.GetComponent<VehiclePaint>();if(paint==null)paint=contents.AddComponent<VehiclePaint>();Set(paint,"palette",palette);Set(paint,"body",contents.GetComponent<Renderer>());PrefabUtility.SaveAsPrefabAsset(contents,path);}
                finally{PrefabUtility.UnloadPrefabContents(contents);}
            }
            report.Add(recipe.id+": "+selected.Length+" paint pixels; "+string.Join(", ",recipe.names));
        }
        AssetDatabase.SaveAssets();return string.Join("\n",report);
    }
}

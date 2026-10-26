using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
public static class InspectHarborHairpin
{
    public static string Run()
    {
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Environments/HarborStreet/Harbor Street asphalt.asset");var v=mesh.vertices;
        var p=Enumerable.Range(0,v.Length/2-1).Select(i=>(v[i*2]+v[i*2+1])*.5f).ToArray();
        int overlaps=0;float min=1000;string pair="";
        for(int i=0;i<p.Length;i++)for(int j=i+35;j<p.Length;j++)
        {
            if(p.Length-j+i<35 || p[i].x< -65||p[i].x>60||p[i].z<65||p[i].z>165||p[j].x< -65||p[j].x>60||p[j].z<65||p[j].z>165)continue;
            float distance=new Vector2(p[i].x-p[j].x,p[i].z-p[j].z).magnitude;
            if(distance<15.2f&&Mathf.Abs(p[i].y-p[j].y)>4)overlaps++;
            if(distance<min){min=distance;pair=i+":"+p[i]+" vs "+j+":"+p[j];}
        }
        return "Nonadjacent hairpin centerlines: closest="+min.ToString("F2")+"m, pair="+pair+", vertically separated pairs inside retaining-wall width="+overlaps;
    }
}

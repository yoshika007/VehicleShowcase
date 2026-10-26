using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

// Reuses the authored display-bay mesh and ivory material. No scene save, new
// colliders or changes to the vehicle display point are needed.
public static class BuildShowcaseParkingLot
{
    static readonly List<Vector3> vertices=new List<Vector3>();
    static readonly List<int> triangles=new List<int>();
    static void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d)
    {
        int n=vertices.Count;vertices.AddRange(new[]{a,b,c,d});
        triangles.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});
    }
    static void Paint(float x0,float x1,float z0,float z1)
    {
        Quad(new Vector3(x0,0,z0),new Vector3(x0,0,z1),new Vector3(x1,0,z1),new Vector3(x1,0,z0));
    }
    static void Box(Vector3 center,Vector3 size)
    {
        var a=center-size*.5f;var b=center+size*.5f;
        Quad(new Vector3(a.x,b.y,a.z),new Vector3(a.x,b.y,b.z),new Vector3(b.x,b.y,b.z),new Vector3(b.x,b.y,a.z));
        Quad(new Vector3(a.x,a.y,a.z),new Vector3(a.x,b.y,a.z),new Vector3(b.x,b.y,a.z),new Vector3(b.x,a.y,a.z));
        Quad(new Vector3(b.x,a.y,b.z),new Vector3(b.x,b.y,b.z),new Vector3(a.x,b.y,b.z),new Vector3(a.x,a.y,b.z));
        Quad(new Vector3(a.x,a.y,b.z),new Vector3(a.x,b.y,b.z),new Vector3(a.x,b.y,a.z),new Vector3(a.x,a.y,a.z));
        Quad(new Vector3(b.x,a.y,a.z),new Vector3(b.x,b.y,a.z),new Vector3(b.x,b.y,b.z),new Vector3(b.x,a.y,b.z));
    }
    // Thin, interrupted stencil strokes give the numbers a faded painted appearance.
    static void Number(float center,int number)
    {
        string text=number.ToString("00");
        string[] digits={"abcedf","bc","abdeg","abcdg"};
        // Segments: a top, b upper-right, c lower-right, d bottom,
        // e lower-left, f upper-left, g middle.
        for(int i=0;i<text.Length;i++)
        {
            int digit=text[i]-'0';
            foreach(char segment in digits[digit])
            {
                float x=0,y=0,w=.32f,h=.045f;
                switch(segment)
                {
                    case 'a': y=.62f;break;
                    case 'g': y=.31f;break;
                    case 'd': y=0;break;
                    case 'b': x=.275f;y=.335f;w=.045f;h=.285f;break;
                    case 'c': x=.275f;y=.045f;w=.045f;h=.265f;break;
                    case 'e': x=0;y=.045f;w=.045f;h=.265f;break;
                    case 'f': x=0;y=.335f;w=.045f;h=.285f;break;
                }
                float offset=-.4f+i*.48f;
                if(w>h)
                {
                    Paint(center-offset-x-w,center-offset-x-w*.52f,4.05f-y-h,4.05f-y);
                    Paint(center-offset-x-w*.48f,center-offset-x,4.05f-y-h,4.05f-y);
                }
                else Paint(center-offset-x-w,center-offset-x,4.05f-y-h,4.05f-y);
            }
        }
    }
    public static string Run()
    {
        vertices.Clear();triangles.Clear();
        // Three generous bays, with their rear corners kept inside the existing apron.
        const float rear=-3.1f,front=3.2f,width=.08f;
        foreach(float x in new[]{-5.4f,-1.8f,1.8f,5.4f})
        {
            Paint(x-width*.5f,x+width*.5f,rear,front);
        }
        Paint(-5.4f,5.4f,rear,rear+width);
        for(int bay=0;bay<3;bay++)
        {
            float x=(bay-1)*3.6f;
            // Short concrete stops sit at the far end, leaving the viewing aisle open.
            Box(new Vector3(x,.085f,-2.72f),new Vector3(1.35f,.18f,.30f));
            Number(x,bay+1);
        }
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Environments/LakesideOverlookTrial/ShowcaseDisplayBay.asset");
        mesh.Clear();mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
        EditorUtility.SetDirty(mesh);AssetDatabase.SaveAssetIfDirty(mesh);
        return "Three angled parking bays, rear wheel stops and worn 01 / 02 / 03 stencils; centre vehicle and open foreground retained.";
    }
}

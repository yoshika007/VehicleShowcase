using UnityEngine;
using UnityEngine.UI;

// Vector UI geometry remains sharp at every Canvas scale; no bitmap assets needed.
public class TestDriveButtonFace : MaskableGraphic
{
    private float emphasis;
    private bool unavailable;
    public void SetAppearance(float highlight, bool disabled)
    {
        if (Mathf.Abs(emphasis-highlight)<0.001f && unavailable==disabled) return;
        emphasis=highlight; unavailable=disabled; SetVerticesDirty();
    }
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        Rect r=GetPixelAdjustedRect();
        Color edge=Color.Lerp(new Color(.64f,.43f,.10f),new Color(1f,.92f,.62f),emphasis);
        Color face=Color.Lerp(new Color(.98f,.77f,.25f),new Color(.85f,.64f,.18f),emphasis);
        Color ink=new Color(.055f,.065f,.075f);
        if(unavailable){face=new Color(.45f,.43f,.36f);edge=new Color(.30f,.29f,.26f);}
        Panel(mesh,r,edge);
        Panel(mesh,new Rect(r.x+1.5f,r.y+1.5f,r.width-3f,r.height-3f),face);
        // Fine upper highlight and a diagonal separation for the flag.
        Quad(mesh,new Vector2(r.x+18,r.yMax-3),new Vector2(r.xMax-22,r.yMax-2),new Color(1,1,.85f,.42f));
        Line(mesh,new Vector2(r.x+58,r.y+12),new Vector2(r.x+67,r.yMax-12),1,new Color(.2f,.13f,.025f,.24f));
        float left=r.x+23, top=r.center.y+11;
        Line(mesh,new Vector2(left-2,r.center.y-14),new Vector2(left-2,top+3),2,ink);
        // Four columns by three rows: an unmistakable checkered start flag.
        for(int y=0;y<3;y++)for(int x=0;x<4;x++)
        {
            var cell=new Rect(left+x*5,top-(y+1)*5,5,5);
            Quad(mesh,cell.min,cell.max,(x+y)%2==0?ink:new Color(1f,.95f,.72f));
        }
        // Compact forward chevron, safely inside the right edge.
        var c=new Vector2(r.xMax-27,r.center.y);
        Line(mesh,c+new Vector2(-4,7),c+new Vector2(3,0),2.5f,ink);
        Line(mesh,c+new Vector2(3,0),c+new Vector2(-4,-7),2.5f,ink);
    }
    private void Panel(VertexHelper mesh,Rect r,Color tint)
    {
        var p=new[]{new Vector2(r.x+12,r.y),new Vector2(r.xMax-18,r.y),new Vector2(r.xMax,r.y+18),new Vector2(r.xMax,r.yMax-12),new Vector2(r.xMax-12,r.yMax),new Vector2(r.x+18,r.yMax),new Vector2(r.x,r.yMax-18),new Vector2(r.x,r.y+12)};
        int start=mesh.currentVertCount;
        foreach(var v in p) mesh.AddVert(v,tint*color,Vector2.zero);
        for(int i=1;i<p.Length-1;i++)mesh.AddTriangle(start,start+i,start+i+1);
    }
    private void Quad(VertexHelper mesh,Vector2 min,Vector2 max,Color tint)
    {
        int i=mesh.currentVertCount;tint*=color;
        mesh.AddVert(new Vector2(min.x,min.y),tint,Vector2.zero);mesh.AddVert(new Vector2(min.x,max.y),tint,Vector2.zero);
        mesh.AddVert(new Vector2(max.x,max.y),tint,Vector2.zero);mesh.AddVert(new Vector2(max.x,min.y),tint,Vector2.zero);
        mesh.AddTriangle(i,i+1,i+2);mesh.AddTriangle(i,i+2,i+3);
    }
    private void Line(VertexHelper mesh,Vector2 a,Vector2 b,float width,Color tint)
    {
        var side=new Vector2(-(b-a).y,(b-a).x).normalized*width*.5f;
        int i=mesh.currentVertCount;tint*=color;
        mesh.AddVert(a-side,tint,Vector2.zero);mesh.AddVert(a+side,tint,Vector2.zero);mesh.AddVert(b+side,tint,Vector2.zero);mesh.AddVert(b-side,tint,Vector2.zero);
        mesh.AddTriangle(i,i+1,i+2);mesh.AddTriangle(i,i+2,i+3);
    }
}

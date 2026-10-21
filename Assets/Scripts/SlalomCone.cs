using UnityEngine;

public class SlalomCone : MonoBehaviour
{
    private SlalomCourse course;
    public void Initialize(SlalomCourse owner) => course = owner;
    public void SetHit(bool hit)
    {
        var block = new MaterialPropertyBlock();
        block.SetColor("_BaseColor", hit ? new Color(.65f, .04f, .025f) : new Color(1f, .3f, .025f));
        GetComponent<Renderer>().SetPropertyBlock(block);
    }
    private void OnTriggerEnter(Collider other) { if (course != null) course.ReportCone(this, other); }
    private void OnTriggerStay(Collider other) { if (course != null) course.ReportCone(this, other); }
}

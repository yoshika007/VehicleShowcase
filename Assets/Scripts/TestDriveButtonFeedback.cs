using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(UnityEngine.UI.Button))]
public class TestDriveButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler, ISubmitHandler
{
    [SerializeField] private TestDriveButtonFace face;
    private UnityEngine.UI.Button button;
    private Vector3 restingScale;
    private bool hovered, selected, pressed;
    private float emphasis, submitUntil;
    private void Awake() { button=GetComponent<UnityEngine.UI.Button>();restingScale=transform.localScale; }
    private void Update()
    {
        bool available=button.IsInteractable();
        float target=available && (hovered || selected)?1f:0f;
        float t=1f-Mathf.Exp(-20f*Time.unscaledDeltaTime);
        emphasis=Mathf.Lerp(emphasis,target,t);
        float scale=available && (pressed || Time.unscaledTime<submitUntil)?.97f:Mathf.Lerp(1f,1.03f,emphasis);
        transform.localScale=Vector3.Lerp(transform.localScale,restingScale*scale,t);
        if(face!=null)face.SetAppearance(emphasis,!available);
    }
    public void OnPointerEnter(PointerEventData e) { hovered=true; }
    public void OnPointerExit(PointerEventData e) { hovered=false;pressed=false; }
    public void OnPointerDown(PointerEventData e) { if(e.button==PointerEventData.InputButton.Left && button.IsInteractable())pressed=true; }
    public void OnPointerUp(PointerEventData e) { pressed=false; }
    public void OnSelect(BaseEventData e) { selected=true; }
    public void OnDeselect(BaseEventData e) { selected=false;pressed=false; }
    public void OnSubmit(BaseEventData e) { if(button.IsInteractable())submitUntil=Time.unscaledTime+.12f; }
    private void OnDisable()
    {
        hovered=selected=pressed=false;emphasis=submitUntil=0;
        if(button!=null)transform.localScale=restingScale;
        if(face!=null)face.SetAppearance(0,false);
    }
}

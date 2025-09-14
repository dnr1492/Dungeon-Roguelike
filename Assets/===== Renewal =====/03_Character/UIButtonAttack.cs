using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class UIButtonAttack : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] Character character;

    private int activePointerId = -1;
    private bool pressed;

    public void OnPointerDown(PointerEventData eventData)
    {
        //이미 누르고 있는 중이면 무시 (다중터치 방지)
        if (pressed) return;  

        pressed = true;
        activePointerId = eventData.pointerId;
        if (character != null) character.FireButtonDown();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!pressed || eventData.pointerId != activePointerId) return;

        pressed = false;
        activePointerId = -1;
        if (character != null) character.FireButtonUp();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        //손가락이 버튼 영역을 벗어나면 Up과 동일하게 해제
        if (!pressed || eventData.pointerId != activePointerId) return;

        pressed = false;
        activePointerId = -1;
        if (character != null) character.FireButtonUp();
    }

    private void OnDisable()
    {
        //버튼이 비활성화되거나 화면 전환 시 눌림 상태가 남지 않도록 복구
        if (!pressed) return;

        pressed = false;
        activePointerId = -1;
        if (character != null) character.FireButtonUp();
    }
}

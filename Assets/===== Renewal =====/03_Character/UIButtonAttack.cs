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
        //�̹� ������ �ִ� ���̸� ���� (������ġ ����)
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
        //�հ����� ��ư ������ ����� Up�� �����ϰ� ����
        if (!pressed || eventData.pointerId != activePointerId) return;

        pressed = false;
        activePointerId = -1;
        if (character != null) character.FireButtonUp();
    }

    private void OnDisable()
    {
        //��ư�� ��Ȱ��ȭ�ǰų� ȭ�� ��ȯ �� ���� ���°� ���� �ʵ��� ����
        if (!pressed) return;

        pressed = false;
        activePointerId = -1;
        if (character != null) character.FireButtonUp();
    }
}

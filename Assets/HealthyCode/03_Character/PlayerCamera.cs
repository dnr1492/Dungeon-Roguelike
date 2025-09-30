using UnityEngine;

[RequireComponent(typeof(Camera))]
public class PlayerCamera : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] Vector2 offset;

    private Camera cam;

    private readonly float smoothTime = 0.15f;
    private readonly bool clampToRoom = true;
    private Player player;
    private Vector3 vel;
    private float lastAspect = -1f;

    private void Awake()
    {
        if (!player && target) player = target.GetComponentInParent<Player>();

        cam = GameManager.Instance.PlayerCam;
    }

    private void Update()
    {
        //화면비 변경 감지
        if (cam && Mathf.Abs(cam.aspect - lastAspect) > 0.0001f)
            ConfigureOrtho();
    }

    private void LateUpdate()
    {
        if (!target) return;

        //기본 추적 위치
        Vector3 desired = new Vector3(
            target.position.x + offset.x,
            target.position.y + offset.y,
            transform.position.z
        );

        if (clampToRoom && player && player.HasRoomBounds && cam.orthographic)
        {
            Bounds b = player.RoomBounds;

            float vert = cam.orthographicSize;
            float horiz = vert * cam.aspect;

            //가로
            if (b.size.x <= 2f * horiz) desired.x = b.center.x;  //방이 화면보다 작으면 x 중앙 고정
            else desired.x = Mathf.Clamp(desired.x, b.min.x + horiz, b.max.x - horiz);

            //세로
            if (b.size.y <= 2f * vert) desired.y = b.center.y;  //방이 화면보다 작으면 y 중앙 고정
            else desired.y = Mathf.Clamp(desired.y, b.min.y + vert, b.max.y - vert);
        }

        //스무스 이동
        Vector3 smoothed = Vector3.SmoothDamp(transform.position, desired, ref vel, smoothTime);
        transform.position = smoothed;
    }

    private void ConfigureOrtho()
    {
        if (!cam) return;
        cam.orthographic = true;

        if (ConstClass.Camera.CONSTANT_VERTICAL)
        {
            //단말 상관없이 '세로 월드 높이'를 고정
            cam.orthographicSize = ConstClass.Camera.DESIGN_HEIGHT_PX / (2f * ConstClass.Camera.SPRITE_PPU);
        }
        else
        {
            //단말 상관없이 '가로 월드 폭'을 고정
            float halfWidth = ConstClass.Camera.DESIGN_WIDTH_PX / (2f * ConstClass.Camera.SPRITE_PPU);
            cam.orthographicSize = halfWidth / cam.aspect;
        }
        lastAspect = cam.aspect;
    }
}

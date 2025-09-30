using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class UIPlay : MonoBehaviour
{
    [Header("무기")]
    [SerializeField] private WeaponController weaponController;
    private List<WeaponController> weaponControllers = new List<WeaponController>();
    private GunController gunController;
    private KnifeController knifeController;

    [Header("UI_버튼")]
    [SerializeField] private Button btnAttack, btnTypeChange;

    private void Awake()
    {
        foreach (Transform child in weaponController.transform)
        {
            var controller = child.GetComponent<WeaponController>();
            if (controller == null) continue;

            switch (controller)
            {
                case GunController gun:
                    gunController = gun;
                    gunController.gameObject.SetActive(true);
                    break;
                case KnifeController knife:
                    knifeController = knife;
                    knifeController.gameObject.SetActive(false);
                    break;
            }

            weaponControllers.Add(controller);
        }

        if (btnAttack != null) btnAttack.onClick.AddListener(() => {
            if (gunController.gameObject.activeSelf) gunController.CreateBullet();
            else if (knifeController.gameObject.activeSelf) knifeController.GetAttackWithKnife();
        });

        if (btnTypeChange != null) btnTypeChange.onClick.AddListener(() => {
            List<GameObject> controllers = weaponControllers.Select(wc => wc.gameObject).ToList();
            GameObject activeController = controllers.Find(go => go.activeSelf);
            if (activeController != null) {
                foreach (var controller in controllers) {
                    if (controller != activeController && !controller.activeSelf) controller.SetActive(true);
                    else if (controller == activeController && controller.activeSelf) controller.SetActive(false);
                }
            }
        });
    }
}

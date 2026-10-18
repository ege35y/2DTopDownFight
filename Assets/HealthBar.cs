using UnityEngine;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private Transform fillTransform;
    [SerializeField] private Vector3 offset = new Vector3(0, 0.8f, 0);

    private Transform parentTarget;
    private Vector3 initialFillScale = Vector3.zero;
    private Vector3 initialFillLocalPos;

    void Awake()
    {
        parentTarget = transform.parent;
        CaptureInitialValues();
    }

    private void CaptureInitialValues()
    {
        if (fillTransform != null && initialFillScale == Vector3.zero)
        {
            initialFillScale = fillTransform.localScale;
            initialFillLocalPos = fillTransform.localPosition;

            // Eğer sıfır veya çok küçük geldiyse varsayılan bir değer ata
            if (initialFillScale.x <= 0.001f)
            {
                initialFillScale = Vector3.one;
            }
        }
    }

    void LateUpdate()
    {
        if (parentTarget != null)
        {
            transform.position = parentTarget.position + offset;
            transform.rotation = Quaternion.identity;
        }
    }

    public void UpdateHealthBar(float currentHealth, float maxHealth)
    {
        CaptureInitialValues();

        if (fillTransform != null && maxHealth > 0)
        {
            float fillRatio = Mathf.Clamp01(currentHealth / maxHealth);

            // Genişliği ayarla
            fillTransform.localScale = new Vector3(initialFillScale.x * fillRatio, initialFillScale.y, initialFillScale.z);

            // Soldan sağa doğru erime hizalaması
            float widthDifference = initialFillScale.x * (1f - fillRatio);
            fillTransform.localPosition = new Vector3(initialFillLocalPos.x - (widthDifference * 0.5f), initialFillLocalPos.y, initialFillLocalPos.z);
        }
    }
}
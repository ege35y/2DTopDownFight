using UnityEngine;

public class CameraFollow2D : MonoBehaviour
{
    [Header("Hedef ve Takip")]
    [SerializeField] private Transform target;
    [Tooltip("Kameranın hedefe ulaşma yumuşaklığı (0.15 - 0.25 arası idealdir)")]
    [SerializeField] private float smoothTime = 0.2f;
    [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);

    private Vector3 currentVelocity = Vector3.zero;

    void Start()
    {
        if (target == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                target = playerObj.transform;
            }
        }

        // Oyun başladığı an kamerayı doğrudan oyuncunun üzerine konumlandır (kayarak gelmesin)
        if (target != null)
        {
            transform.position = target.position + offset;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 targetPosition = target.position + offset;

        // SmoothDamp: Fizik hareketlerindeki titreşimi filtreleyen en akıcı sönümleme algoritması
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref currentVelocity, smoothTime);
    }
}
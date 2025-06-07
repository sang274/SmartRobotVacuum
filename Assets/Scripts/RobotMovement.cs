using UnityEngine;

public class RobotMovement : MonoBehaviour
{
    public float speed = 2f; // Tốc độ có thể điều chỉnh trong Inspector
    public float raycastDistance = 1f; // Khoảng cách kiểm tra raycast
    public float stuckThreshold = 0.5f; // Thời gian tối đa cho phép bị kẹt (giây)
    public float stuckForce = 3f; // Lực đẩy để thoát kẹt
    public float stuckCooldown = 1f; // Thời gian chờ sau khi thoát kẹt
    public float stopDuration = 0.3f; // Thời gian dừng sau va chạm (giây)
    public float rotationSpeed = 5f; // Tốc độ xoay body

    private Vector2 direction; // Hướng di chuyển hiện tại
    private Rigidbody2D rb;
    private float lastCollisionTime; // Thời gian va chạm cuối cùng
    private bool isStuck; // Trạng thái bị kẹt
    private float lastEscapeTime; // Thời gian thoát kẹt cuối cùng
    private Collider2D robotCollider; // Collider của robot
    private bool isStopped; // Trạng thái dừng sau va chạm
    private float stopStartTime; // Thời gian bắt đầu dừng
    private Quaternion targetRotation; // Góc xoay mục tiêu

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        robotCollider = GetComponent<Collider2D>();
        // Đặt hướng ban đầu ngẫu nhiên
        direction = Random.insideUnitCircle.normalized;
    }

    void Update()
    {
        // Kiểm tra xem robot có bị kẹt không
        if (Time.time - lastCollisionTime < stuckThreshold && rb.linearVelocity.magnitude < 0.1f)
        {
            isStuck = true;
        }
        else
        {
            isStuck = false;
        }

        // Nếu bị kẹt, thử thoát ra
        if (isStuck && Time.time - lastEscapeTime > stuckCooldown)
        {
            EscapeStuckSituation();
            lastEscapeTime = Time.time;
        }

        // Xử lý trạng thái dừng và xoay sau va chạm
        if (isStopped)
        {
            // Dừng robot
            rb.linearVelocity = Vector2.zero;

            // Tính thời gian dừng
            if (Time.time - stopStartTime >= stopDuration)
            {
                isStopped = false; // Kết thúc thời gian dừng
            }
            else
            {
                // Xoay body từ từ về hướng đi mới
                if (direction != Vector2.zero)
                {
                    float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                    targetRotation = Quaternion.Euler(0f, 0f, angle);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
                }
            }
        }
        else
        {
            // Di chuyển robot khi không dừng
            rb.linearVelocity = direction * speed;

            // Xoay robot theo hướng di chuyển khi không va chạm
            if (direction != Vector2.zero)
            {
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        lastCollisionTime = Time.time;

        // Phản xạ hướng dựa trên pháp tuyến va chạm
        Vector2 normal = collision.contacts[0].normal;
        direction = Vector2.Reflect(direction, normal).normalized;

        // Kiểm tra xem hướng mới có khả thi không
        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, raycastDistance);
        if (hit.collider != null)
        {
            // Nếu hướng phản xạ không khả thi, tìm hướng thoát
            direction = FindEscapeDirection();
        }

        // Bắt đầu trạng thái dừng sau va chạm
        isStopped = true;
        stopStartTime = Time.time;
    }

    private void EscapeStuckSituation()
    {
        // Tìm hướng thoát khả thi
        direction = FindEscapeDirection();

        // Áp dụng lực đẩy để thoát kẹt
        rb.AddForce(direction * stuckForce, ForceMode2D.Impulse);

        // Tạm thời vô hiệu hóa va chạm với một số collider nếu cần
        Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(transform.position, 1f);
        foreach (Collider2D collider in nearbyColliders)
        {
            if (collider != robotCollider)
            {
                Physics2D.IgnoreCollision(robotCollider, collider, true);
                StartCoroutine(ReEnableCollision(collider, 0.5f));
            }
        }
    }

    private Vector2 FindEscapeDirection()
    {
        // Quét 8 hướng xung quanh robot (0, 45, 90, 135, 180, 225, 270, 315 độ)
        float bestDistance = 0f;
        Vector2 bestDirection = direction;

        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f * Mathf.Deg2Rad;
            Vector2 testDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)).normalized;
            RaycastHit2D hit = Physics2D.Raycast(transform.position, testDirection, raycastDistance);

            if (hit.collider == null || hit.distance > bestDistance)
            {
                bestDistance = hit.distance;
                bestDirection = testDirection;
            }
        }

        return bestDirection;
    }

    private System.Collections.IEnumerator ReEnableCollision(Collider2D collider, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (collider != null && robotCollider != null)
        {
            Physics2D.IgnoreCollision(robotCollider, collider, false);
        }
    }
}
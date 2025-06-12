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
    public float stuckCheckInterval = 1f; // Thời gian kiểm tra bị kẹt
    public float minMoveDistance = 0.1f; // Khoảng cách tối thiểu để không bị coi là kẹt
    public float maxSurfaceDistance = 0.5f; // Khoảng cách tối đa di chuyển dọc bề mặt trước khi đổi hướng

    private Vector2 direction; // Hướng di chuyển hiện tại
    private Rigidbody2D rb;
    private float lastCollisionTime; // Thời gian va chạm cuối cùng
    private bool isStuck; // Trạng thái bị kẹt
    private float lastEscapeTime; // Thời gian thoát kẹt cuối cùng
    private Collider2D robotCollider; // Collider của robot
    private bool isStopped; // Trạng thái dừng sau va chạm
    private float stopStartTime; // Thời gian bắt đầu dừng
    private Quaternion targetRotation; // Góc xoay mục tiêu
    private Vector2 lastPosition; // Vị trí trước đó để theo dõi di chuyển
    private float stuckTimer; // Bộ đếm thời gian kiểm tra bị kẹt
    private Vector2 lastCollisionNormal; // Pháp tuyến va chạm gần nhất
    private Vector2 surfaceStartPosition; // Vị trí bắt đầu di chuyển dọc bề mặt
    private bool isFollowingSurface; // Trạng thái di chuyển dọc bề mặt

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        robotCollider = GetComponent<Collider2D>();
        lastPosition = transform.position;
        // Đặt hướng ban đầu ngẫu nhiên
        direction = Random.insideUnitCircle.normalized;
    }

    void FixedUpdate()
    {
        // Kiểm tra xem robot có bị kẹt không
        if (!isStopped)
        {
            stuckTimer += Time.fixedDeltaTime;
            if (stuckTimer >= stuckCheckInterval)
            {
                if (Vector2.Distance(transform.position, lastPosition) < minMoveDistance)
                {
                    ChangeDirection();
                }
                lastPosition = transform.position;
                stuckTimer = 0f;
            }

            // Kiểm tra di chuyển dọc bề mặt
            if (isFollowingSurface)
            {
                float distanceAlongSurface = Vector2.Distance(transform.position, surfaceStartPosition);
                if (distanceAlongSurface >= maxSurfaceDistance)
                {
                    ChangeDirection();
                    isFollowingSurface = false;
                }
            }
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
                isFollowingSurface = false; // Kết thúc theo dõi bề mặt
            }
            else
            {
                // Xoay body từ từ về hướng đi mới
                if (direction != Vector2.zero)
                {
                    float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                    targetRotation = Quaternion.Euler(0f, 0f, angle);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * rotationSpeed);
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

        // Lưu pháp tuyến va chạm gần nhất
        lastCollisionNormal = collision.contacts[0].normal;

        // Phản xạ hướng dựa trên pháp tuyến va chạm
        direction = Vector2.Reflect(direction, lastCollisionNormal).normalized;

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
        surfaceStartPosition = transform.position; // Đặt vị trí bắt đầu dọc bề mặt
        isFollowingSurface = true; // Bắt đầu theo dõi di chuyển dọc bề mặt
    }

    private void ChangeDirection()
    {
        isStopped = true;
        stopStartTime = Time.time; // Đặt lại thời gian dừng
        direction = Random.insideUnitCircle.normalized; // Thay đổi hướng ngẫu nhiên
        isFollowingSurface = false; // Dừng theo dõi bề mặt khi thay đổi hướng
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
        // Quét 8 hướng xung quanh robot, ưu tiên hướng vuông góc với pháp tuyến va chạm
        float bestDistance = 0f;
        Vector2 bestDirection = direction;

        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f * Mathf.Deg2Rad;
            Vector2 testDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)).normalized;
            // Ưu tiên hướng có góc lớn với pháp tuyến va chạm (tránh song song)
            if (lastCollisionNormal != Vector2.zero)
            {
                float dotProduct = Vector2.Dot(testDirection, lastCollisionNormal);
                if (Mathf.Abs(dotProduct) < 0.5f) // Ưu tiên hướng không quá song song (góc > 60 độ)
                {
                    RaycastHit2D hit = Physics2D.Raycast(transform.position, testDirection, raycastDistance);
                    if (hit.collider == null || hit.distance > bestDistance)
                    {
                        bestDistance = hit.distance;
                        bestDirection = testDirection;
                    }
                }
            }
        }

        // Nếu không tìm được hướng tốt, quét lại tất cả
        if (bestDistance == 0f)
        {
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
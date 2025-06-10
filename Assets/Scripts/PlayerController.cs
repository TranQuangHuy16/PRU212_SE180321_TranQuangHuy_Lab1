using UnityEngine;

public class PlayerController : MonoBehaviour
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
        // Nếu bị kẹt, xử lý thoát kẹt
        if (isStuck && Time.time - lastEscapeTime > stuckCooldown)
        {
            EscapeStuckSituation();
            lastEscapeTime = Time.time;
        }

        // Nếu đang dừng
        if (isStopped)
        {
            rb.linearVelocity = Vector2.zero;

            if (Time.time - stopStartTime >= stopDuration)
            {
                isStopped = false;
            }
            else
            {
                RotateSmoothly();
                return;
            }
        }

        // Nếu sắp va chạm, chuyển hướng thông minh
        if (IsObstacleAhead(direction))
        {
            direction = FindEscapeDirection();
        }

        // Di chuyển bình thường
        rb.linearVelocity = direction * speed;
        RotateSmoothly();
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        lastCollisionTime = Time.time;

        Vector2 normal = collision.contacts[0].normal;
        direction = Vector2.Reflect(direction, normal).normalized;

        if (IsObstacleAhead(direction))
        {
            direction = FindEscapeDirection();
        }

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
        float bestDistance = 0f;
        Vector2 bestDir = direction;

        // Quét 36 hướng (mỗi 10 độ)
        for (int i = 0; i < 36; i++)
        {
            float angle = i * 10f * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)).normalized;
            RaycastHit2D hit = Physics2D.Raycast(transform.position, dir, raycastDistance);

            if (hit.collider == null)
            {
                return dir; // Hướng an toàn hoàn toàn
            }
            else if (hit.distance > bestDistance)
            {
                bestDistance = hit.distance;
                bestDir = dir;
            }
        }

        return bestDir;
    }

    private System.Collections.IEnumerator ReEnableCollision(Collider2D collider, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (collider != null && robotCollider != null)
        {
            Physics2D.IgnoreCollision(robotCollider, collider, false);
        }
    }

    private bool IsObstacleAhead(Vector2 dir)
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, dir, raycastDistance);
        return hit.collider != null;
    }

    private void RotateSmoothly()
    {
        if (direction != Vector2.zero)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            targetRotation = Quaternion.Euler(0f, 0f, angle);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }
    }

}
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class OffroadCarController : MonoBehaviour
{
    [Header("基本走行性能")]
    public float acceleration = 35f;
    public float boostMultiplier = 1.6f; // Shiftキー加速の倍率
    public float turnSpeed = 120f;
    public float gripStrength = 4.5f;

    [Header("オフロード・空中制御")]
    public float groundCheckDistance = 1.2f;
    public float airStabilizeSpeed = 3f;

    private Rigidbody rb;
    private bool isGrounded;
    private Vector3 groundNormal = Vector3.up;
    private float terrainSpeedMult = 1f;
    private float terrainGripMult = 1f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        // 横転しにくいよう重心を低く設定
        rb.centerOfMass = new Vector3(0f, -0.6f, 0f);
    }

    void Update()
    {
        // Rキーで車の姿勢を水平にリセット（横転・スタック対策）
        if (Input.GetKeyDown(KeyCode.R))
        {
            transform.position += Vector3.up * 1.5f;
            transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    void FixedUpdate()
    {
        CheckGround();

        float moveInput = Input.GetAxis("Vertical");
        float turnInput = Input.GetAxis("Horizontal");
        bool isBoosting = Input.GetKey(KeyCode.LeftShift) && moveInput > 0f;

        if (isGrounded)
        {
            // 坂道の傾斜に沿った前進方向へ加速
            Vector3 slopeForward = Vector3.ProjectOnPlane(transform.forward, groundNormal).normalized;
            float currentAccel = acceleration * terrainSpeedMult * (isBoosting ? boostMultiplier : 1f);
            rb.AddForce(slopeForward * moveInput * currentAccel, ForceMode.Acceleration);

            // 傾斜に沿った横滑り抑制（グリップ力）
            Vector3 slopeRight = Vector3.ProjectOnPlane(transform.right, groundNormal).normalized;
            Vector3 lateralVelocity = slopeRight * Vector3.Dot(rb.linearVelocity, slopeRight);
            rb.AddForce(-lateralVelocity * gripStrength * terrainGripMult, ForceMode.Acceleration);

            // 走行中のみステアリング操作を有効化
            if (rb.linearVelocity.magnitude > 1f)
            {
                float reverseSign = Vector3.Dot(rb.linearVelocity, transform.forward) >= 0 ? 1f : -1f;
                float turn = turnInput * turnSpeed * reverseSign * Time.fixedDeltaTime;
                rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, turn, 0f));
            }
        }
        else
        {
            // ジャンプ中は空中でひっくり返らないよう水平姿勢へ緩やかに補正
            Quaternion targetUpright = Quaternion.FromToRotation(transform.up, Vector3.up) * rb.rotation;
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetUpright, airStabilizeSpeed * Time.fixedDeltaTime));
        }
    }

    void CheckGround()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, groundCheckDistance))
        {
            isGrounded = true;
            groundNormal = hit.normal;
        }
        else
        {
            isGrounded = false;
            groundNormal = Vector3.up;
        }
    }

    // 泥濘（Mud）エリアに入った時の減速・スリップ判定
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Mud"))
        {
            terrainSpeedMult = 0.55f;
            terrainGripMult = 0.35f;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Mud"))
        {
            terrainSpeedMult = 1f;
            terrainGripMult = 1f;
        }
    }
}
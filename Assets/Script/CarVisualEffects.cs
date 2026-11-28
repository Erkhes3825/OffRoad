using UnityEngine;

public class CarVisualEffects : MonoBehaviour
{
	[Header("車体ボディ（傾き演出用）")]
	[Tooltip("PlayerCarの子になっている車モデルのルートを指定")]
	public Transform carBody;

	[Header("タイヤのTransform（4輪）")]
	public Transform frontLeftWheel;
	public Transform frontRightWheel;
	public Transform rearLeftWheel;
	public Transform rearRightWheel;

	[Header("タイヤ回転・ステアリング設定")]
	public float wheelRadius = 0.4f;
	public float maxSteerAngle = 30f;
	public float steerSmooth = 10f;

	[Header("サスペンション（車体の傾き）設定")]
	public float maxRollAngle = 6f;   // カーブ時の横傾き（ロール）最大角度
	public float maxPitchAngle = 4f;  // 加減速時の前後傾き（ピッチ）最大角度
	public float bodyTiltSmooth = 8f;

	private Rigidbody rb;
	private float currentSteerAngle = 0f;
	private float frontWheelSpin = 0f;
	private Quaternion initialBodyRotation;

	void Start()
	{
		rb = GetComponent<Rigidbody>();
		if (carBody != null)
		{
			initialBodyRotation = carBody.localRotation;
		}
	}

	void Update()
	{
		if (rb == null) return;

		// ローカル空間での前後速度（Z）と横滑り速度（X）を取得
		Vector3 localVelocity = transform.InverseTransformDirection(rb.linearVelocity);
		float forwardSpeed = localVelocity.z;
		float lateralSpeed = localVelocity.x;

		float moveInput = Input.GetAxis("Vertical");
		float turnInput = Input.GetAxis("Horizontal");

		// 1. タイヤの回転・ステアリング更新
		UpdateWheels(forwardSpeed, turnInput);

		// 2. 車体ボディの傾き（ロール＆ピッチ）更新
		UpdateBodyTilt(forwardSpeed, lateralSpeed, moveInput, turnInput);
	}

	void UpdateWheels(float forwardSpeed, float turnInput)
	{
		// 速度から回転角度（度/秒）を算出
		float wheelCircumference = 2f * Mathf.PI * wheelRadius;
		float spinDelta = (forwardSpeed / wheelCircumference) * 360f * Time.deltaTime;

		// 後輪は回転のみ
		if (rearLeftWheel != null) rearLeftWheel.Rotate(Vector3.right, spinDelta, Space.Self);
		if (rearRightWheel != null) rearRightWheel.Rotate(Vector3.right, spinDelta, Space.Self);

		// 前輪は「ステアリング（Y軸）」と「タイヤ回転（X軸）」を合成してジンバルロックを防止
		frontWheelSpin = (frontWheelSpin + spinDelta) % 360f;
		float targetSteer = turnInput * maxSteerAngle;
		currentSteerAngle = Mathf.Lerp(currentSteerAngle, targetSteer, steerSmooth * Time.deltaTime);

		Quaternion frontRot = Quaternion.Euler(frontWheelSpin, currentSteerAngle, 0f);
		if (frontLeftWheel != null) frontLeftWheel.localRotation = frontRot;
		if (frontRightWheel != null) frontRightWheel.localRotation = frontRot;
	}

	void UpdateBodyTilt(float forwardSpeed, float lateralSpeed, float moveInput, float turnInput)
	{
		if (carBody == null) return;

		// カーブやドリフト時の横傾き（Z軸回転）：右に曲がると左へ傾く
		float speedFactor = Mathf.Clamp01(Mathf.Abs(forwardSpeed) / 10f);
		float targetRoll = (turnInput * speedFactor * maxRollAngle) + (lateralSpeed * 0.4f);
		targetRoll = Mathf.Clamp(targetRoll, -maxRollAngle * 1.5f, maxRollAngle * 1.5f);

		// 加速時に後ろへ沈み（マイナスX回転）、ブレーキ・バック時に前へつんのめる（プラスX回転）
		float targetPitch = -moveInput * maxPitchAngle;

		Quaternion tiltRotation = Quaternion.Euler(targetPitch, 0f, targetRoll);
		carBody.localRotation = Quaternion.Slerp(
			carBody.localRotation,
			initialBodyRotation * tiltRotation,
			bodyTiltSmooth * Time.deltaTime
		);
	}
}
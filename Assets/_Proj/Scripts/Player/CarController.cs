using System.Runtime.CompilerServices;
using UnityEngine;

public class CarController : MonoBehaviour
{
  #region References
  [Header("References")]
  [SerializeField] private Rigidbody carRB;
  [SerializeField] private Transform[] rayPoints;
  [SerializeField] private LayerMask drivable;
  [SerializeField] private Transform accelerationPoint;
  #endregion

  #region Suspension
  [Header("Suspension Settings")]
  [SerializeField] private float springStiffness = 30000f;
  [SerializeField] private float damperStiffness = 3000f;
  [SerializeField] private float restLength = 1f;
  [SerializeField] private float springTravel = 0.5f;
  [SerializeField] private float wheelRadius = 0.33f;
  #endregion

  #region Drive & Steering
  [Header("Drive / Steering")]
  [SerializeField] private float acceleration = 25f;
  [SerializeField] private float maxSpeed = 100f;
  [SerializeField] private float coastDrag = 2.0f;           // 입력 없음: 전/후 감쇠
  [SerializeField] private float lateralFriction = 6.0f;     // 좌/우(횡) 감쇠
  [SerializeField] private float longitudinalFriction = 0.8f;// 전/후(구름) 감쇠

  [SerializeField] private float steerTorque = 120f;         // 조향 토크(작게 시작)
  [SerializeField] private float minSteerSpeed = 1.0f;       // 너무 느리면 조향X
  #endregion
  [SerializeField] float maxSteerAngle = 25f;      // 최대 조향각(도)
  [SerializeField] float steerSpeedFalloff = 0.06f;// 속도 높을수록 조향 감소
  [SerializeField] float cornerStiffnessFront = 9f;// 앞바퀴 횡력 계수(점성형)
  [SerializeField] float cornerStiffnessRear = 7f;// 뒷바퀴 횡력 계수
  [SerializeField] float muLat = 1.2f;             // 횡 마찰 상한(마찰원)
  [SerializeField] float muLong = 1.0f;            // 종 마찰 상한
  [SerializeField] float yawDamping = 1.0f;        // 아주 약한 요 감쇠

  #region Debug
  [Header("Debug Draw")]
  [SerializeField] private bool debugDraw = true;
  [SerializeField] private float debugForceScale = 0.01f;
  const int LINES_PER_WHEEL = 4; // ray, Fn, Flat, Flong
  Vector3[] dbgStart, dbgEnd;
  Color[] dbgColor;
  #endregion

  // State / Input
  int[] wheelsIsGrounded;
  bool isGrounded;
  float moveInput, steerInput;

  void Awake()
  {
    if (!carRB) carRB = GetComponent<Rigidbody>();

    int n = rayPoints != null ? rayPoints.Length : 0;
    wheelsIsGrounded = new int[n];

    int totalLines = n * LINES_PER_WHEEL;
    dbgStart = new Vector3[totalLines];
    dbgEnd = new Vector3[totalLines];
    dbgColor = new Color[totalLines];
  }

  void Update()
  {
    // 입력은 Update에서
    steerInput = Input.GetAxis("Horizontal");
    moveInput = Input.GetAxis("Vertical");

    // 디버그 라인은 렌더 타이밍에
    if (!debugDraw) return;
    for (int i = 0; i < dbgStart.Length; i++)
      Debug.DrawLine(dbgStart[i], dbgEnd[i], dbgColor[i]);
  }

  void FixedUpdate()
  {
    Suspension();
    isGrounded = GroundedWheelCount() > 1;
    MovePhysics();
  }

  #region Physics Motion
  void MovePhysics()
  {
    if (!isGrounded) return;

    var up = carRB.transform.up;
    var fwd = carRB.transform.forward;
    var right = carRB.transform.right;

    Vector3 v = carRB.velocity;
    float fwdSpeed = Vector3.Dot(v, fwd);
    float speedAbs = Mathf.Abs(fwdSpeed);

    if (speedAbs > minSteerSpeed)
    {
      float steerScale = 1f / (1f + speedAbs * steerSpeedFalloff);
      float reverseSign = (fwdSpeed < -minSteerSpeed) ? -1f : 1f;
      float groundedFact = Mathf.Clamp01(GroundedWheelCount() / (float)rayPoints.Length);

      carRB.AddTorque(up * steerInput * steerTorque * steerScale * reverseSign * groundedFact,
                      ForceMode.Acceleration);
    }

    if (Mathf.Abs(moveInput) > 0.01f)
      carRB.AddForce(fwd * (acceleration * moveInput), ForceMode.Acceleration);
    else
    {
      // 입력 없을 때 평면 성분만 부드럽게 감속
      Vector3 vPlanar = Vector3.ProjectOnPlane(carRB.velocity, up);
      carRB.AddForce(-vPlanar * coastDrag, ForceMode.Acceleration);
    }
    float wy = Vector3.Dot(carRB.angularVelocity, up);
    carRB.AddTorque(-wy * yawDamping * up, ForceMode.Acceleration);

    if (fwdSpeed > maxSpeed)
    {
      Vector3 vSideKeep = v - Vector3.Project(v, fwd);
      carRB.velocity = fwd * maxSpeed + vSideKeep;
    }
  }
  #endregion

  #region Suspension
  void Suspension()
  {
    if (rayPoints == null || rayPoints.Length == 0) return;

    float maxLen = restLength + springTravel;
    Vector3 up = carRB.transform.up;

    // 속도에 따라 유효 조향각
    float speed = Vector3.ProjectOnPlane(carRB.velocity, up).magnitude;
    float steerAngle = (maxSteerAngle / (1f + speed * steerSpeedFalloff)) * steerInput;

    for (int i = 0; i < rayPoints.Length; i++)
    {
      int baseIdx = i * LINES_PER_WHEEL;
      var origin = rayPoints[i].position;

      if (Physics.Raycast(origin, -up, out var hit, maxLen + wheelRadius, drivable, QueryTriggerInteraction.Ignore))
      {
        wheelsIsGrounded[i] = 1;

        float currLen = Mathf.Max(0f, hit.distance - wheelRadius);
        float x = Mathf.Clamp(restLength - currLen, -springTravel, springTravel);

        // 법선/표면 축
        Vector3 n = hit.normal;
        Vector3 fwdSurf = Vector3.ProjectOnPlane(carRB.transform.forward, n).normalized;

        // 앞바퀴만 조향각 적용
        bool isFront = (i < 2);
        Vector3 wheelFwd = isFront ? (Quaternion.AngleAxis(steerAngle, n) * fwdSurf) : fwdSurf;
        Vector3 wheelRight = Vector3.Cross(n, wheelFwd).normalized;

        // 휠 접점 속도 분해
        Vector3 vP = carRB.GetPointVelocity(origin);
        float vN = Vector3.Dot(vP, n);           // 법선
        float vLongW = Vector3.Dot(vP, wheelFwd);    // 바퀴 종
        float vLatW = Vector3.Dot(vP, wheelRight);  // 바퀴 횡

        // 스프링/댐퍼 (법선 힘 음수 금지)
        float Fspring = springStiffness * x;
        float Fdamper = damperStiffness * vN;
        float Fn = Mathf.Max(0f, Fspring - Fdamper);

        // 횡력(코너링): 속도비례 간이 모델 + 마찰원 제한
        float C = isFront ? cornerStiffnessFront : cornerStiffnessRear;
        float Fy = -vLatW * C;
        float FyMax = muLat * Fn;
        Fy = Mathf.Clamp(Fy, -FyMax, FyMax);

        // 종 마찰(구름 저항): 너무 크면 가속 죽음 → 소량 + 제한
        float Fx = -vLongW * longitudinalFriction;
        float FxMax = muLong * Fn;
        Fx = Mathf.Clamp(Fx, -FxMax, FxMax);

        Vector3 F = n * Fn + wheelRight * Fy + wheelFwd * Fx;
        carRB.AddForceAtPosition(F, origin, ForceMode.Force);

        if (debugDraw)
        {
          dbgStart[baseIdx + 0] = origin;
          dbgEnd[baseIdx + 0] = hit.point;
          dbgColor[baseIdx + 0] = Color.red;

          dbgStart[baseIdx + 1] = origin;
          dbgEnd[baseIdx + 1] = origin + (n * Fn) * debugForceScale;
          dbgColor[baseIdx + 1] = Color.cyan;

          dbgStart[baseIdx + 2] = origin;
          dbgEnd[baseIdx + 2] = origin + (wheelRight * Fy) * debugForceScale;
          dbgColor[baseIdx + 2] = Color.yellow;

          dbgStart[baseIdx + 3] = origin;
          dbgEnd[baseIdx + 3] = origin + (wheelFwd * Fx) * debugForceScale;
          dbgColor[baseIdx + 3] = Color.magenta;
        }
      }
      else
      {
        wheelsIsGrounded[i] = 0;
        if (debugDraw)
        {
          dbgStart[baseIdx + 0] = origin;
          dbgEnd[baseIdx + 0] = origin + (-up) * (wheelRadius + maxLen);
          dbgColor[baseIdx + 0] = Color.green;
          for (int k = 1; k < LINES_PER_WHEEL; k++)
          {
            dbgStart[baseIdx + k] = origin;
            dbgEnd[baseIdx + k] = origin;
            dbgColor[baseIdx + k] = Color.clear;
          }
        }
      }
    }
  }

  #endregion

  #region Helpers
  int GroundedWheelCount()
  {
    int cnt = 0;
    for (int i = 0; i < wheelsIsGrounded.Length; i++) cnt += wheelsIsGrounded[i];
    return cnt;
  }
  #endregion
}

using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class CarController : MonoBehaviour
{
  [Header("References")]
  [SerializeField] private Rigidbody carRB;
  [SerializeField] private Transform[] rayPoints;
  [SerializeField] private LayerMask drivable;
  [SerializeField] private Transform accelerationPoint;

  [Header("suspension Settings")]
  [SerializeField] private float springStiffness; // 수축/이완 시 적용되는 힘
  [SerializeField] private float damperStiffness;
  [SerializeField] private float restLength; // 압축되지 않은 스프링 길이
  [SerializeField] private float springTravel; // 스프링 수축/이완 최대 거리
  [SerializeField] private float wheelRadius;

  private int[] wheelsIsGrounded = new int[4];
  private bool isGrounded = false;

  [Header("Input")]
  private float moveInput = 0;
  private float steerInput = 0;

  [Header("Car Settings")]
  [SerializeField] private float acceleration = 25f;
  [SerializeField] private float maxSpeed = 100f;
  [SerializeField] private float deceleration = 10f;

  private Vector3 currCarLocalVelocity = Vector3.zero;
  private float carVelocityRatio = 0;

  [SerializeField] float coastDrag = 2.0f;   // 입력 없을 때 전진 성분 감쇠
  [SerializeField] float sideGrip = 6.0f;   // 타이어 측면 감쇠(가로 마찰)
  [SerializeField] float tireFriction = 8f;   // 접선(앞/옆) 감쇠
  [SerializeField] bool debugDraw = true;
  [SerializeField] float debugForceScale = 0.01f; 

  const int RAY_DRAWLINES_PER_WHEEL = 4;
  Vector3[] dbgStart, dbgEnd;
  Color[] dbgColor;
  
  void Awake()
  {
    int total = (rayPoints != null ? rayPoints.Length : 0) * RAY_DRAWLINES_PER_WHEEL;
    dbgStart = new Vector3[total];
    dbgEnd = new Vector3[total];
    dbgColor = new Color[total];
  }
  void Start()
  {
    carRB = GetComponent<Rigidbody>();
  }
  void Update()
  {
    GetPlayerInput();
    //Movement();
    if (!debugDraw) return;
    // 렌더 타이밍에 그리기(Interpolate 어긋남 방지)
    for (int i = 0; i < dbgStart.Length; i++)
      Debug.DrawLine(dbgStart[i], dbgEnd[i], dbgColor[i]);
  }

  void FixedUpdate()
  {
    Suspension();
    GroundCheck();
    CaculateCarVelocity();
    MovePhysics();
    //ApplyRestSnap();
  }
  void ApplyRestSnap()
  {
    if (!isGrounded || Mathf.Abs(moveInput) > 0.01f) return;

    Vector3 up = carRB.transform.up;
    Vector3 planar = Vector3.ProjectOnPlane(carRB.velocity, up);

    if (planar.sqrMagnitude < 1e-3f)
    {
      carRB.velocity -= planar;
      carRB.angularVelocity = Vector3.zero;
    }
  }
  void MovePhysics()
  {
    if (!isGrounded) return;

    var fwd = carRB.transform.forward;
    var right = carRB.transform.right;
    var up = carRB.transform.up;

    float fwdSpeed = Vector3.Dot(carRB.velocity, fwd);

    // 가속/후진
    if (Mathf.Abs(moveInput) > 0.01f)
      carRB.AddForce(fwd * (acceleration * moveInput), ForceMode.Acceleration);
    else
    {
      // 입력 없을 때 전진 감쇠
      Vector3 vFwd = Vector3.Project(carRB.velocity, fwd);
      carRB.AddForce(-vFwd * coastDrag, ForceMode.Acceleration);
    }

    // 측면 그립: 옆으로 미는 속도를 줄여서 파란 화살표 현상 제거
    Vector3 vSide = Vector3.Project(carRB.velocity, right);
    carRB.AddForce(-vSide * sideGrip, ForceMode.Acceleration);

    // 전진 속도 제한
    if (fwdSpeed > maxSpeed)
    {
      Vector3 vOnlyFwd = fwd * maxSpeed;
      Vector3 vSideKeep = carRB.velocity - Vector3.Project(carRB.velocity, fwd);
      carRB.velocity = vOnlyFwd + vSideKeep;
    }
  }
  #region Movement
  void Movement()
  {
    if (isGrounded)
    {
      if (Mathf.Abs(moveInput) > 0.1f)
        Acceleration();
      else
        Deceleration();
    }
  }
  void Acceleration()
  {
    carRB.AddForceAtPosition(acceleration * moveInput * transform.forward, accelerationPoint.position, ForceMode.Acceleration);
  }

  void Deceleration()
  {
    carRB.AddForceAtPosition(acceleration * moveInput * -transform.forward, accelerationPoint.position, ForceMode.Acceleration);
  }
  #endregion

  #region Car Status Check
  void GroundCheck()
  {
    int tempGroundedWheels = 0;
    for (int i = 0; i < wheelsIsGrounded.Length; i++)
    {
      tempGroundedWheels += wheelsIsGrounded[i];
    }

    if (tempGroundedWheels > 1)
    {
      isGrounded = true;
    }
    else
    {
      isGrounded = false;
    }
  }

  void CaculateCarVelocity()
  {
    if (isGrounded)
    {
      currCarLocalVelocity = transform.InverseTransformDirection(carRB.velocity);
      carVelocityRatio = currCarLocalVelocity.z / maxSpeed;
    }
  }
  #endregion

  #region Input Handling
  void GetPlayerInput()
  {
    steerInput = Input.GetAxis("Horizontal");
    moveInput = Input.GetAxis("Vertical");
  }
  #endregion

  #region suspension Functions
  void Suspension()
  {
    float maxLen = restLength + springTravel;
    Vector3 up = carRB.transform.up;

    for (int i = 0; i < rayPoints.Length; i++)
    {
      int baseIdx = i * RAY_DRAWLINES_PER_WHEEL;
      var origin = rayPoints[i].position;

      if (Physics.Raycast(origin, -up, out var hit, maxLen + wheelRadius, drivable, QueryTriggerInteraction.Ignore))
      {
        wheelsIsGrounded[i] = 1;

        float currLen = Mathf.Max(0f, hit.distance - wheelRadius);
        float x = Mathf.Clamp(restLength - currLen, -springTravel, springTravel);

        Vector3 n = hit.normal;
        Vector3 fwdOnSurface = Vector3.ProjectOnPlane(carRB.transform.forward, n).normalized;
        Vector3 rightOnSurf = Vector3.Cross(n, fwdOnSurface).normalized;

        Vector3 vP = carRB.GetPointVelocity(origin);
        float vN = Vector3.Dot(vP, n);
        float vLong = Vector3.Dot(vP, fwdOnSurface);
        float vLat = Vector3.Dot(vP, rightOnSurf);

        float Fspring = springStiffness * x;
        float Fdamper = damperStiffness * vN;

        // 최종 힘
        Vector3 F =
            n * (Fspring - Fdamper)              // 법선(스프링/댐퍼)
          - rightOnSurf * (vLat * sideGrip)  // 좌/우 감쇠
          - fwdOnSurface * (vLong * tireFriction); // 전/후 감쇠

        carRB.AddForceAtPosition(F, origin, ForceMode.Force);
        // [0] 레이: origin -> hit.point (빨강)
        dbgStart[baseIdx + 0] = origin;
        dbgEnd[baseIdx + 0] = hit.point;
        dbgColor[baseIdx + 0] = Color.red;

        // [1] 법선 힘 벡터 (시안)
        Vector3 Fnorm = n * (Fspring - Fdamper) * 0.001f;
        dbgStart[baseIdx + 1] = origin;
        dbgEnd[baseIdx + 1] = origin + Fnorm;
        dbgColor[baseIdx + 1] = Color.cyan;

        // [2] 세로 마찰 벡터 (노랑)
        Vector3 Flat = -rightOnSurf * (vLat * sideGrip) * debugForceScale;
        dbgStart[baseIdx + 2] = origin;
        dbgEnd[baseIdx + 2] = origin + Flat;
        dbgColor[baseIdx + 2] = Color.yellow;

        // [3] 가로 마찰 벡터 (마젠타)
        Vector3 Flong = -fwdOnSurface * (vLong * tireFriction) * debugForceScale;
        dbgStart[baseIdx + 3] = origin;
        dbgEnd[baseIdx + 3] = origin + Flong;
        dbgColor[baseIdx + 3] = Color.magenta;
      }
      else
      {
        wheelsIsGrounded[i] = 0;

        // 지면 접촉 안 하면: 아래로 최대 길이 표시(초록)
        dbgStart[baseIdx + 0] = origin;
        dbgEnd[baseIdx + 0] = origin + (-up) * (wheelRadius + maxLen);
        dbgColor[baseIdx + 0] = Color.green;

        // 나머지 라인은 0 길이로
        for (int k = 1; k < RAY_DRAWLINES_PER_WHEEL; k++)
        {
          dbgStart[baseIdx + k] = origin;
          dbgEnd[baseIdx + k] = origin;
          dbgColor[baseIdx + k] = Color.clear;
        }
      }
    }
  }
  #endregion
}

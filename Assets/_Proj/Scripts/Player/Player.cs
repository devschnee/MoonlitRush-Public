using System;
using UnityEngine;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using UnityEditor.SceneTemplate;

public class Player : MonoBehaviour
{
  public BaseInput inputSource;
  public Stats stats;
  public Rigidbody rb;
  public Transform CenterOfMass;

  WheelFrictionCurve defaultSidewaysFriction;

  public InputData input { get; private set; }
  public float airPercent { get; private set; }
  public float groundPercent { get; private set; }
  public List<GameObject> visualWheelsMesh;

  [Header("WheelColliders")]
  public WheelCollider flWheel;
  public WheelCollider frWheel;
  public WheelCollider rlWheel;
  public WheelCollider rrWheel;

  [Header("VisualSteering")]
  [Range(1.0f, 20.0f), Tooltip("스티어링이 얼마나 빠르게 반응하는지 조절")]
  public float steerSpeed = 10.0f;

  [Range(0.0f, 20.0f)]
  public float airborneReorientation = 5f;

  [Header("Drift")]
  [Range(0.01f, 1.0f)]
  public float driftGrip = 0.4f;
  [Range(0.0f, 10.0f)]
  public float addDriftSteer = 5.0f;
  [Range(1.0f, 30.0f)]
  public float inAngleToFinishDrift = 10.0f;
  [Range(0.01f, 0.99f)]
  public float minSpeedPercentToFinishDrift = 0.5f;
  [Range(1.0f, 20.0f)]
  public float driftControl = 10.0f;
  [Range(0.0f, 20.0f)]
  public float driftDampening = 10.0f;
  public LayerMask groundLayers = Physics.DefaultRaycastLayers;

  [Header("Booster")]
  public float boosterSpeedAdd;
  public float boosterAccelAdd;
  public float boosterDuration;
  
  private float currBoosterTime;
  private Stats originalStats;

  public bool wantsToDrift { get; private set; } = false;
  public bool isDrifting { get; private set; } = false;
  float currGrip = 1.0f;
  float driftTurningPower = 0.0f;
  float preGroundPercent = 1.0f;
  bool canMove = true;
  List<PowerUpEffect> activePowerupList = new List<PowerUpEffect>();
  Stats finalStats;

  void Awake()
  {
    rb = GetComponent<Rigidbody>();
    if (inputSource == null)
      inputSource = GetComponent<BaseInput>();

    finalStats = new Stats(stats);
    originalStats = new Stats(stats);
    currGrip = stats.grip;
    defaultSidewaysFriction = flWheel.sidewaysFriction;
  }

  void FixedUpdate()
  {
    input = inputSource.GenerateInput();
    stats.currSpeed = rb.velocity.magnitude;
    rb.centerOfMass = transform.InverseTransformPoint(CenterOfMass.position);

    wantsToDrift = Input.GetKey(KeyCode.W) && (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D)) && Vector3.Dot(rb.velocity, transform.forward) > 0;

    
    ApplyPowerups();

    ApplySpeedUp();

    // 물리 조작
    if (canMove)
    {
      ApplyMotorAndBrake();
      ApplySteering();
    }

    // 휠 접지 상태 확인
    UpdateGroundedState();

    // 공중 상태 처리
    GroundAirbourne();

    if(wantsToDrift)
      HandleDrift();

    // 시각적 휠 업데이트
    UpdateVisualWheels();
  }

  void ApplyMotorAndBrake()
  {
    if (rb == null || rlWheel == null || finalStats == null) return; 
    
    float accelInput = (input.Accelerate ? 1.0f : 0.0f) - (input.Brake || input.Reverse ? 1.0f : 0.0f);
    bool isBraking = (Vector3.Dot(rb.velocity, transform.forward) > 0 && accelInput < 0) || (Vector3.Dot(rb.velocity, transform.forward) < 0 && accelInput > 0);

    if (isBraking)
    {
      rlWheel.brakeTorque = finalStats.braking * 1000;
      rrWheel.brakeTorque = finalStats.braking * 1000;
      rlWheel.motorTorque = 0;
      rrWheel.motorTorque = 0;
    }
    else if (Mathf.Abs(accelInput) > 0.01f)
    {
      float motorForce = finalStats.acceleration * rb.mass;
      float motorTorque = motorForce / 2;
      rlWheel.motorTorque = motorTorque * accelInput;
      rrWheel.motorTorque = motorTorque * accelInput;
      rlWheel.brakeTorque = 0;
      rrWheel.brakeTorque = 0;
    }
    else
    {
      // 입력이 없으면 감속 위해 브레이크 토크 적용
      rlWheel.motorTorque = 0;
      rrWheel.motorTorque = 0;
      rlWheel.brakeTorque = finalStats.dragTime * 100;
      rrWheel.brakeTorque = finalStats.dragTime * 100;
    }
  }

  void ApplySteering()
  {
    float currentSteerAngle = flWheel.steerAngle;

    float speedFactor = Mathf.Clamp01(stats.currSpeed / finalStats.limitSpeed);
    float steerDamp = Mathf.Lerp(1f, 0.2f, speedFactor); // 고속일수록 덜 회전

    float targetSteerAngle = input.TurnInput * finalStats.steer * steerDamp;
    float newSteerAngle = Mathf.Lerp(currentSteerAngle, targetSteerAngle, Time.fixedDeltaTime * steerSpeed);

    flWheel.steerAngle = newSteerAngle;
    frWheel.steerAngle = newSteerAngle;

    // 실제 차 돌리기 위한 회전력 추가
    Vector3 angular = rb.angularVelocity;
    float targetAngularY = input.TurnInput * finalStats.steer * 0.5f;
    Debug.Log($"Angular target: {targetAngularY}, Steer: {finalStats.steer}");
    angular.y = Mathf.Lerp(angular.y, targetAngularY, Time.fixedDeltaTime * 0.1f); // 반응 속도
    rb.angularVelocity = angular;

  }
  void HandleDrift()
  {
    wantsToDrift = input.Brake && Vector3.Dot(rb.velocity, transform.forward) > 0f;

    if (groundPercent > 0.0f)
    {
      // 드리프트 시작
      if (!isDrifting && wantsToDrift)
      {
        isDrifting = true;

        // WheelFrictionCurve 설정
        WheelFrictionCurve driftFriction = flWheel.sidewaysFriction;
        driftFriction.stiffness = driftGrip;

        flWheel.sidewaysFriction = driftFriction;
        frWheel.sidewaysFriction = driftFriction;
        rlWheel.sidewaysFriction = driftFriction;
        rrWheel.sidewaysFriction = driftFriction;
      }
      // 드리프트 종료
      else if (isDrifting && !wantsToDrift)
      {
        isDrifting = false;

        // WheelFrictionCurve 원상 복구
        flWheel.sidewaysFriction = defaultSidewaysFriction;
        frWheel.sidewaysFriction = defaultSidewaysFriction;
        rlWheel.sidewaysFriction = defaultSidewaysFriction;
        rrWheel.sidewaysFriction = defaultSidewaysFriction;
      }
    }
    else
    {
      isDrifting = false;

      // 공중에 있을 때 마찰력 원상 복구
      flWheel.sidewaysFriction = defaultSidewaysFriction;
      frWheel.sidewaysFriction = defaultSidewaysFriction;
      rlWheel.sidewaysFriction = defaultSidewaysFriction;
      rrWheel.sidewaysFriction = defaultSidewaysFriction;
    }
  }

  void ApplySpeedUp()
  {
    //float elapsedTime = boosterDuration - currBoosterTime;
    //float t = Mathf.Clamp01(elapsedTime / boosterDuration);
    if(currBoosterTime > 0)
    {
      currBoosterTime -= Time.fixedDeltaTime;

      float t = 1.0f - (currBoosterTime / boosterDuration);
      finalStats.acceleration = originalStats.acceleration + boosterAccelAdd;

      float boostForce = Mathf.Lerp(0, boosterSpeedAdd, t);
      rb.AddRelativeForce(transform.forward * boostForce, ForceMode.Impulse);

      WheelFrictionCurve tmpFriction = flWheel.sidewaysFriction;
      tmpFriction.stiffness = Mathf.Lerp(defaultSidewaysFriction.stiffness, defaultSidewaysFriction.stiffness * 2.5f, t);
      flWheel.sidewaysFriction = tmpFriction;
      frWheel.sidewaysFriction = tmpFriction;
      rlWheel.sidewaysFriction = tmpFriction;
      rrWheel.sidewaysFriction = tmpFriction;

    }
    else
    {
      finalStats.acceleration = Mathf.Lerp(finalStats.acceleration, originalStats.acceleration, Time.fixedDeltaTime * 2.0f);
      flWheel.sidewaysFriction = defaultSidewaysFriction;
      frWheel.sidewaysFriction = defaultSidewaysFriction;
      rlWheel.sidewaysFriction = defaultSidewaysFriction;
      rrWheel.sidewaysFriction = defaultSidewaysFriction;
    }
  }

  void UpdateGroundedState()
  {
    int groundedCount = 0;
    if (flWheel.isGrounded) groundedCount++;
    if (frWheel.isGrounded) groundedCount++;
    if (rlWheel.isGrounded) groundedCount++;
    if (rrWheel.isGrounded) groundedCount++;

    groundPercent = (float)groundedCount / 4.0f;
    airPercent = 1 - groundPercent;
  }

  void GroundAirbourne()
  {
    if (airPercent >= 1)
    {
      rb.velocity += Physics.gravity * Time.fixedDeltaTime * finalStats.addedGravity;
    }
    if(airPercent > 0.5f)
    {
      Quaternion targetRot = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
      rb.MoveRotation(Quaternion.Slerp(transform.rotation, targetRot, Time.fixedDeltaTime * airborneReorientation));
    }
  }

  void ApplyPowerups()
  {
    activePowerupList.RemoveAll(p => p.elapsedTime > p.duration);
    Stats totalEffects = new Stats();
    for (int i = 0; i < activePowerupList.Count; i++)
    {
      activePowerupList[i].elapsedTime += Time.fixedDeltaTime;
      var effect = activePowerupList[i].modifier;
    }

    finalStats.grip = Mathf.Clamp(finalStats.grip, 0f, 1f);
  }

  void OnTriggerEnter(Collider other)
  {
    if (other.CompareTag("SpeedUp")) // 추후 태그 명 변경(Booster, Barrel 등)
    {
      Debug.Log("Trigger Detected");
      currBoosterTime = boosterDuration;

      //Vector3 boosterForce = transform.forward * boosterSpeedAdd * rb.mass;
      rb.AddForce(transform.forward * boosterSpeedAdd, ForceMode.Impulse);
    }
  }

  void UpdateVisualWheels()
  {
    UpdateWheelPose(frWheel, visualWheelsMesh[0].transform);
    UpdateWheelPose(flWheel, visualWheelsMesh[1].transform);
    UpdateWheelPose(rrWheel, visualWheelsMesh[2].transform);
    UpdateWheelPose(rlWheel, visualWheelsMesh[3].transform);
  }

  void UpdateWheelPose(WheelCollider collider, Transform wheelTransform)
  {
    Vector3 pos;
    Quaternion rot;
    collider.GetWorldPose(out pos, out rot);
    wheelTransform.position = pos;
    wheelTransform.rotation = rot;
    //if (!collider.motorTorque.Equals(0)) return;
    //float visualSpinSpeed = rb.velocity.magnitude * 2f;
    //wheelTransform.Rotate(Vector3.right, visualSpinSpeed * Time.deltaTime, Space.Self);
    //if (collider.motorTorque == 0)
    //{
    //  float visualSpinSpeed = rb.velocity.magnitude * 2f;
    //  wheelTransform.Rotate(Vector3.right, visualSpinSpeed * Time.deltaTime, Space.Self);
    //}

    if (collider == flWheel || collider == frWheel)
    {
      float visualSteerAngle = input.TurnInput * 20f;
      Quaternion steeringRot = Quaternion.Euler(0, visualSteerAngle, 0);

      wheelTransform.rotation = rot * steeringRot;
    }
    else
    {
      wheelTransform.rotation = rot;
    }
  }

  
  public void AddPowerup(PowerUpEffect powerUpEffect) => activePowerupList.Add(powerUpEffect);
  public void SetCanMove(bool move) => canMove = move;
  public float GetMaxSpeed() => Mathf.Max(finalStats.limitSpeed, finalStats.reverseSpeed);
  public void Reset()
  {
    Vector3 euler = transform.rotation.eulerAngles;
    euler.x = euler.z = 0f;
    transform.rotation = Quaternion.Euler(euler);
  }
}
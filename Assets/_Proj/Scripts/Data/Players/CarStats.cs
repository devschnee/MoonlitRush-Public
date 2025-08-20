using UnityEngine;

[CreateAssetMenu(fileName = "carStats", menuName = "CarStats", order = 0)]
public class CarStats : ScriptableObject
{
  [Header("Physics")]
  public float mass = 1000f;
  public float acceleration = 3f;
  public float maxSpeed = 20f;
  public float deceleration = 3f;

  [Header("Handling")]
  public float steerForce = 20f;
  public float angularDrag = 10f;
  public AnimationCurve turningCurve = AnimationCurve.Linear(0, 1, 1, 1);

  [Header("Gear Settings")]
  [Range(0.1f, 2f)]public float holdTopSpeed = 1f;

  [Header("Downforce")]
  public float baseDownforce = 1000f;
  public float downforcePerMS = 1f;

  [Header("Airbourne")]
  [Range(0.1f, 1)] public float airGravity = 0.6f;
  public float airGravityDuration = 0.6f;

  [Header("Accel Curve")]
  public AnimationCurve accelCurve = AnimationCurve.Linear(0, 1, 1, 1);

  [Header("Braking")]
  public float brakePow = 5f;
}

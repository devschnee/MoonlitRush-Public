using System;

[Serializable]
public class PowerUpEffect
{
  public float boostSpeed;
  public float boostAccel;
  public float driftSteer;
  public float barrelSpeed;
  public float barrelAccel;
  public float duration;
  public float elapsedTime;

  public Stats modifier;
}

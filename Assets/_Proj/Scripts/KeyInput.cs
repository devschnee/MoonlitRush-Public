using UnityEngine;

public struct InputData
{
  public bool Accelerate;
  public bool Brake;
  public float TurnInput;
}

public interface IInput
{
  InputData GenerateInput();
}


public abstract class KeyInput : MonoBehaviour, IInput
{
  public string TurnInputName = "Horizontal";
  public string AccelerateButtonName = "Accelerate";
  public string BrakeButtonName = "Brake";
  
  public InputData GenerateInput()
  {
    return new InputData
    {
      Accelerate = Input.GetButton(AccelerateButtonName),
      Brake = Input.GetButton(BrakeButtonName),
      TurnInput = Input.GetAxis("Horizontal")
    };
  }
}


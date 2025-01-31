using UnityEngine;

/**
 * Based on https://www.kinematicsoup.com/news/2016/8/9/rrypp5tkubynjwxhxjzd42s3o034o8
 * 
 * Tracks the delta between monitor refresh and simulation tick rate to provide
 * an interpolation factor that view code can use.
 * 
 * TODO: This is a general solution.  In our case, the simulator which owns the accumlator
 * should just expose the accumluator value explicitly as the interpolation factor, this
 * is overly complicated, but it works for now.
 */
public class InterpolationController {
  public static float InterpolationFactor { get; private set; } = 1f;

  private Ice.DoubleBuffer<float> timestampBuffer = new Ice.DoubleBuffer<float>();

  private float totalFixedTime;
  private float totalTime;

  public void ExplicitFixedUpdate(float dt) {
    totalFixedTime += dt;
    timestampBuffer.Push(totalFixedTime);
  }

  public void ExplicitUpdate(float dt) {
    totalTime += dt;

    float newTime = timestampBuffer.New();
    float oldTime = timestampBuffer.Old();

    if (newTime != oldTime) {
      InterpolationFactor = (totalTime - newTime) / (newTime - oldTime);
    } else {
      InterpolationFactor = 1;
    }
  }
}

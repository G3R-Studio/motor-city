using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
namespace UnityEngine.Rendering { public sealed class Stub {} }
namespace UnityEngine
{
    public static class RenderSettings { public static bool fog = true; }
    public sealed class Coroutine { internal IEnumerator Routine; }
    public class MonoBehaviour
    {
        public bool isActiveAndEnabled = true;
        private readonly List<Coroutine> active = new();
        public int Stops;
        public Coroutine StartCoroutine(IEnumerator routine)
        {
            var coroutine = new Coroutine { Routine = routine };
            if (routine.MoveNext()) active.Add(coroutine);
            return coroutine;
        }
        public void StopCoroutine(Coroutine coroutine) { Stops++; active.Remove(coroutine); }
        public void Tick()
        {
            foreach (Coroutine coroutine in active.ToArray())
                if (!coroutine.Routine.MoveNext()) active.Remove(coroutine);
        }
        public void Finish() { for (int i=0; i<30; i++) Tick(); }
    }
    public sealed class ReflectionProbe
    {
        public bool isActiveAndEnabled = true;
        public int Captures;
        private int remaining;
        public int RenderProbe() { Captures++; remaining=2; return Captures; }
        public bool IsFinishedRendering(int id) { return remaining-- <= 0; }
    }
}
public static class ReflectionCaptureChecks
{
    private static void Check(bool condition, string name) { if (!condition) throw new Exception(name); }
    public static void Run()
    {
        var runner = new MotorCity.World.CityReflectionProbeCaptureRunner();
        var first = new UnityEngine.ReflectionProbe();
        var discarded = new UnityEngine.ReflectionProbe();
        var latest = new UnityEngine.ReflectionProbe();
        runner.Capture(new[] { first });
        runner.Capture(new[] { discarded });
        runner.Capture(new[] { latest });
        runner.Finish();
        Check(first.Captures == 1 && discarded.Captures == 0 && latest.Captures == 1, "Active capture must finish and only latest pending request must run");
        Check(runner.Stops == 0, "Refresh must not cancel active GPU work");
        Check(UnityEngine.RenderSettings.fog, "Fog must be restored after queued captures");
        var snapshot = new List<UnityEngine.ReflectionProbe> { first };
        runner.Capture(snapshot); snapshot.Clear(); runner.Finish();
        Check(first.Captures == 2, "Mutable caller collection must be snapshotted");
        runner.Capture(new[] { latest });
        runner.Capture(new[] { discarded });
        typeof(MotorCity.World.CityReflectionProbeCaptureRunner).GetMethod("OnDisable", BindingFlags.Instance|BindingFlags.NonPublic).Invoke(runner,null);
        runner.Finish();
        Check(UnityEngine.RenderSettings.fog && discarded.Captures == 0, "Disable must clear pending work and restore fog");
        runner.isActiveAndEnabled = false;
        runner.Capture(new[] { discarded });
        runner.Capture(null);
        Check(discarded.Captures == 0, "Inactive/null requests must not capture");
    }
}

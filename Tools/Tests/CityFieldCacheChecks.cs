using System;
using System.Reflection;
public static class CityFieldCacheChecks
{
    private class BaseNetwork { private int waypoint = 5; public int Keep => waypoint; }
    private class Network : BaseNetwork { public float around = 2f; }
    public static void Run()
    {
        var inherited = MotorCity.World.AuthoredCityFieldCache.FindField(typeof(Network), "waypoint");
        if (inherited == null || inherited.DeclaringType != typeof(BaseNetwork)) throw new Exception("Inherited private field lookup changed");
        if (!object.ReferenceEquals(inherited, MotorCity.World.AuthoredCityFieldCache.FindField(typeof(Network), "waypoint"))) throw new Exception("Field cache did not reuse entry");
        if (MotorCity.World.AuthoredCityFieldCache.FindField(typeof(Network), "around") == null) throw new Exception("Public field lookup failed");
        if (MotorCity.World.AuthoredCityFieldCache.FindField(typeof(Network), "missing") != null || MotorCity.World.AuthoredCityFieldCache.FindField(null, "missing") != null || MotorCity.World.AuthoredCityFieldCache.FindField(typeof(Network), "") != null) throw new Exception("Missing/invalid lookup changed");
    }
}

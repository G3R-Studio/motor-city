using System;
using System.Collections.Generic;
namespace UnityEngine
{
    public class Texture { }
    public struct Vector2 { public float x,y; public Vector2(float x,float y){this.x=x;this.y=y;} }
    public struct Vector3
    {
        public float x,y,z;
        public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}
        public static Vector3 operator +(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);
        public static Vector3 Scale(Vector3 a,Vector3 b)=>new Vector3(a.x*b.x,a.y*b.y,a.z*b.z);
        public static float Dot(Vector3 a,Vector3 b)=>a.x*b.x+a.y*b.y+a.z*b.z;
    }
    public struct Bounds { public Vector3 center,extents; }
    public static class Mathf { public static float Min(float a,float b)=>Math.Min(a,b); public static float Max(float a,float b)=>Math.Max(a,b); }
    public class Transform { public string name; public Transform parent; }
    public class Material
    {
        // Used by the production VehicleVisualRoleUtility material classifiers.
        public string name;
        public readonly Dictionary<string,Texture> textures=new Dictionary<string,Texture>();
        public readonly Dictionary<string,Vector2> scales=new Dictionary<string,Vector2>(),offsets=new Dictionary<string,Vector2>();
        public bool HasProperty(string p)=>textures.ContainsKey(p);
        public Texture GetTexture(string p)=>textures[p];
        public Vector2 GetTextureScale(string p)=>scales[p];
        public Vector2 GetTextureOffset(string p)=>offsets[p];
        public void SetTextureScale(string p,Vector2 value){scales[p]=value;}
        public void SetTextureOffset(string p,Vector2 value){offsets[p]=value;}
    }
}
public static class VehicleLampUtilityChecks
{
    public static void Run()
    {
        var old=new UnityEngine.Material();var dest=new UnityEngine.Material();var legacy=new UnityEngine.Texture();var urp=new UnityEngine.Texture();
        old.textures["_MainTex"]=legacy;old.scales["_MainTex"]=new UnityEngine.Vector2(2,3);old.offsets["_MainTex"]=new UnityEngine.Vector2(.1f,.2f);
        if(MotorCity.World.VehicleLampMaterialUtility.ResolveBaseTexture(old)!=legacy)throw new Exception("Legacy texture lost");
        MotorCity.World.VehicleLampMaterialUtility.CopyTextureTransform(old,dest);
        if(dest.scales["_BaseMap"].x!=2||dest.offsets["_BaseMap"].y!=.2f)throw new Exception("Legacy UV transform lost");
        old.textures["_BaseMap"]=urp;
        if(MotorCity.World.VehicleLampMaterialUtility.ResolveBaseTexture(old)!=urp)throw new Exception("URP texture precedence changed");
        old.textures["_BaseMap"]=null;
        if(MotorCity.World.VehicleLampMaterialUtility.ResolveBaseTexture(old)!=legacy||MotorCity.World.VehicleLampMaterialUtility.ResolveBaseTexture(null)!=null)throw new Exception("Texture fallback changed");
        MotorCity.World.VehicleLampMaterialUtility.ResolveProjectionRange(new UnityEngine.Bounds{center=new UnityEngine.Vector3(1,2,3),extents=new UnityEngine.Vector3(2,4,6)},new UnityEngine.Vector3(.5f,0,1),out float min,out float max);
        if(min!= -3.5f||max!=10.5f)throw new Exception("Lamp projection bounds changed");
        var wheel=new UnityEngine.Transform{name="front wheel"};var child=new UnityEngine.Transform{name="mesh",parent=wheel};
        if(!MotorCity.World.VehicleLampMaterialUtility.IsWheelRenderer(child)||MotorCity.World.VehicleLampMaterialUtility.IsWheelRenderer(new UnityEngine.Transform{name="Body"}))throw new Exception("Wheel exclusion changed");
        var rim=new UnityEngine.Transform{name="Rear Rim"};
        var alloy=new UnityEngine.Transform{name="Alloy"};
        if(!MotorCity.World.VehicleLampMaterialUtility.IsWheelRenderer(rim))throw new Exception("Rim hierarchy exclusion changed");
        if(MotorCity.World.VehicleLampMaterialUtility.IsWheelRenderer(alloy))throw new Exception("Alloy must not be classified as a wheel by lamp helper");
        if(!MotorCity.Vehicle.VehicleVisualRoleUtility.IsWheelHierarchy(alloy))throw new Exception("Shared role utility should still support alloy");
        if(!MotorCity.Vehicle.VehicleVisualRoleUtility.IsRimMaterial(new UnityEngine.Material{name="car rim (Instance)"}))throw new Exception("Rim material classification changed");
    }
}

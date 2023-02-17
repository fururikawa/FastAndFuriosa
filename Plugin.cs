using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using Mirror;
using BepInEx.Configuration;
using System;

namespace FastAndFuriosa;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    private Harmony _harmony;

    private ConfigEntry<float> _acceleration;
    private ConfigEntry<float> _maxSpeed;

    public Plugin()
    {
        _acceleration = Config.Bind<float>("Main", "Acceleration", 1.5f);
        _maxSpeed = Config.Bind<float>("Main", "Max Speed", 14f);
    }

    private void Awake()
    {
        VehiclePatch.MaxSpeed = _maxSpeed.Value;
        VehiclePatch.Acceleration = _acceleration.Value;

        _harmony = Harmony.CreateAndPatchAll(typeof(VehiclePatch), "fururikawa.FastAndFuriosa");

        // Plugin startup logic
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
    }

    private void OnDestroy()
    {
        _harmony.UnpatchSelf();
    }
}

[HarmonyPatch]
public static class VehiclePatch
{
    public static float MaxSpeed = 14f;
    public static float Acceleration = 1.5f;

    [HarmonyPatch(typeof(Tractor), "OnStartClient")]
    [HarmonyPostfix]
    public static void OnStartClientPatch(Tractor __instance)
    {
        ControlVehicle vehicleControls = __instance.control;

        vehicleControls.acceleration = Acceleration;
        vehicleControls.maxSpeed = MaxSpeed;
        vehicleControls.turnSpeed = 195f;
        vehicleControls.turningReversedWhenBackwards = true;

        var spots = new List<Transform>();
        Transform original = __instance.harvestableSpot[0];
        Transform parent = original.GetParent();
        float breadth = 1.5f;

        for (float i = -breadth; i <= breadth; i++)
        {
            var obj = GameObject.Instantiate(original);
            obj.name = $"HarvestHere ({i + 4})";
            obj.transform.SetParent(parent, false);
            obj.localPosition = new Vector3(i * 2, 0, 0);
            spots.Add(obj);
        }

        foreach (var obj in __instance.harvestableSpot)
        {
            UnityEngine.Object.Destroy(obj.gameObject);
        }

        __instance.harvestableSpot = spots.ToArray();
    }

    [HarmonyPatch(typeof(Vehicle), "LateUpdate")]
    [HarmonyPrefix]
    public static bool LateUpdate(Vehicle __instance)
    {
        if (!hasAuthority(__instance))
        {
            ControlVehicle control = __instance.GetComponent<ControlVehicle>();
            FieldInfo folVelocity = typeof(Vehicle).GetField("folVelocity", BindingFlags.NonPublic | BindingFlags.Instance);
            Vector3 velocity = (Vector3)folVelocity.GetValue(__instance);
            __instance.myHitBox.position = Vector3.SmoothDamp(__instance.myHitBox.position, __instance.hitBoxFollow.position, ref velocity, 0.05f);
            __instance.myHitBox.rotation = Quaternion.Lerp(__instance.myHitBox.rotation, __instance.hitBoxFollow.rotation, control.turnSpeed / 100f);
        }

        return false;
    }

    [HarmonyPatch(typeof(NetworkBehaviour), "hasAuthority", MethodType.Getter)]
    [HarmonyReversePatch]
    private static bool hasAuthority(object instance)
    {
        throw new NotImplementedException();
    }
}

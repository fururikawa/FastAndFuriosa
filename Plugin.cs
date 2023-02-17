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
    private ConfigEntry<int> _breadth;

    public Plugin()
    {
        _acceleration = Config.Bind<float>("Main", "Acceleration", 1.5f);
        _maxSpeed = Config.Bind<float>("Main", "Max Speed", 14f);
        _breadth = Config.Bind<int>("Main", "Harvester Breadth", 4);
    }

    private void Awake()
    {
        ModSettings.MaxSpeed = _maxSpeed.Value;
        ModSettings.Acceleration = _acceleration.Value;
        ModSettings.Breadth = _breadth.Value;

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
    [HarmonyPatch(typeof(Tractor), "OnStartClient")]
    [HarmonyPostfix]
    public static void OnStartClientPatch(Tractor __instance)
    {
        ControlVehicle vehicleControls = __instance.control;

        vehicleControls.acceleration = ModSettings.Acceleration;
        vehicleControls.maxSpeed = ModSettings.MaxSpeed;
        vehicleControls.turnSpeed = 195f;
        vehicleControls.turningReversedWhenBackwards = true;

        var spots = new List<Transform>();
        Transform original = __instance.harvestableSpot[0];
        Transform parent = original.GetParent();

        float breadth = (ModSettings.Breadth - 1) / 2f;

        for (float i = -breadth; i <= breadth; i++)
        {
            var obj = GameObject.Instantiate(original);
            obj.name = $"HarvestHere ({Mathf.RoundToInt(i + breadth)})";
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

    [HarmonyPatch(typeof(Vehicle), "OnEnable")]
    [HarmonyPostfix]
    public static void OnEnablePatch(Vehicle __instance)
    {
        if (__instance.saveId == 6) // Helicopter
        {
            var control = __instance.GetComponent<ControlVehicle>();
            control.maxSpeed = 20;
            control.turningReversedWhenBackwards = true;
        }
        else if (__instance.saveId == 2) // Motorbike
        {
            var control = __instance.GetComponent<ControlVehicle>();
            control.maxSpeed = 23;
            control.acceleration = 1.5f;
            control.reverseSpeedClamp0to1 = -0.45f;
            control.turningReversedWhenBackwards = true;
        }
    }

    [HarmonyPatch(typeof(NetworkBehaviour), "hasAuthority", MethodType.Getter)]
    [HarmonyReversePatch]
    private static bool hasAuthority(object instance)
    {
        throw new NotImplementedException();
    }
}

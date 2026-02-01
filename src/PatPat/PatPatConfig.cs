using BepInEx.Configuration;
using UnityEngine;

public class PatPatConfig
{

    public static ConfigEntry<byte> PatPacketId;
    public static ConfigEntry<float> PatCooldown;
    public static ConfigEntry<float> StaminaExchange;
    public static ConfigEntry<float> PatMoralBoost;
    public static ConfigEntry<float> PatRaycastDistance;
    public static ConfigEntry<float> PatRaycastRadius;
    public static ConfigEntry<float> PatStaminaCooldown;
    public static ConfigEntry<float> PatWeight;
    public static ConfigEntry<float> PatSoundVolume;
    public static ConfigEntry<KeyCode> PatFirstKeybind;
    public static ConfigEntry<KeyCode> PatSecondKeybind;

    public static void Initialize(ConfigFile config)
    {
        PatPacketId = config.Bind(
            "PatPat",
            "PacketID",
            (byte) 139,
            "Packet ID used for PatPat networking"
        );

        PatCooldown = config.Bind(
            "PatPat",
            "Cooldown",
            4f / 20f,
            "Cooldown between pat actions"
        );

        StaminaExchange = config.Bind(
            "PatPat",
            "StaminaExchange",
            0.02f,
            "Amount of stamina exchanged per pat"
        );

        PatMoralBoost = config.Bind(
            "PatPat",
            "MoralBoost",
            0.2f,
            "Morale boost gained from patting"
        );

        PatRaycastDistance = config.Bind(
            "PatPat",
            "PatDistance",
            10f,
            "Maximum distance for patting targets"
        );

        PatRaycastRadius = config.Bind(
            "PatPat",
            "RaycastSphereRadius",
            0.3f,
            "Raycast sphere radius for pat detection"
        );

        PatStaminaCooldown = config.Bind(
            "PatPat",
            "StaminaCooldown",
            0.5f,
            "Cooldown before stamina regeneration after pat"
        );

        PatWeight = config.Bind(
            "PatPat",
            "PatWeight",
            0.425f,
            "The pat weight"
        );

        PatSoundVolume = config.Bind(
            "PatPat",
            "PatSoundVolume",
            0.25f,
            "The volume of pat sound"
        );
        
        PatFirstKeybind = config.Bind(
            "PatPat",
            "Pat1Keybind",
            KeyCode.LeftShift,
            "First keybind for pat action"
        );

        PatSecondKeybind = config.Bind(
            "PatPat",
            "Pat2Keybind",
            KeyCode.Mouse1,
            "Second keybind for pat action"
        );

    }
}
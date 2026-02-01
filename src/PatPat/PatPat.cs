using BepInEx;
using BepInEx.Logging;
using ExitGames.Client.Photon;
using HarmonyLib;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static CharacterAfflictions;

[BepInPlugin("net.lopymine.patpat", "PatPat", "1.0.0")]
public class PatPatPeakMod : BaseUnityPlugin, IOnEventCallback
{
    private readonly Harmony _harmony = new("net.lopymine.patpat");

    public static ManualLogSource ModLogger;
    public static PatPatSoundManager SoundManager;
    public static PatPatTextureManager TextureManager;
    public static PatPatManager PatPatManager;

    private IInteractible _customInteractible;

    private static readonly int[] DISALLOWED_TYPES = [(int) STATUSTYPE.Curse, (int) STATUSTYPE.Weight, (int) STATUSTYPE.Hunger];

    private bool _combinationPressed = false;

    public static float patCooldown = 0f;
    public static float lastPatTime = 0f;

    private void Awake()
    {
        ModLogger = Logger;
        _harmony.PatchAll();
        Logger.LogInfo("PatPat v1.0.0 initializing...");

        string pluginFolder = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "assets");

        PatPatConfig.Initialize(Config);

        SoundManager = new PatPatSoundManager();
        SoundManager.OnInitialize(pluginFolder, this);

        TextureManager = new PatPatTextureManager();
        TextureManager.OnInitialize(pluginFolder, this);

        PatPatManager = new PatPatManager();

        PhotonNetwork.AddCallbackTarget(this);
    }

    private void OnDestroy()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    private void Update()
    {
        patCooldown -= Time.deltaTime;
        if (patCooldown < 0f) patCooldown = 0f;

        bool wasCombinationPressed = _combinationPressed;

        _combinationPressed = (PatPatConfig.PatFirstKeybind.Value == KeyCode.None || Input.GetKey(PatPatConfig.PatFirstKeybind.Value)) && (PatPatConfig.PatSecondKeybind.Value == KeyCode.None || Input.GetKey(PatPatConfig.PatSecondKeybind.Value));

        if (wasCombinationPressed && !_combinationPressed)
        {
            patCooldown = 0;
        }

        if (_combinationPressed && patCooldown == 0f)
        {
            DoInteractableRaycasts(out _customInteractible);
            if (_customInteractible == null)
            {
                return;
            }

            patCooldown = PatPatConfig.PatCooldown.Value;
            lastPatTime = Time.time;

            CharacterInteractible? hovered = _customInteractible as CharacterInteractible;
            if (Character.localCharacter == null || hovered == null)
            {
                return;
            }

            Character character = Character.localCharacter;

            if (character.data.currentStamina <= 0)
            {
                return;
            }

            //if (hovered.character == character)
            //{
            //    return;
            //}

            character.AddStamina(-PatPatConfig.StaminaExchange.Value);

            object[] data = [
                hovered.character.photonView.ViewID,
                Character.localCharacter.photonView.ViewID
            ];

            var raiseOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All };
            PhotonNetwork.RaiseEvent(PatPatConfig.PatPacketId.Value, data, raiseOptions, SendOptions.SendReliable);
        }
    }


    private void DoInteractableRaycasts(out IInteractible interactableResult)
    {
        interactableResult = null;

        Camera cam = Camera.main;
        if (cam == null) return;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);

        RaycastHit[] hits = Physics.SphereCastAll(ray, PatPatConfig.PatRaycastRadius.Value, PatPatConfig.PatRaycastDistance.Value, HelperFunctions.GetMask(HelperFunctions.LayerType.CharacterAndDefault), QueryTriggerInteraction.Collide);

        if (hits == null || hits.Length == 0) return;

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var h in hits)
        {
            if (h.collider == null) continue;
            //if (IsColliderPartOfSelf(h.collider)) continue;

            IInteractible interactible = h.collider.GetComponentInParent<IInteractible>();
            if (interactible != null)
            {
                interactableResult = interactible;
                break;
            }
        }
    }

    private bool IsColliderPartOfSelf(Collider collider)
    {
        if (collider == null) return false;
        return collider.transform.IsChildOf(Character.localCharacter.transform);
    }

    public void OnEvent(EventData data)
    {
        if (data == null) return;
        if (data.Code != PatPatConfig.PatPacketId.Value) return;
        if (data.CustomData is not object[]) return;

        object[] array = (object[]) data.CustomData;

        PatPatManager.PatPlayer((int) array[0]);

        if ((int) array[0] != Character.localCharacter.photonView.ViewID)
        {
            return;
        }

        Character.localCharacter.AddStamina(PatPatConfig.StaminaExchange.Value);

        string whoPatted = PhotonNetwork.GetPhotonView((int)array[1]).GetComponent<Character>().characterName;
        if (UnityEngine.Random.Range(0, 300) == 1)
        {
            CharacterAfflictions afflictions = Character.localCharacter.refs.afflictions;

            List<int> activeIds = afflictions.currentStatuses
                .Select((value, index) => new { value, index })
                .Where(x => x.value > 0f && !DISALLOWED_TYPES.Contains(x.index))
                .Select(x => x.index)
                .ToList();

            float boost = PatPatConfig.PatMoralBoost.Value;

            if (activeIds.Count == 0)
            {
                if (Character.localCharacter.data.currentStamina == Character.localCharacter.GetMaxStamina())
                {
                    return;
                }
                Character.localCharacter.AddStamina(boost * 1.5F);
            }
            else
            {
                Array statuses = Enum.GetValues(typeof(STATUSTYPE));
                STATUSTYPE type = (STATUSTYPE) statuses.GetValue(activeIds[UnityEngine.Random.Range(0, activeIds.Count - 1)]);
                afflictions.AdjustStatus(type, -boost);
            }

            StaminaBar bar = GUIManager.instance.bar;
            bar.moraleBoostText.enabled = true;
            bar.moraleBoostText.text = $"Моральный буст от {whoPatted}!";
            StartMoraleBoost(bar);
        }
    }

    public static Coroutine StartMoraleBoost(MonoBehaviour instance)
    {
        MethodInfo mi = AccessTools.Method(instance.GetType(), "MoraleBoostRoutine");
        if (mi == null) throw new MissingMethodException("MoraleBoostRoutine not found");
        IEnumerator enumerator = (IEnumerator) mi.Invoke(instance, null);
        return instance.StartCoroutine(enumerator);
    }

}

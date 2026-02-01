using Photon.Pun;
using System.Collections.Generic;
using UnityEngine;

public class PatPatManager
{

    private readonly Dictionary<int, PatPatCharacterData> data = new Dictionary<int, PatPatCharacterData>();

    public void PatPlayer(int pattedCharacterId)
    {
        GameObject? gameObject;

        if (data.ContainsKey(pattedCharacterId))
        {
            PatPatCharacterData characterData = data[pattedCharacterId];
            characterData.animation?.Reset();
            characterData.model?.Reset();
            gameObject = characterData.soundGameObject;
        }
        else
        {
            Character character = PhotonNetwork.GetPhotonView(pattedCharacterId).GetComponent<Character>();
            PatPatModelManager.PatPatModel? model = PatPatModelManager.CreatePattableModelIfDoesntExists(character.gameObject);
            if (model == null) return;
            PatPatAnimation? animation = PatPatAnimation.Create(character, model, PatPatPeakMod.TextureManager.frames);
            if (animation == null) return;
            
            PatPatCharacterData characterData = new PatPatCharacterData();
            characterData.animation = animation;
            characterData.model = model;
            characterData.soundGameObject = character.transform.Find("Scout").Find("Armature").Find("Hip").gameObject;

            data[pattedCharacterId] = characterData;
            gameObject = characterData.soundGameObject;
        }

        if (gameObject == null) return;
        PatPatPeakMod.SoundManager.PlayRandomPatSoundForObject(gameObject);
    }

    public void RemoveExpired(int characterId)
    {
        if (data.ContainsKey(characterId))
        {
            data.Remove(characterId);
        }
    }


    private class PatPatCharacterData
    {
        public PatPatAnimation? animation;
        public PatPatModelManager.PatPatModel? model;
        public GameObject? soundGameObject;
    }
}
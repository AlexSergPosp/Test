using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using UniRx;
using UnityEngine;
using Resources = UnityEngine.Resources;

public class GameController : MonoBehaviour
{
    [HideInInspector]
    public PlayerData playerData;
    public GameData gameData;
    public static GameController inst;

    public void Awake()
    {
        playerData = null;
        if (inst == null) inst = this;
        LoadSavedPlayer();
    }

    private float d = 0;
    private float eve = 0;
    void Update () {

        d += Time.deltaTime;
        eve += Time.deltaTime;

        if (d >= 1)
        {
            d = 0;
            playerData.gold.Apply(playerData.passiveGold.count.value);
        }

        if (eve >= 20)
        {
            PopupController.Show(PopupType.Event, gameData.events[Random.Range(0, gameData.events.Count-1)]);
            eve = 0;
        }
    }

    public static string savePath { get { return Application.persistentDataPath + "/player.dat"; } }

    void SavePlayerData()
    {
        BinaryFormatter bf = new BinaryFormatter();
        FileStream f = File.Open(savePath, FileMode.Create);
        bf.Serialize(f, playerData);
        f.Close();
    }

    void LoadSavedPlayer()
    {
        gameData = ReadGameDataFromDisk();
        if (playerData == null) playerData = ReadPlayerFromDisk();
        if (playerData == null)
        {
            playerData = new PlayerData();
            playerData.Inicialization();
        }
    }

    public static PlayerData ReadPlayerFromDisk()
    {
        PlayerData player = null;
        if (File.Exists(savePath))
        {
            FileStream f = null;
            try
            {
                BinaryFormatter bf = new BinaryFormatter();
                f = File.Open(savePath, FileMode.Open);
                player = bf.Deserialize(f) as PlayerData;
                f.Close();
            }
            catch (System.Exception e)
            {
                if (f != null && f.CanRead)
                {
                    f.Close();
                }
                Debug.LogError("player deserialization error: " + e.Message);
            }
        }
        return player;
    }

    void OnApplicationPause(bool pause)
    {
        Debug.Log("pause: " + pause.ToString());
        if (playerData == null) return;
        if (pause)
        {
            SavePlayerData();
        }
    }

    public static GameData ReadGameDataFromDisk()
    {
        GameData game = null;
        var data = UnityEngine.Resources.Load("StaticData") as TextAsset;
        var stream = new MemoryStream(data.bytes);
        IFormatter formatter = new BinaryFormatter();
        game = formatter.Deserialize(stream) as GameData;
        //game.PrepareRefs();
        return game;
    }
}

using Assets.Scripts.Core;
using Mirror;
using Mirror.SimpleWeb;
using UnityEngine;
using UltimateArcade.Frontend;
using UltimateArcade.Server;
using System;
using System.Threading;

public class AutoConnect : NetworkBehaviour
{
#if UNITY_WEBGL && !UNITY_EDITOR

    void Start()
    {
        var nm = this.GetComponent<NetworkManager>();
        var swt = this.GetComponent<SimpleWebTransport>();
        swt.sslEnabled = ExternalScriptBehavior.IsSecure();
        swt.port = (ushort)ExternalScriptBehavior.Port();
        nm.networkAddress = ExternalScriptBehavior.Hostname();
        nm.StartClient();
    }

#else

    private UltimateArcadeGameServerAPI api;

    void Start()
    {
        var nm = this.GetComponent<NetworkManager>();
        nm.StartServer();
        this.api = new UltimateArcadeGameServerAPI();
        StartCoroutine(api.Init(this.ServerReady, this.ServerNotReady));
    }

    private void ServerReady(ServerData obj)
    {
        UnityEngine.Debug.Log("random seed: "+obj.RandomSeed);
    }

    private void ServerNotReady(string obj)
    {
        Thread.Sleep(1000);
        StartCoroutine(api.Init(this.ServerReady, this.ServerNotReady));
    }
#endif

}

using DefaultNamespace;
using GameLogic.model;
using PurrNet.Packing;
using UnityEngine;

public class UICellEnemy : UICell
{
    private int _x, _y;
    public GameObject[] impactParticles;
    public GameObject[] missParticles;
    public Material[] colors;
    private NetworkCommunicationHandler _networkCommunication;


    public override void Setup(int x, int y, NetworkCommunicationHandler networkCommunication)
    {
        _x = x;
        _y = y;
        _networkCommunication = networkCommunication;
    }

    public async void OnTouch()
    {
        if (CurrentState.State != States.Playing)
        {
            return;
        }
        using var writer = BitPackerPool.Get();
        Packer<int>.Write(writer, Config.PlayerId);
        Packer<int>.Write(writer, _x);
        Packer<int>.Write(writer, _y);

        var result = await _networkCommunication.HandleShoot(writer);
        Debug.Log(result);
    }


    public override void UpdateLook(ActionResult result)
    {
        switch (result)
        {
            case ActionResult.Hit or ActionResult.HitAndSunk or ActionResult.HitSunkAndWon:
            {
                if (impactParticles != null)
                {
                    foreach (var t in impactParticles)
                    {
                        if (t == null) continue;
                        Instantiate(t, transform); 
                    }
                }
                GetComponent<MeshRenderer>().material = colors[0];
                break;
            }
            case ActionResult.Miss:
            {
                GetComponent<MeshRenderer>().material = colors[1];
                foreach (var t in missParticles)
                {
                    if (t == null) continue;
                    Instantiate(t, transform);
                }
                break;
            }
            case ActionResult.AbilityShipFound:
            {
                GetComponent<MeshRenderer>().material = colors[3];
                break;
            }
            case ActionResult.AbilityShipNotFound:
            {
                GetComponent<MeshRenderer>().material = colors[2];
                break;
            }
            case ActionResult.AbilityUsed:
            {
                GetComponent<MeshRenderer>().material = colors[4];
                break;
            }
        }
    }
}
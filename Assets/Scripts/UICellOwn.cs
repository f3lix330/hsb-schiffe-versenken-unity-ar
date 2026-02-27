using GameLogic.model;
using UnityEngine;

public class UICellOwn : UICell
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

    public override void UpdateLook(ActionResult result)
    {
        switch (result)
        {
            case ActionResult.Hit or ActionResult.HitAndSunk or ActionResult.HitSunkAndWon:
            {
                if (impactParticles != null)
                {
                    for (int i = 0; i < impactParticles.Length; i++)
                    {
                        if (impactParticles[i] != null)
                        {
                            Instantiate(impactParticles[i], transform);
                        }
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
            case ActionResult.AbilityUsed:
            {
                GetComponent<MeshRenderer>().material = colors[2];
                break;
            }
        }
    }
}
using GameLogic.model;
using UnityEngine;

public abstract class UICell : MonoBehaviour
{
    public abstract void Setup(int x, int y, NetworkCommunicationHandler networkCommunication);
    public abstract void UpdateLook(ActionResult result);
}

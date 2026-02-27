using GameLogic.model;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public void HandleEvent(int x, int y, ActionResult result)
    {
        transform.GetChild(x * UIGridGenerator.Rows + y).gameObject.GetComponent<UICell>().UpdateLook(result);
    }
}

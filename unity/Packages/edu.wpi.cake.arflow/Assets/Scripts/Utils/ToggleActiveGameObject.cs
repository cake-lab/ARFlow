using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToggleActiveGameObject : MonoBehaviour
{
    public void toggle()
    {
        gameObject.SetActive(!gameObject.activeSelf);
    }
}

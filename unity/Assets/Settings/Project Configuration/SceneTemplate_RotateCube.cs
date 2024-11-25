using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// Rotates the cube in the template scene
public class SceneTemplate_RotateCube : MonoBehaviour
{
    [Tooltip("Changes the rotation speed of the cube")]
    public float rotateSpeed = 1f;

    [Tooltip("Changes orientation of the cube")]
    public Vector3 objectRotation;

    //Called every frame the app is running
    // Note that "*" represents multiplication
    void Update()
    {
        //Change the rotation (by the defined orientation * the time that has passed * defined speed)
        transform.Rotate(objectRotation * Time.deltaTime * rotateSpeed);
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

public class PopupText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textTemplate;
    
    private GameObject clientCamera;
    private Vector3 origin;
    private Vector3 direction;

    public void Initialize(string text, Color colorOnText)
    {
        origin = transform.position;
        clientCamera = FindObjectOfType<SphericCamera>().gameObject;
        
        transform.LookAt(clientCamera.transform);
        direction = new Vector3(Random.Range(-4, 4), 4, 0);

        textTemplate.text = text;
        textTemplate.color = colorOnText;
    }

    private void FixedUpdate()
    {
        Movement();
    }

    private void Movement()
    {
        if (Vector3.Distance(origin, transform.position) >= 3)
        {
            Destroy(gameObject);
        }
        else
        {
            transform.LookAt(clientCamera.transform);
            direction -= new Vector3(0, 0.1f, 0);
            transform.position += direction * Time.deltaTime;
        }
    }
}

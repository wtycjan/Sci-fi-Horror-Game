﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]

public class ProgressBarCircle : MonoBehaviour {
    [Header("Title Setting")]
    public string Title;
    public Color TitleColor;
    public Font TitleFont;    

    [Header("Bar Setting")]
    public Color BarColor;
    public Color BarBackGroundColor;
    public Color MaskColor;
    public Sprite BarBackGroundSprite;
    [Range(1f, 100f)]
    public int Alert = 20;
    public Color BarAlertColor;

    [Header("Sound Alert")]
    public AudioClip sound;
    public bool repeat = false;
    public float RepearRate = 1f;

    private Image bar, barBackground,Mask;
    private float nextPlay;
    private AudioSource audiosource;
    private Animator animator;
    private float barValue;
    public float BarValue
    {
        get { return barValue; }

        set
        {
            value = Mathf.Clamp(value, 0, 100);
            barValue = value;
            UpdateValue(barValue);

        }
    }

    private void Awake()
    {

        barBackground = transform.Find("BarBackgroundCircle").GetComponent<Image>();
        bar = transform.Find("BarCircle").GetComponent<Image>();
        audiosource = GetComponent<AudioSource>();
        Mask= transform.Find("Mask").GetComponent<Image>();
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        bar.color = BarColor;
        Mask.color = MaskColor;
        barBackground.color = BarBackGroundColor;
        barBackground.sprite = BarBackGroundSprite;

        UpdateValue(barValue);


    }

    void UpdateValue(float val)
    {
       
        bar.fillAmount = -(val / 100) + 1f;
        val = Convert.ToInt32(val);

        if (Alert >= val)
        {
            barBackground.color = BarAlertColor;
        }
        else
        {
            barBackground.color = BarBackGroundColor;
        }

    }
    public void Hide()
    {
        animator.SetBool("active", true);
    }
    public void Show()
    {
        animator.SetBool("active", false);
        animator.SetBool("start", true);
    }

    private void Update()
    {
       

        if (!Application.isPlaying)
        {
           
            UpdateValue(50);
            Mask.color = MaskColor;
            bar.color = BarColor;
            barBackground.color = BarBackGroundColor;
            barBackground.sprite = BarBackGroundSprite;
            
        }
        else
        {
            if (Alert >= barValue && Time.time > nextPlay)
            {
                nextPlay = Time.time + RepearRate;
                audiosource.PlayOneShot(sound);
            }
        }
    }

}

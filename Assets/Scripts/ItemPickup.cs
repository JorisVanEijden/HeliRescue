﻿using System;
using System.Collections;
using Items;
using Persistence;
using Skytanet.SimpleDatabase;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(PersistenceComponent))]
public class ItemPickup : MonoBehaviour, IPersist
{
    private AudioSource _audioSource;
    private Guid _guid;
    private ParticleSystem _particleSystem;

#pragma warning disable 649   // Backing fields are assigned through the Inspector
    [SerializeField] private int amountAvailable = 3;
    [SerializeField] private InventoryItem pickupType;
    [SerializeField] private bool playMultipleAudio;
    [SerializeField] private Transform visual;


    public void Load(SaveFile file)
    {
        amountAvailable = file.Get(_guid.ToString(), amountAvailable);
        gameObject.SetActive(amountAvailable > 0);
    }

    public void Save(SaveFile file)
    {
        file.Set(_guid.ToString(), amountAvailable);
    }

    private void Awake()
    {
        _guid = GetComponent<GuidComponent>().GetGuid();
        _audioSource = GetComponent<AudioSource>();
        _particleSystem = GetComponent<ParticleSystem>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        var transferAmount = amountAvailable;
        var freeSpace = pickupType.maximumAmount - pickupType.amount;
        if (amountAvailable > freeSpace) transferAmount = freeSpace;

        PlayPickupSound(transferAmount);
        PlayPickupParticles(transferAmount);

        pickupType.amount += transferAmount;
        amountAvailable -= transferAmount;

        if (amountAvailable <= 0)
        {
            visual.gameObject.SetActive(false);
            Destroy(gameObject, 2f);
        }
    }

    private void PlayPickupParticles(int amount)
    {
        if (_particleSystem != null && amount > 0) _particleSystem.Play();
    }

    private void PlayPickupSound(int amount)
    {
        if (_audioSource != null && amount > 0)
        {
            _audioSource.loop = playMultipleAudio;
            if (playMultipleAudio)
            {
                var duration = amount * _audioSource.clip.length - 0.1f;
                StartCoroutine(StopAudioSourceAfterDelay(duration));
            }

            _audioSource.Play();
        }
    }

    private IEnumerator StopAudioSourceAfterDelay(float duration)
    {
        yield return new WaitForSeconds(duration);
        _audioSource.loop = false;
    }
}
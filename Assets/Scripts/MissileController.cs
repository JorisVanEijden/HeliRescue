﻿using Items;
using Pooling;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class MissileController : PooledMonoBehaviour
{
    private Missile _missileData;
    private Rigidbody2D _physicsBody;
    private bool _colliding;

#pragma warning disable 0649   // Backing fields are assigned through the Inspector
    [SerializeField] private AudioClip[] audioClips;
    [SerializeField] private Transform body;
    [SerializeField] private ParticleSystem exhaust;
    [SerializeField] private Explosion[] explosionPrefabs;
#pragma warning restore 0649

    private void Awake()
    {
        _physicsBody = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        GetComponent<Collider2D>().enabled = true;
        body.gameObject.SetActive(true);
        exhaust.gameObject.SetActive(true);
    }

    private void FixedUpdate()
    {
        var direction = transform.right.normalized;
        _physicsBody.AddForce(direction * _missileData.thrust, ForceMode2D.Force);
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (_colliding) return;
        _colliding = true;
        var contact = other.GetContact(0);

        InstantiateExplosionPrefab(contact);
        PlayExplosionAudio();

        GetComponent<Collider2D>().enabled = false;
        body.gameObject.SetActive(false);
        exhaust.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        ReturnToPool(3f);
    }

    public void Launch(Vector2 direction, Vector2 velocity, Missile missileData)
    {
        _missileData = missileData;
        transform.right = direction;
        _physicsBody.AddForce(velocity * 0.5f, ForceMode2D.Impulse);
    }

    private void InstantiateExplosionPrefab(ContactPoint2D contact)
    {
        var explosionPrefab = explosionPrefabs[Random.Range(0, explosionPrefabs.Length)];
        var explosionInstance =
            explosionPrefab.Get<Explosion>(contact.point, Quaternion.identity);
        explosionInstance.transform.up = contact.normal;
        explosionInstance.ReturnToPool(5f);
    }

    private void PlayExplosionAudio()
    {
        var audioSource = GetComponent<AudioSource>();
        if (!audioSource || audioClips.Length == 0) return;

        audioSource.PlayOneShot(audioClips[Random.Range(0, audioClips.Length)]);
    }
}
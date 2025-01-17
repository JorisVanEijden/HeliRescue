﻿using System;
using System.Collections;
using System.Collections.Generic;
using Persistence;
using Skytanet.SimpleDatabase;
using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
[RequireComponent(typeof(PersistenceComponent))]
public class ChasingEnemy : MonoBehaviour, IPersist
{
    private bool _activated;
    private AudioSource _audioSource;
    private Transform _currentTarget;
    private Fader _fader;
    private Guid _guid;
    private bool _colliding;

#pragma warning disable 0649   // Backing fields are assigned through the Inspector
    [SerializeField] private Transform body;
    [SerializeField] private float chaseSpeed = 5f;
    [SerializeField] private float visualRange = 50f;
#pragma warning restore 0649

    private EnemyData Data
    {
        get =>
            new EnemyData
            {
                alive = gameObject.activeSelf,
                activated = _activated,
                position = transform.position
            };
        set
        {
            transform.position = value.position;
            gameObject.SetActive(value.alive);
            if (!_activated && value.activated) Activate();
        }
    }

    public void Load(SaveFile file)
    {
        Data = file.Get(_guid.ToString(), Data);
    }

    public void Save(SaveFile file)
    {
        file.Set(_guid.ToString(), Data);
    }

    private void Awake()
    {
        _guid = GetComponent<PersistenceComponent>().GetGuid();
        _fader = body.GetComponent<Fader>();
        _audioSource = GetComponent<AudioSource>();
        float radius = GetComponent<CircleCollider2D>().radius;
        if (visualRange <= radius)
        {
            Debug.LogWarning(
                $"visualRange {visualRange} on {gameObject.name} is too small. Setting to {radius * 2}");
            visualRange = radius * 2;
        }
    }

    private void FixedUpdate()
    {
        if (!_activated) return;

        if (_currentTarget != null)
        {
            FollowCurrentTarget();
        }
        else
        {
            if (!AcquireTarget()) StartCoroutine(Deactivate());
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!_activated && IsPlayer(other))
        {
            _currentTarget = other.transform;
            Activate();
        }
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (_colliding) return;
        _colliding = true;
        if (_activated) StartCoroutine(Die());
    }

    private void FollowCurrentTarget()
    {
        if (!TargetInRange()) return;

        float maxDistanceDelta = chaseSpeed * Time.deltaTime;
        transform.position = Vector3.MoveTowards(
            transform.position,
            _currentTarget.position,
            maxDistanceDelta
        );
    }

    private bool AcquireTarget()
    {
        ContactFilter2D contactFilter2D = new ContactFilter2D().NoFilter();
        var collider2Ds = new List<Collider2D>();
        Physics2D.OverlapCircle(transform.position, visualRange, contactFilter2D,
            collider2Ds);
        foreach (Collider2D other in collider2Ds)
        {
            if (!IsPlayer(other)) continue;
            _currentTarget = other.transform;
            return true;
        }

        return false;
    }

    private static bool IsPlayer(Component other)
    {
        return other.CompareTag("Player");
    }

    private bool TargetInRange()
    {
        float distance = Vector3.Distance(transform.position, _currentTarget.position);
        return distance < visualRange;
    }

    private IEnumerator Die()
    {
        yield return Deactivate();
        Destroy(gameObject);
    }

    private void Activate()
    {
        _activated = true;
        if (_fader) _fader.StartFadeIn();
        if (_audioSource) _audioSource.Play();
    }

    private IEnumerator Deactivate()
    {
        if (_fader) yield return _fader.FadeOut();
        _activated = false;
        gameObject.SetActive(false);
    }
}
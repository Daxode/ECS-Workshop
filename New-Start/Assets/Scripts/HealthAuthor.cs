using System;
using UnityEngine;

class HealthAuthor : MonoBehaviour
{
    [SerializeField] int maxHealth;
    [SerializeField] Sprite[] healthSprites;
    [SerializeField] ParticleSystem damageParticles;
}
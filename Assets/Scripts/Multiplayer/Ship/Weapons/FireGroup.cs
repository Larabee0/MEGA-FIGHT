using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MultiplayerRunTime {
    public class FireGroup : MonoBehaviour
    {
        private FireControlMP controller;
        [SerializeField] private WeaponBase[] weapons;
        public bool ShouldbeFiring => controller.fire;

        [SerializeField][Range(0f, 1f)] float groupFireIntervalMin = 0.01f;
        [SerializeField][Range(0f, 1f)] float groupFireIntervalMax = 0.05f;
        private float fireInterval = 0f;
        private int currentWeaponIndex = 0;

        private void Awake()
        {
            controller = GetComponentInParent<FireControlMP>();
            for (int i = 0; i < weapons.Length; i++)
            {
                weapons[i].fireGroup = this;
            }
        }

        private void Update()
        {
            switch (controller.fire)
            {
                case true:
                    Fire();
                    break;
            }
        }

        public void Fire()
        {
            fireInterval -= Time.deltaTime;

            switch (fireInterval)
            {
                case <= 0f:
                    fireInterval = UnityEngine.Random.Range(groupFireIntervalMin, groupFireIntervalMax);
                    weapons[currentWeaponIndex].GroupFire();
                    currentWeaponIndex = (currentWeaponIndex + 1) % weapons.Length;
                    break;
            }
        }
    }
}
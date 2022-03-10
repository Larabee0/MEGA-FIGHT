using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;


namespace SinglePlayerRunTime
{
    public class FireControlSP : MonoBehaviour
    {
        [SerializeField] private Transform[] WeaponOutputPoints;

        [SerializeField] private Transform TargetPoint;

        [SerializeField] private float dstSenstivity = 1f;
        [SerializeField] private float minDst;
        [SerializeField] private float maxDst;
        public float TargetDistance
        {
            get => targetDistance;
            set
            {
                targetDistance = math.clamp(value, minDst, maxDst); ;
                TargetPoint.localPosition = new(TargetPoint.localPosition.x, TargetPoint.localPosition.y, targetDistance);
            }
        }

        [SerializeField] private float targetDistance = 50f;

        void Start()
        {
            //SetTargetPointXY();
            TargetDistance = targetDistance;
        }

        private void SetTargetPointXY()
        {
            if (WeaponOutputPoints.Length == 0) return;
            Vector3 point = Vector3.zero;
            for (int i = 0; i < WeaponOutputPoints.Length; i++)
            {
                point += WeaponOutputPoints[i].position;
            }
            point /= WeaponOutputPoints.Length;
            TargetPoint.position = point;
        }

        // Update is called once per frame
        void Update()
        {
            for (int i = 0; i < WeaponOutputPoints.Length; i++)
            {
                Debug.DrawLine(WeaponOutputPoints[i].position, TargetPoint.position, Color.red);
            }
            TargetDistance += Input.GetAxis("Mouse ScrollWheel") * dstSenstivity * Time.deltaTime;
        }
    }
}
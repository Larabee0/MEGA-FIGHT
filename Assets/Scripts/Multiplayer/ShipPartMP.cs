using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace MultiplayerRunTime
{
    public class ShipPartMP : NetworkBehaviour
    {
        private NetworkVariable<float> health = new();

        public float Health
        {
            get => health.Value;
            set => SetDamageServerRpc(value);
        }

        [ServerRpc(Delivery = RpcDelivery.Reliable)]
        public void SetDamageServerRpc(float ammount)
        {
            health.Value -= ammount;
        }
    }
}
using System;
using UnityEngine;

namespace CliffGame
{
    [RequireComponent(typeof(PlayerMovement))]
    public class Player : MonoBehaviour
    {
        public static Player Instance { get; private set; }
        
        private void Awake()
        {
            Instance = this;
        }
        
        
    }
}

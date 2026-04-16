using UnityEngine;

namespace CliffGame
{
    public class BuildingManager : MonoBehaviour
    {
        public BuildingManager Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }
        
        
    }
}

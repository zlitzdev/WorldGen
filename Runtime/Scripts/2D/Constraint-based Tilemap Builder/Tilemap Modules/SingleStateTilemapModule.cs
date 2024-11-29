using UnityEngine;

using Rng = System.Random;

namespace Zlitz.Extra2D.WorldGen
{
    public abstract class SingleStateTilemapModule : MonoBehaviour, ITilemapModule<SingleStateTilemapModule.State>
    {
        private static State s_sharedState;

        public abstract string id { get; }

        public abstract bool isValid { get; }

        public State CreateState(Rng rng)
        {
            if (s_sharedState == null)
            {
                s_sharedState = new State();
            }
            return s_sharedState;
        }

        public GeneratedTilemapModule Generate(State state)
        {
            return Generate();
        }

        public bool NextState(State state)
        {
            return false;
        }

        protected abstract GeneratedTilemapModule Generate();

        public sealed class State : ITilemapModuleState
        {
        }
    }
}

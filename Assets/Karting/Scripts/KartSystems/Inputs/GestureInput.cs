using UnityEngine;

namespace KartGame.KartSystems
{
    public class GestureInput : BaseInput
    {
        private string currentGesture;

        public void SetGesture(string gesture)
        {
            currentGesture = gesture.ToLower();
        }

        public override InputData GenerateInput()
        {
            bool accelerate;
            bool brake;

            if (currentGesture == null || currentGesture == "stop")
            {
                accelerate = currentGesture == "stop";
                brake = currentGesture == "stop";

            }
            else
            {
                accelerate = currentGesture == "up";
                brake = currentGesture == "down";
            }

            float turn = 0f;

            if (currentGesture == "right") turn = 1f;
            else if (currentGesture == "left") turn = -1f;

            //currentGesture = null;

            return new InputData
            {
                Accelerate = accelerate,
                Brake = brake,
                TurnInput = turn
            };
        }
    }
}

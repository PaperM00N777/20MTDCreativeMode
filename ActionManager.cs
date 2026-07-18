using System.Collections.Generic;

namespace Final
{
    public class ActionManager
    {
        public List<GameAction> Actions { get; } = new List<GameAction>();
        public bool ExecutingActions { get; private set; } = false;

        private GameAction currentExecutingAction = null;
        private bool executionToggle = true;

        public void AddAction(GameAction action)
        {
            Actions.Add(action);
            SyncDisplay();
        }

        public void StartExecuting()
        {
            ExecutingActions = true;
        }

        public void Tick()
        {
            if (ExecutingActions && Actions.Count > 0)
            {
                if (executionToggle)
                {
                    currentExecutingAction = Actions[0];
                    SyncDisplay();
                    currentExecutingAction.execute();
                    currentExecutingAction.Complete = true;

                    if (currentExecutingAction.Complete)
                    {
                        currentExecutingAction = null;
                        Actions.RemoveAt(0);
                    }
                    executionToggle = false;
                }
                else
                {
                    executionToggle = true;
                }
            }

            if (ExecutingActions && Actions.Count == 0)
            {
                ExecutingActions = false;
                executionToggle = false;
            }
        }

        public void SyncDisplay() 
        {
            Main.Instance.uiAssetLoader.SyncActionDisplay(Actions);
        }
    }
}

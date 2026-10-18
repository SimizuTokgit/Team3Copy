using Unity.VisualScripting;

public class StateManager
{
    public ICharacterState CurrentState { get; private set; }

    public StateManager()
    {
        CurrentState = new AirMoveState();
    }

    public void Transition(Player player, ICharacterState fromState, ICharacterState nextState)
    {
        if (CurrentState != fromState) return;

        CurrentState.Exit(player);
        CurrentState = nextState;
        CurrentState.Enter(player);
    }
}

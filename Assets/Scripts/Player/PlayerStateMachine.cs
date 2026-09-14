public class PlayerStateMachine
{
    public PlayerState currentState { get; private set; }

    public void Initialize(PlayerState _startState)
    {
        currentState = _startState;
        currentState.Enter();
    }


    public void ChangeState(PlayerState _newstate)
    {
        currentState.Exit();
        currentState = _newstate;
        currentState.Enter();

    }
}

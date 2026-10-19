public interface ICharacterState
{
    void Enter(Player player);
    void Execute(Player player, InputInfo inputInfo);
    void Exit(Player player);
}

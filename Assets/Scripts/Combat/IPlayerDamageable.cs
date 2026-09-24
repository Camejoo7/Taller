/// <summary>
/// Enganche para el sistema de vida, que todavia no existe (va en una fase
/// posterior del roadmap). El combate ya llama a TakeDamage cuando el jugador
/// le erra a un fragmento; el dia que exista un componente de vida, solo tiene
/// que implementar esta interfaz y ponerse en el jugador — no hay que tocar
/// nada del codigo de combate.
/// </summary>
public interface IPlayerDamageable
{
    void TakeDamage(int amount);
}

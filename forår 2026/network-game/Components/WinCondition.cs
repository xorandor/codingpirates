using Engine;

namespace Components;

public class WinCondition : IComponent
{
    public static bool HasWon { get; set; }

    public string? Credits => "Oliver";

    public void Update(UpdateContext context)
    {
    }

    public void Render()
    {
    }
}

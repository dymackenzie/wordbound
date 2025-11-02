using Godot;

public interface ICameraShakeService
{
    void Update(double delta);
    Vector2 CurrentOffset { get; }
    void Shake(double duration, float magnitude);
    void Reset();
}

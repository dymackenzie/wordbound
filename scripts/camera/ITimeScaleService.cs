using Godot;

public interface ITimeScaleService
{
    float Current { get; }
    void SetScale(float scale);
    void Restore();
}

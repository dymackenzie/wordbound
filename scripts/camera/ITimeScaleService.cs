using Godot;

public interface ITimeScaleService
{
    double Current { get; }
    void SetScale(double scale);
    void Restore();
}

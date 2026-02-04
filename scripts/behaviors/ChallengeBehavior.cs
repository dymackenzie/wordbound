using Godot;

public partial class ChallengeBehavior : Resource
{
    /// <summary>
    ///  Transform the given word according to this behavior.
    /// </summary>
    public virtual string TransformWord(string word) => word;

    /// <summary>
    ///  Adjust time limit for this behavior (default: return baseTime)
    /// </summary>
    public virtual double GetTimeLimit(string baseWord, double baseTime) => baseTime;

    /// <summary>
    ///  Hook called when a fragment is assigned this word (optional visual effects)
    /// </summary>
    public virtual void OnAssigned(Node fragment, string word) { }
}

namespace Microtone.Models.Enums
{
    public enum LengthKind
    {
        None = -1, //点として扱う
        Fixed,      // 明示長
        Infinite,   // 無限
        UntilNext,   // 次のStartまで
        //UntilNext_Unsolved // 終点のため未解決
    }
}

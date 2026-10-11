// 장판이 사라져도 지속 피해의 출처가 유지된다. 조합 시 기존 출처도 같은 소유권 변경을 본다.
public sealed class PotionAreaOwnership
{
    private static long _nextThrowOrder;
    private static long _nextReactionOrder;
    public IDamageable Owner { get; private set; }
    public long ThrowOrder { get; private set; }
    public long ReactionOrder { get; private set; }

    public static long NextThrowOrder() => ++_nextThrowOrder;
    public void SetOwner(IDamageable owner) => Owner = owner;
    public void SetThrowOrder(long order) => ThrowOrder = order;
    // 날아가는 시간이 달라도 실제 착지해서 반응에 참여한 순서가 우선한다.
    public void MarkReaction() => ReactionOrder = ++_nextReactionOrder;

    public void TakeLatest(PotionAreaOwnership other)
    {
        if (other == null || other.ReactionOrder <= ReactionOrder) return;
        Owner = other.Owner;
        ThrowOrder = other.ThrowOrder;
        ReactionOrder = other.ReactionOrder;
    }
}

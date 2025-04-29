using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Goobstation.Shared.MartialArts.Events;

[Serializable, NetSerializable]
public sealed class MartialArtSaying(LocId saying) : EntityEventArgs
{
    public LocId Saying = saying;
};

[Serializable, NetSerializable]
public sealed class BurnSomeMunta : EntityEventArgs
{
    public NetEntity Target;
    public int Stacks;

    public BurnSomeMunta(NetEntity target, int stacks)
    {

        Target = target;
        Stacks = stacks;
    }
};

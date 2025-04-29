using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Goobstation.Shared.MartialArts.Events;

public sealed class MartialArtSaying(LocId saying) : EntityEventArgs
{
    public LocId Saying = saying;
};

public sealed class BurnSomeMunta : EntityEventArgs
{
    public EntityUid Target;
    public int Stacks;

    public BurnSomeMunta(EntityUid target, int stacks)
    {

        Target = target;
        Stacks = stacks;
    }
};

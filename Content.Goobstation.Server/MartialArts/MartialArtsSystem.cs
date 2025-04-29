// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aidenkrz <aiden@djkraz.com>
// SPDX-FileCopyrightText: 2025 Lincoln McQueen <lincoln.mcqueen@gmail.com>
// SPDX-FileCopyrightText: 2025 Misandry <mary@thughunt.ing>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.MartialArts;
using Content.Goobstation.Shared.MartialArts.Components;
using Content.Goobstation.Shared.MartialArts.Events;
using Content.Server.Chat.Systems;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;

namespace Content.Goobstation.Server.MartialArts;

/// <summary>
/// Just handles carp sayings for now.
/// </summary>
public sealed class MartialArtsSystem : SharedMartialArtsSystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly FlammableSystem _flammable = default!;


    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CanPerformComboComponent, MartialArtSaying>(OnMartialArtSaying);
        SubscribeLocalEvent<CanPerformComboComponent, BurnSomeMunta>(OnBurnSomeMunta);
    }

    private void OnMartialArtSaying(Entity<CanPerformComboComponent> ent, ref MartialArtSaying args)
    {
        _chat.TrySendInGameICMessage(ent, Loc.GetString(args.Saying), InGameICChatType.Speak, false);
    }
    private void OnBurnSomeMunta(Entity<CanPerformComboComponent> ent, ref BurnSomeMunta args)
    {
       if (TryComp(args.Target, out FlammableComponent? flammable))
            _flammable.AdjustFireStacks(args.Target, args.Stacks, flammable, true);
    }
}

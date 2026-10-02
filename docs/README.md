# GenesisUI documentation

Current baseline: [Release 1.1.2](releases/1.1.2.md), approved on 2026-10-02. The release tag
identifies its tested binary source; main also contains current documentation. Hexium upload
is managed by Diego. F4/F6 and in-game menus are shipped; main-menu/native-settings work and
exact-version resource adapters remain planned.

## Current guides

| Document | Purpose |
|---|---|
| [Vision](VISION.md) | Product boundaries and safety priorities |
| [Decisions](DECISIONS.md) | Recorded decisions and amendments |
| [Architecture](ARCHITECTURE.md) | Layers, ownership, scheduling, snapshots and input |
| [Regions](regions.md) | Native UI ownership, veils and hierarchy evidence |
| [Capabilities](CAPABILITIES.md) | What ships and what still needs integration/certification |
| [Stability fixes](STABILITY-FIXES.md) | All 25 review corrections and their scope |
| [Gameplay](GAMEPLAY.md) | Position-only inventory rules and native action delegation |
| [Art direction](ART-DIRECTION.md) | Approved gold language, assets and review requirements |
| [Diagnostics](DIAGNOSTICS.md) | Logs, reports and channel behavior |
| [Testing](TESTING.md) | Automated gates and client regression scripts |
| [Release](RELEASE.md) | Hexium layout, verified packaging and publication workflow |
| [Roadmap](ROADMAP.md) | Shipped phases and remaining work |
| [Patch policy](PATCH-POLICY.md) | Requirements for each guarded Harmony patch |

## Planned integration surfaces

- [Extension API](EXTENSION-API.md) — design draft; no public provider registration shipped.
- [Adapters](ADAPTERS.md) — exact-version module rules; foreign resource modules are future work.
- [FullPlaythrough study](review/2026-10-02/MODPACK.md) and
  [implementation sequence](review/2026-10-02/IMPLEMENTATION.md) — integration planning.

## Historical and client evidence

- [F4 plan](F4-PLAN.md) and [art review](F4-ART-REVIEW.md) retain their original design context.
- [Stability review](review/2026-10-02/README.md) is an immutable earlier-source audit.
- [Client scripts](testing/scripts/) and [results](testing/results/) retain version-specific evidence.
- [R-067 approval](testing/results/R-067-1.1.2-preview.3.md) records overall Release acceptance;
  unreported detailed scenarios and full modpack certification are tracked separately.

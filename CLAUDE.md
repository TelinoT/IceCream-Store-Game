# Elena's Ice Cream Store

A cozy mobile management sim (Unity, C#) in the spirit of *Good Pizza, Great Pizza* — but ice cream.
The player runs a shop, serves customers under a patience timer, completes daily/lifetime tasks,
upgrades their shop, and unlocks branching episodic storylines with VIP characters.

**Target platform:** iOS, distributed via TestFlight → App Store. Built on macOS (project lives on
an external SSD) through Xcode. Ship-fast is the priority — favor targeted fixes over refactors
unless asked.

**Engine:** Unity 6000.0.74f1 (Unity 6), Universal Render Pipeline (URP) 17.2.0, TextMeshPro, new
Input System-adjacent mobile input via a custom `MobileInputManager`.

## Repo layout

- `Assets/Scripts/` — all gameplay C# (flat folder, ~40 scripts, no namespaces currently).
- `Assets/ScriptableObjects/` — data-driven content:
  - `VIPs/` — one `VIPCharacterData` asset per character (e.g. `Jonas`), each holding its `Encounters` list.
  - `TASKS/Daily/` and `TASKS/Lifetime/` — `TaskData` assets.
  - `Upgrades/` — `UpgradeData` assets (shop upgrades, flavor unlocks).
  - `Perks/` — perk assets (tip boosts, wrong-order forgiveness, decor discounts, etc.).
  - `FOOD/Ingredients/` and `FOOD/Recipes/` — `IceCreamIngredient` and `IceCreamRecipe` assets.
- `Assets/Scenes/` — **there are several `Main*.unity` variants (`Main`, `Main 2`..`Main 5`) plus
  `SampleScene` and `PhotoShoot`.** It's not obvious from the filesystem alone which is the live
  dev scene — confirm with the user before assuming, and ask if the unused variants can be deleted.
- Third-party asset packs live under `Assets/` at top level (`Synty`, `Toon Suburban Pack`,
  `kenney_food-kit`, `GUI Pro - Simple Casual(Light)-2`, `3D Ice Cream Pack`, `whipped-cream-can`).
  Treat these as vendored/read-only.

## Core architecture

### Daily loop
`DayManager` is the game clock and phase driver (singleton `DayManager.Instance`). It owns:
- The in-world clock (`currentTimeOfDay`, catch-up speed logic tied to `customersServedToday`).
- Day/night lighting + skybox lerps, camera glides between counter/sign/night-hub positions.
- The day → summary panel → night hub → next day state machine (`StartDay` → `EndShift` →
  `ConfirmShopClosed` → `StartNextDay`).
- Kicking off `TaskManager.RolloverToNewDay()` and `VIPManager.ResetDailyVIPLimit()` each morning.

`CustomerManager` just spawns via `CustomerSpawner.SpawnCustomer()`, on a delay
(`timeBetweenCustomers`) between customers. `DayManager.CustomerServed()` decides whether to spawn
the next customer or end the shift.

### Customers & orders
`CustomerSpawner.SpawnCustomer()` first asks `VIPManager.GetReadyVIP()` whether a VIP should show
up today; otherwise it procedurally builds an `IceCreamRecipe` (`GenerateDynamicRecipe`) from
unlocked bases/flavors/toppings, respecting `UpgradeManager` unlock IDs, and generates matching
order dialogue from template strings.

`CustomerOrder` (on the customer prefab) branches hard on `isVIP`:
- **Generic:** greeting line → `StartPatienceTimer()` (delay from `UpgradeManager` `initial_delay`
  stat) → countdown modified by `max_patience`/`max_tip_up` upgrade stats → `ReceiveOrder` or
  `LeaveAngry`.
- **VIP:** `PlayVIPDialogue()` coroutine plays `VIPEncounter.introLines` sequentially (tap to
  advance, tap-while-typing to skip via `DialogueTypewriter.SkipTyping()`), fades in A/B choice
  buttons via a `CanvasGroup` lerp, plays the matching `responseA_Lines`/`responseB_Lines`, then
  calls `VIPManager.AssignVIPTask()` and either wraps up (`requiresOrder == false`) or drops into
  the same order flow as a generic customer.

`DialogueTypewriter` does the letter-by-letter reveal (`isTyping`, `ShowDialogue`,
`SkipTyping`), with punctuation pauses and blip SFX via `AudioManager`.

Serving itself: the player drags ingredients (`IngredientDispenser`, `DraggableBase`) onto
`IceCreamStack`, which is compared against `desiredRecipe` in
`CustomerOrder.HandleOrderWithDelay` (`stack.MatchesRecipe`).

### VIP episodic story system
`VIPManager` is the story director, entirely `PlayerPrefs`-driven, keyed by **character name**
(not an ID — renaming a `VIPCharacterData.characterName` will orphan existing players' save data):
- `VIP_Encounter_<name>` (int) — index into `VIPCharacterData.encounters`.
- `VIP_WaitingForTask_<name>` (0/1) — true once a task has been assigned and we're waiting for the
  player to complete + claim it before advancing the encounter index.

`GetReadyVIP()` only offers one VIP per day (`vipSpawnChance` roll, `vipSpawnedToday` latch reset
each morning), and advances the encounter index by checking whether the task it previously assigned
is still sitting in `TaskManager.currentLifetimeTasks` — if it's gone, the player claimed it and the
story can move to the next encounter.

`VIPCharacterData` (ScriptableObject) → list of `VIPEncounter`: intro lines, optional A/B choice +
response lines, `requiresOrder`, optional forced `specificOrder`, and an optional `taskToAssign`.

### Tasks & save data
`TaskManager` manages three lists: `todayTasks`, `tomorrowTasks` (daily, regenerated each
`GenerateTomorrowTasks()`/`RolloverToNewDay()`), and `currentLifetimeTasks` (persistent, capped at
`maxActiveLifetimeTasks`). Progress is reported by goal type (`TaskGoalType`: ServeCustomers,
EarnMoney, SellPerfectIceCream, AddSprinkles, ServeCones, BuyUpgrades) via
`TaskManager.ReportProgress`, called from `CustomerOrder`, `EconomyManager`, `UpgradeManager`.

Completed lifetime tasks move into an internal `completedLifetimeTasks` vault
(`RolloverToNewDay`) so they never re-enter the `allLifetimeTasks` pool via `RefillLifetimeTasks()`.
This vault is why `VIPManager` can tell a task was *claimed* rather than just *completed*.

Saved as JSON under `PlayerPrefs["TaskSaveData"]` (`TaskSaveData`/`SavedTask` classes,
`JsonUtility`). Tasks are matched back to their `TaskData` asset by `taskID` (falls back to the
asset's file `.name` if `taskID` is blank) — **don't rename or delete a `TaskData` asset that's
referenced by `taskID`/name in an existing save**, and don't leave `taskID` blank if you can help it.

### Economy & upgrades
`EconomyManager` holds `coins`/`xp` in memory, mirrors to `PlayerPrefs` (`Coins`, `XP`) on every
change, and drives the floating-reward-text juice. `UpgradeManager` stores per-upgrade level under
`PlayerPrefs["Upgrade_<upgradeID>"]` and exposes `GetCurrentStatValueByID(id)` — this is the generic
stat-lookup hook other systems pull from (`max_patience`, `max_tip_up`, `initial_delay`,
`wrong_order_chance`, `double_pay`, `base_price_up`, `topping_craze`, etc., defined per `UpgradeData`
asset, not as an enum — the ID string is the only link between code and data, so typos fail silently
via `Debug.LogWarning` + return `0f`).

## Persistence model (important)

**Everything is `PlayerPrefs`** — no cloud save, no external DB. On iOS this is backed by
`NSUserDefaults`, so it survives app updates but not uninstalls, and isn't shared across devices.
Known keys: `TaskSaveData` (JSON), `Coins`, `XP`, `CurrentDay`, `Upgrade_<id>`,
`VIP_Encounter_<name>`, `VIP_WaitingForTask_<name>`. When adding new persistent state, follow this
pattern unless there's a specific reason to introduce a different save path — don't mix in a new
save system without discussing it first.

## Conventions observed in this codebase

- Heavy singleton usage: `X.Instance` set in `Awake()`, generally without null-checking for a
  pre-existing instance (`EconomyManager`/`TaskManager` do guard with `Destroy(gameObject)` on
  duplicates; several others — `VIPManager`, `UpgradeManager`, `DayManager` — just overwrite
  `Instance` unconditionally). Match whichever pattern the file you're editing already uses rather
  than "fixing" it as a drive-by.
- Data-driven content lives in ScriptableObjects (`Shop/...` and `IceCream/...` menu paths); avoid
  hardcoding game content (recipes, tasks, VIP dialogue, upgrade values) in code.
- UI transitions are hand-rolled coroutines lerping `CanvasGroup.alpha` / `RectTransform.anchoredPosition`
  with an ease-out cubic (`1 - (1-t)^3`), using `Time.unscaledDeltaTime` so they keep running while
  `Time.timeScale = 0` (used during the day/night transition). Follow this style for new UI juice
  rather than introducing a tweening library, unless asked.
- Dialogue/typing waits on `Input.GetMouseButtonDown(0)` for both "advance" and "skip" — this is also
  how mobile tap input is read here (no separate touch handling in `CustomerOrder`/`DialogueTypewriter`).
- `FindObjectOfType<T>()` is used at a couple of call sites (`DayManager` → `CustomerManager`) instead
  of a cached reference — expected to be slow-ish but infrequent (once per customer/day), not a hot path.

## Known rough edges (flag, don't silently "fix" unless asked)

- `ProjectSettings/ProjectSettings.asset` still has the default template
  `applicationIdentifier` (`com.UnityTechnologies.com.unity.template.urp-blank`) — this **must**
  be changed to a real bundle ID before any App Store submission.
- Multiple near-duplicate scenes (`Main`, `Main 2`–`5`, `SampleScene`, `PhotoShoot`) — confirm which
  is canonical before editing scene content, and consider cleaning up the rest.
- `EconomyManager.AddReward` re-reads `coins` from `PlayerPrefs.GetInt("Coins", 0)` at the top
  (discarding the in-memory value) before adding — worth understanding before touching economy code,
  since it means in-memory `coins` and the saved value are expected to always agree.
- The repo's `.git` is ~530MB, mostly large vendored textures/fonts (some single files 30–40MB:
  `Toon Suburban Pack` terrain textures, Synty textures, TMP `Rubik`/`Quicksand` SDF font assets).
  Worth considering Git LFS before this grows further — not done as part of this setup pass since
  migrating existing history needs a deliberate decision from you.

## Git

- `.gitignore` follows the standard Unity template (`Library/`, `Temp/`, `Obj/`, `Build(s)/`,
  `Logs/`, `UserSettings/`, generated `.csproj`/`.sln`, etc.) plus additions for `.DS_Store`,
  `.idea/`, `.vscode/`, Xcode build/user-state files, and iOS signing material
  (`*.mobileprovision`, `*.p12`, `*.cer`) — never commit those regardless of what's ignored.
- `.idea/` and stray `.DS_Store` files were previously committed; they've been untracked (still
  ignored going forward) but not deleted from disk.

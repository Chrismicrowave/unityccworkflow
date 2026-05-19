# Systems Patterns

## Awake vs Start — self vs cross-script

- **Awake** is for yourself: `GetComponent`, `GetComponentsInChildren`, setting private fields, registering in a singleton (e.g. `RefHub.Instance.System = this`)
- **Start** is for talking to others: resolving references from `RefHub`, `FindObjectOfType`, reading another script's state
- **Why**: Unity runs all Awakes in undefined order, then all Starts in undefined order. Reading cross-script state in Awake is a race condition. Start guarantees every script has had its Awake — so every script has already self-registered and self-initialized.
- **Common mistake**: resolving `RefHub.Instance.PlayerController` in Awake. If PlayerController's Awake hasn't run yet, you get null. Move to Start.
- **Exception**: none. Execution-order settings are a fragile workaround, not a fix.

## Subscribe in Start, not OnEnable

- **OnEnable is for your own setup**, not for subscribing to other scripts' events.
- **When you subscribe to a singleton event in OnEnable**: the singleton's `Awake()` (which sets `Instance = this`) may not have run yet, especially if both components are on the same GameObject and the singleton is listed later in the inspector order. The subscription silently fails — no error, no warning, the event never fires.
- **Start guarantees all Awakes have completed**, so the singleton Instance is guaranteed to exist.
- **Common violation**: `GameModeManager.Instance.OnModeChanged += Handler` in `OnEnable()`. If GameModeManager is the last component on the GO, its Awake hasn't run yet — `Instance` is null, subscription silently dropped.
- **Rule**: subscribe in `Start()`, unsubscribe in `OnDisable()`. This survives disable/re-enable cycles and guarantees the provider is initialized.
- **Exception**: subscribing to self-resolved events (e.g. `_myAction.performed += Handler` where `_myAction` was resolved from your own `GetComponent<PlayerInput>()` in Awake) — fine in OnEnable since the resolution doesn't depend on other scripts.

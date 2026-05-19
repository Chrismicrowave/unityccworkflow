# Systems Patterns

## Awake vs Start — self vs cross-script

- **Awake** is for yourself: `GetComponent`, `GetComponentsInChildren`, setting private fields, registering in a singleton (e.g. `RefHub.Instance.System = this`)
- **Start** is for talking to others: resolving references from `RefHub`, `FindObjectOfType`, reading another script's state
- **Why**: Unity runs all Awakes in undefined order, then all Starts in undefined order. Reading cross-script state in Awake is a race condition. Start guarantees every script has had its Awake — so every script has already self-registered and self-initialized.
- **Common mistake**: resolving `RefHub.Instance.PlayerController` in Awake. If PlayerController's Awake hasn't run yet, you get null. Move to Start.
- **Exception**: none. Execution-order settings are a fragile workaround, not a fix.
